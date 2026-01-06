using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Manages turn-based gameplay for multiplayer Trampoline game.
/// Tracks current player, handles turn progression, and manages game state.
/// </summary>
public class TurnManager : MonoBehaviour
{
    private int currentPlayerId_ = 0;
    private int numberOfPlayers_ = 2;
    private bool[] playerFinished_;
    private bool gameStarted_ = false;
    
    // Events
    public event Action<int> OnTurnChanged;
    public event Action OnGameCompleted;

    void Start()
    {
        // Get number of players from PlayerPrefs
        numberOfPlayers_ = PlayerPrefs.GetInt("NumberOfPlayers", 2);
        numberOfPlayers_ = Mathf.Clamp(numberOfPlayers_, 2, 4);
        
        playerFinished_ = new bool[numberOfPlayers_];
        for (int i = 0; i < numberOfPlayers_; i++)
        {
            playerFinished_[i] = false;
        }
        
        gameStarted_ = true;
        currentPlayerId_ = 0;
        
        // Notify listeners of initial turn
        OnTurnChanged?.Invoke(currentPlayerId_);
        
        Debug.Log($"TurnManager: Game started with {numberOfPlayers_} players. Player {currentPlayerId_ + 1}'s turn.");
    }

    /// <summary>
    /// Get the current player's ID (0-indexed).
    /// </summary>
    public int GetCurrentPlayerId()
    {
        return currentPlayerId_;
    }

    /// <summary>
    /// Get the total number of players in the game.
    /// </summary>
    public int GetNumberOfPlayers()
    {
        return numberOfPlayers_;
    }

    /// <summary>
    /// Check if it's a specific player's turn.
    /// </summary>
    public bool IsPlayerTurn(int playerId)
    {
        if (playerId < 0 || playerId >= numberOfPlayers_)
        {
            return false;
        }
        return currentPlayerId_ == playerId;
    }

    /// <summary>
    /// End the current player's turn and advance to the next player.
    /// Skips players who have already finished.
    /// </summary>
    public void EndCurrentTurn()
    {
        if (!gameStarted_)
        {
            Debug.LogWarning("TurnManager: Cannot end turn - game not started.");
            return;
        }

        Debug.Log($"TurnManager: Player {currentPlayerId_ + 1} ended their turn.");
        
        // Move to next player
        int startPlayerId = currentPlayerId_;
        do
        {
            currentPlayerId_ = (currentPlayerId_ + 1) % numberOfPlayers_;
            
            // If we've cycled through all players, check if game is complete
            if (currentPlayerId_ == startPlayerId)
            {
                if (IsGameComplete())
                {
                    Debug.Log("TurnManager: Game completed!");
                    gameStarted_ = false;
                    OnGameCompleted?.Invoke();
                    return;
                }
            }
        }
        while (playerFinished_[currentPlayerId_]);
        
        Debug.Log($"TurnManager: Now Player {currentPlayerId_ + 1}'s turn.");
        OnTurnChanged?.Invoke(currentPlayerId_);
    }

    /// <summary>
    /// Mark a player as having finished their words (13 words completed).
    /// </summary>
    public void SetPlayerFinished(int playerId, bool finished)
    {
        if (playerId < 0 || playerId >= numberOfPlayers_)
        {
            Debug.LogError($"TurnManager: Invalid player ID {playerId}");
            return;
        }
        
        playerFinished_[playerId] = finished;
        Debug.Log($"TurnManager: Player {playerId + 1} finished status set to {finished}.");
        
        // If current player just finished, automatically move to next turn
        if (finished && playerId == currentPlayerId_)
        {
            EndCurrentTurn();
        }
    }

    /// <summary>
    /// Check if a specific player has finished their words.
    /// </summary>
    public bool IsPlayerFinished(int playerId)
    {
        if (playerId < 0 || playerId >= numberOfPlayers_)
        {
            return false;
        }
        return playerFinished_[playerId];
    }

    /// <summary>
    /// Check if the game is complete (all players finished).
    /// </summary>
    public bool IsGameComplete()
    {
        for (int i = 0; i < numberOfPlayers_; i++)
        {
            if (!playerFinished_[i])
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Force switch to a specific player's turn (useful for debugging or special cases).
    /// </summary>
    public void ForcePlayerTurn(int playerId)
    {
        if (playerId < 0 || playerId >= numberOfPlayers_)
        {
            Debug.LogError($"TurnManager: Invalid player ID {playerId}");
            return;
        }
        
        if (playerFinished_[playerId])
        {
            Debug.LogWarning($"TurnManager: Player {playerId + 1} has already finished.");
            return;
        }
        
        currentPlayerId_ = playerId;
        Debug.Log($"TurnManager: Forced turn to Player {currentPlayerId_ + 1}.");
        OnTurnChanged?.Invoke(currentPlayerId_);
    }

    /// <summary>
    /// Reset the game state (useful for testing).
    /// </summary>
    public void ResetGame()
    {
        currentPlayerId_ = 0;
        for (int i = 0; i < numberOfPlayers_; i++)
        {
            playerFinished_[i] = false;
        }
        gameStarted_ = true;
        OnTurnChanged?.Invoke(currentPlayerId_);
        Debug.Log("TurnManager: Game reset.");
    }

    /// <summary>
    /// Get how many players have finished.
    /// </summary>
    public int GetFinishedPlayerCount()
    {
        int count = 0;
        for (int i = 0; i < numberOfPlayers_; i++)
        {
            if (playerFinished_[i])
            {
                count++;
            }
        }
        return count;
    }
}
