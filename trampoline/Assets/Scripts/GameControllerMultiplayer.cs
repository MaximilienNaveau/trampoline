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
    private StoreMultiplayer[] playerStores_;
    
    private int numberOfPlayers_ = 2;
    private int[] playerScores_;
    private List<WordWithOwner>[] playerValidWords_;  // Track valid words per player

    void Start()
    {
        // Initialize dictionary
        frenchDictionary_ = new FrenchDictionary();
        frenchDictionary_.initialize(async: false);
        
        // Get number of players
        numberOfPlayers_ = PlayerPrefs.GetInt("NumberOfPlayers", 2);
        numberOfPlayers_ = Mathf.Clamp(numberOfPlayers_, 2, 4);
        
        // Initialize score tracking
        playerScores_ = new int[numberOfPlayers_];
        playerValidWords_ = new List<WordWithOwner>[numberOfPlayers_];
        for (int i = 0; i < numberOfPlayers_; i++)
        {
            playerScores_[i] = 0;
            playerValidWords_[i] = new List<WordWithOwner>();
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
        // Update scores for all players
        UpdateAllPlayerScores();
        
        // Check if current player has completed 13 words
        CheckPlayerCompletion();
    }

    /// <summary>
    /// Called when the turn changes to a new player.
    /// </summary>
    private void OnTurnChanged(int newPlayerId)
    {
        Debug.Log($"GameControllerMultiplayer: Turn changed to Player {newPlayerId + 1}.");
        
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
    /// </summary>
    private void UpdateAllPlayerScores()
    {
        for (int playerId = 0; playerId < numberOfPlayers_; playerId++)
        {
            List<WordWithOwner> playerWords = board_.GetPlayerWords(playerId);
            List<WordWithOwner> validWords = ComputeListOfValidWords(playerWords);
            
            playerValidWords_[playerId] = validWords;
            playerScores_[playerId] = ComputeScore(validWords);
        }
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
    /// Compute score for a list of valid words.
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
        return playerScores_[playerId];
    }

    /// <summary>
    /// Get all player scores.
    /// </summary>
    public int[] GetAllPlayerScores()
    {
        return (int[])playerScores_.Clone();
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
        int highestScore = playerScores_[0];
        
        for (int i = 0; i < numberOfPlayers_; i++)
        {
            int completeWords = board_.GetPlayerCompleteWordCount(i);
            Debug.Log($"Player {i + 1}: {playerScores_[i]} points, {completeWords} complete words");
            
            if (playerScores_[i] > highestScore)
            {
                highestScore = playerScores_[i];
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
        int highestScore = playerScores_[0];
        
        for (int i = 1; i < numberOfPlayers_; i++)
        {
            if (playerScores_[i] > highestScore)
            {
                highestScore = playerScores_[i];
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
