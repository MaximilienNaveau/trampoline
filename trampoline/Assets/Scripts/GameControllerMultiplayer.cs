using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Multiplayer game controller that manages multiple players, their scores, and turn flow.
/// </summary>
public class GameControllerMultiplayer : MonoBehaviour
{
    private FrenchDictionary frenchDictionary_;
    private BoardMultiplayer board_;
    private TokenPool tokenPool_;
    private TokenDistributor tokenDistributor_;
    private TurnManager turnManager_;
    private PlayerManager playerManager_;
    private StoreMultiplayer[] playerStores_;
    
    private int numberOfPlayers_ = 2;
    private List<WordWithOwner>[] playerValidWords_;  // Track valid words per player
    private int[] playerFrozenScores_;  // Frozen scores from previous turns

    void Start()
    {
        // Initialize dictionary
        frenchDictionary_ = new FrenchDictionary();
        frenchDictionary_.initialize(async: false);
        
        // Get PlayerManager
        playerManager_ = PlayerManager.Instance;
        if (playerManager_ == null)
        {
            Debug.LogError("GameControllerMultiplayer: PlayerManager not found!");
            throw new System.Exception("GameControllerMultiplayer: PlayerManager not found!");
        }
        
        // Get number of players
        numberOfPlayers_ = playerManager_.GetNumberOfPlayers();
        
        // Initialize valid words tracking
        playerValidWords_ = new List<WordWithOwner>[numberOfPlayers_];
        playerFrozenScores_ = new int[numberOfPlayers_];
        for (int i = 0; i < numberOfPlayers_; i++)
        {
            playerValidWords_[i] = new List<WordWithOwner>();
            playerFrozenScores_[i] = 0;
        }
        
        // Get required components
        board_ = FindAnyObjectByType<BoardMultiplayer>();
        if (board_ == null)
        {
            Debug.LogError("GameControllerMultiplayer: BoardMultiplayer not found!");
            throw new System.Exception("GameControllerMultiplayer: BoardMultiplayer not found!");
        }
        
        tokenPool_ = FindAnyObjectByType<TokenPool>();
        if (tokenPool_ == null)
        {
            Debug.LogError("GameControllerMultiplayer: TokenPool not found!");
            throw new System.Exception("GameControllerMultiplayer: TokenPool not found!");
        }
        
        tokenDistributor_ = FindAnyObjectByType<TokenDistributor>();
        if (tokenDistributor_ == null)
        {
            Debug.LogError("GameControllerMultiplayer: TokenDistributor not found!");
            throw new System.Exception("GameControllerMultiplayer: TokenDistributor not found!");
        }
        
        turnManager_ = FindAnyObjectByType<TurnManager>();
        if (turnManager_ == null)
        {
            Debug.LogError("GameControllerMultiplayer: TurnManager not found!");
            throw new System.Exception("GameControllerMultiplayer: TurnManager not found!");
        }
        
        // Find the single dynamic store (no longer need array)
        StoreMultiplayer store = FindAnyObjectByType<StoreMultiplayer>();
        if (store == null)
        {
            Debug.LogWarning("GameControllerMultiplayer: StoreMultiplayer not found (optional for gameplay).");
        }
        
        // Subscribe to turn changes
        turnManager_.OnTurnChanged += OnTurnChanged;
        turnManager_.OnGameCompleted += OnGameCompleted;
        
        Debug.Log($"GameControllerMultiplayer: Initialized for {numberOfPlayers_} players.");
    }

    void OnDestroy()
    {
        if (turnManager_ != null)
        {
            turnManager_.OnTurnChanged -= OnTurnChanged;
            turnManager_.OnGameCompleted -= OnGameCompleted;
        }
    }

    void Update()
    {
        // Only update current player's score in real-time
        // Other players' scores remain frozen until their turn
        if (turnManager_ != null)
        {
            int currentPlayer = turnManager_.GetCurrentPlayerId();
            UpdateCurrentPlayerScore(currentPlayer);
        }
        
        // Check if current player has completed 13 words
        CheckPlayerCompletion();
    }

    /// <summary>
    /// Called when the turn changes to a new player.
    /// </summary>
    private void OnTurnChanged(int newPlayerId)
    {
        // When turn changes, the previous player's turn just ended
        // Assign position scores to new tokens and freeze validated words
        int previousPlayerId = (newPlayerId - 1 + numberOfPlayers_) % numberOfPlayers_;
        
        Debug.Log($"GameControllerMultiplayer: Turn changed to Player {newPlayerId + 1}. Processing Player {previousPlayerId + 1}'s moves.");
        
        // Assign position scores to newly placed tokens
        board_.AssignPositionScoresToNewTokens(previousPlayerId);
        
        // Freeze validated words
        FreezeValidatedWordsForPlayer(previousPlayerId);
        
        // Finalize and freeze the score for the previous player
        FinalizePlayerScore(previousPlayerId);
        
        // The StoreMultiplayer will automatically draw tokens when turn changes
    }

    /// <summary>
    /// Called when all players have finished.
    /// </summary>
    private void OnGameCompleted()
    {
        Debug.Log("GameControllerMultiplayer: Game completed!");
        ShowFinalResults();
    }

    /// <summary>
    /// Update scores for all players based on their valid words.
    /// Uses progressive scoring: 1+2+3+4... for each letter placed.
    /// Only counts scores for dictionary-validated words.
    /// </summary>
    private void UpdateAllPlayerScores()
    {
        for (int playerId = 0; playerId < numberOfPlayers_; playerId++)
        {
            FinalizePlayerScore(playerId);
        }
    }
    
    /// <summary>
    /// Update the current player's score in real-time (frozen + current turn).
    /// </summary>
    private void UpdateCurrentPlayerScore(int playerId)
    {
        if (playerId < 0 || playerId >= numberOfPlayers_)
        {
            return;
        }
        
        // Get current turn score (unfrozen tokens)
        int currentTurnScore = board_.GetPlayerCurrentTurnScore(playerId, frenchDictionary_);
        
        // Get word-based penalties for current turn
        List<WordWithOwner> playerWords = board_.GetPlayerWords(playerId);
        List<WordWithOwner> validWords = ComputeListOfValidWords(playerWords);
        int greenLetterPenalty = ComputeGreenLetterPenalty(validWords);
        
        // Current score = frozen score (from previous turns) + current turn score - penalty
        int currentScore = playerFrozenScores_[playerId] + currentTurnScore - greenLetterPenalty;
        
        playerValidWords_[playerId] = validWords;
        playerManager_.SetPlayerScore(playerId, currentScore);
    }
    
    /// <summary>
    /// Finalize a player's score after their turn ends.
    /// Freezes the current turn score and adds it to their frozen score.
    /// </summary>
    private void FinalizePlayerScore(int playerId)
    {
        if (playerId < 0 || playerId >= numberOfPlayers_)
        {
            return;
        }
        
        // Get total progressive score (frozen + current turn)
        int totalProgressiveScore = board_.GetPlayerProgressiveScore(playerId, frenchDictionary_);
        
        // Get word-based penalties (green letters)
        List<WordWithOwner> playerWords = board_.GetPlayerWords(playerId);
        List<WordWithOwner> validWords = ComputeListOfValidWords(playerWords);
        int greenLetterPenalty = ComputeGreenLetterPenalty(validWords);
        
        // Final score after this turn
        int finalScore = totalProgressiveScore - greenLetterPenalty;
        
        // Store as frozen score for next turn
        playerFrozenScores_[playerId] = finalScore;
        
        playerValidWords_[playerId] = validWords;
        playerManager_.SetPlayerScore(playerId, finalScore);
        
        Debug.Log($"GameControllerMultiplayer: Player {playerId + 1} frozen score: {playerFrozenScores_[playerId]}");
    }

    /// <summary>
    /// Compute list of valid words from a list of words with owners.
    /// </summary>
    private List<WordWithOwner> ComputeListOfValidWords(List<WordWithOwner> listOfWords)
    {
        List<WordWithOwner> listOfValidWords = new List<WordWithOwner>();
        
        foreach (WordWithOwner word in listOfWords)
        {
            if (frenchDictionary_.isWordValid(word.word_))
            {
                listOfValidWords.Add(word);
            }
        }
        
        return listOfValidWords;
    }

    /// <summary>
    /// Compute green letter penalty for valid words.
    /// Green letters subtract 5 points each.
    /// </summary>
    private int ComputeGreenLetterPenalty(List<WordWithOwner> listOfValidWords)
    {
        int penalty = 0;
        
        foreach (WordWithOwner word in listOfValidWords)
        {
            penalty += word.nb_green_letters_ * 5;
        }
        
        return penalty;
    }
    
    /// <summary>
    /// Freeze validated words for a player after their turn ends.
    /// </summary>
    private void FreezeValidatedWordsForPlayer(int playerId)
    {
        List<WordWithOwner> playerWords = board_.GetPlayerWords(playerId);
        List<WordWithOwner> validWords = ComputeListOfValidWords(playerWords);
        
        foreach (WordWithOwner word in validWords)
        {
            board_.FreezeValidatedWord(word.rowIndex_, word.word_);
        }
        
        Debug.Log($"GameControllerMultiplayer: Froze {validWords.Count} validated words for Player {playerId + 1}");
    }

    /// <summary>
    /// Compute score for a list of valid words (OLD METHOD - kept for reference).
    /// Now we use progressive scoring instead.
    /// </summary>
    private int ComputeScore(List<WordWithOwner> listOfValidWords)
    {
        int score = 0;
        
        foreach (WordWithOwner word in listOfValidWords)
        {
            int n = word.word_.Length;
            score += n * (n + 1) / 2;
            score -= word.nb_green_letters_ * 5;
        }
        
        return score;
    }

    /// <summary>
    /// Check if the current player has completed 13 words of 9 letters.
    /// </summary>
    private void CheckPlayerCompletion()
    {
        int currentPlayer = turnManager_.GetCurrentPlayerId();
        
        if (turnManager_.IsPlayerFinished(currentPlayer))
        {
            return;  // Already marked as finished
        }
        
        int completeWords = board_.GetPlayerCompleteWordCount(currentPlayer);
        
        if (completeWords >= 13)
        {
            Debug.Log($"GameControllerMultiplayer: Player {currentPlayer + 1} has completed 13 words!");
            turnManager_.SetPlayerFinished(currentPlayer, true);
        }
    }

    /// <summary>
    /// Get the score for a specific player.
    /// </summary>
    public int GetPlayerScore(int playerId)
    {
        if (playerId < 0 || playerId >= numberOfPlayers_)
        {
            return 0;
        }
        return playerManager_.GetPlayerScore(playerId);
    }

    /// <summary>
    /// Get all player scores.
    /// </summary>
    public int[] GetAllPlayerScores()
    {
        return playerManager_.GetAllPlayerScores();
    }

    /// <summary>
    /// Get valid words for a specific player.
    /// </summary>
    public List<WordWithOwner> GetPlayerValidWords(int playerId)
    {
        if (playerId < 0 || playerId >= numberOfPlayers_)
        {
            return new List<WordWithOwner>();
        }
        return new List<WordWithOwner>(playerValidWords_[playerId]);
    }

    /// <summary>
    /// Get the number of complete words (9 letters) for a player.
    /// </summary>
    public int GetPlayerCompleteWordCount(int playerId)
    {
        return board_.GetPlayerCompleteWordCount(playerId);
    }

    /// <summary>
    /// End the current player's turn manually.
    /// </summary>
    public void EndCurrentPlayerTurn()
    {
        turnManager_.EndCurrentTurn();
    }

    /// <summary>
    /// Show final results when game is complete.
    /// </summary>
    private void ShowFinalResults()
    {
        Debug.Log("=== FINAL RESULTS ===");
        
        int winnerIndex = 0;
        int highestScore = playerManager_.GetPlayerScore(0);
        
        for (int i = 0; i < numberOfPlayers_; i++)
        {
            int completeWords = board_.GetPlayerCompleteWordCount(i);
            int playerScore = playerManager_.GetPlayerScore(i);
            Debug.Log($"Player {i + 1}: {playerScore} points, {completeWords} complete words");
            
            if (playerScore > highestScore)
            {
                highestScore = playerScore;
                winnerIndex = i;
            }
        }
        
        Debug.Log($"Winner: Player {winnerIndex + 1} with {highestScore} points!");
    }

    /// <summary>
    /// Get the winner's player ID (or -1 if game not complete).
    /// </summary>
    public int GetWinnerId()
    {
        if (!turnManager_.IsGameComplete())
        {
            return -1;
        }
        
        int winnerIndex = 0;
        int highestScore = playerManager_.GetPlayerScore(0);
        
        for (int i = 1; i < numberOfPlayers_; i++)
        {
            int playerScore = playerManager_.GetPlayerScore(i);
            if (playerScore > highestScore)
            {
                highestScore = playerScore;
                winnerIndex = i;
            }
        }
        
        return winnerIndex;
    }

    /// <summary>
    /// Get number of players in the game.
    /// </summary>
    public int GetNumberOfPlayers()
    {
        return numberOfPlayers_;
    }

    /// <summary>
    /// Get current player ID.
    /// </summary>
    public int GetCurrentPlayerId()
    {
        return turnManager_.GetCurrentPlayerId();
    }
}
