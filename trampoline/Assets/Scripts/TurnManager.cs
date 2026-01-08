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
    private PlayerManager playerManager_;
    private bool gameStarted_ = false;
    
    // Events
    public event Action<int> OnTurnChanged;
    public event Action OnGameCompleted;

    void Start()
    {
        playerManager_ = PlayerManager.Instance;
        if (playerManager_ == null)
        {
            Debug.LogError("TurnManager: PlayerManager not found!");
            throw new System.Exception("TurnManager: PlayerManager not found!");
        }
        
        gameStarted_ = true;
        playerManager_.SetCurrentPlayer(0);
        
        // Notify listeners of initial turn
        OnTurnChanged?.Invoke(playerManager_.GetCurrentPlayerId());
        
        Debug.Log($"TurnManager: Game started with {playerManager_.GetNumberOfPlayers()} players. Player {playerManager_.GetCurrentPlayerId() + 1}'s turn.");
    }

    /// <summary>
    /// Get the current player's ID (0-indexed).
    /// </summary>
    public int GetCurrentPlayerId()
    {
        PlayerManager pm = PlayerManager.Instance;
        if (pm == null)
        {
            Debug.LogError("TurnManager.GetCurrentPlayerId: PlayerManager.Instance is null!");
            return 0; // Default fallback
        }
        return pm.GetCurrentPlayerId();
    }

    /// <summary>
    /// Get the total number of players in the game.
    /// </summary>
    public int GetNumberOfPlayers()
    {
        PlayerManager pm = PlayerManager.Instance;
        if (pm == null)
        {
            Debug.LogError("TurnManager.GetNumberOfPlayers: PlayerManager.Instance is null!");
            return 2; // Default fallback
        }
        return pm.GetNumberOfPlayers();
    }

    /// <summary>
    /// Check if it's a specific player's turn.
    /// </summary>
    public bool IsPlayerTurn(int playerId)
    {
        if (playerId < 0 || playerId >= playerManager_.GetNumberOfPlayers())
        {
            return false;
        }
        return playerManager_.GetCurrentPlayerId() == playerId;
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

        int currentPlayer = playerManager_.GetCurrentPlayerId();
        Debug.Log($"TurnManager: Player {currentPlayer + 1} ended their turn.");
        
        // Move to next player
        int startPlayerId = currentPlayer;
        int nextPlayer = currentPlayer;
        do
        {
            nextPlayer = (nextPlayer + 1) % playerManager_.GetNumberOfPlayers();
            
            // If we've cycled through all players, check if game is complete
            if (nextPlayer == startPlayerId)
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
        while (playerManager_.IsPlayerFinished(nextPlayer));
        
        playerManager_.SetCurrentPlayer(nextPlayer);
        Debug.Log($"TurnManager: Now Player {nextPlayer + 1}'s turn.");
        OnTurnChanged?.Invoke(nextPlayer);
    }

    /// <summary>
    /// Mark a player as having finished their words (13 words completed).
    /// </summary>
    public void SetPlayerFinished(int playerId, bool finished)
    {
        if (playerId < 0 || playerId >= playerManager_.GetNumberOfPlayers())
        {
            Debug.LogError($"TurnManager: Invalid player ID {playerId}");
            return;
        }
        
        playerManager_.SetPlayerFinished(playerId, finished);
        Debug.Log($"TurnManager: Player {playerId + 1} finished status set to {finished}.");
        
        // If current player just finished, automatically move to next turn
        if (finished && playerId == playerManager_.GetCurrentPlayerId())
        {
            EndCurrentTurn();
        }
    }

    /// <summary>
    /// Check if a specific player has finished their words.
    /// </summary>
    public bool IsPlayerFinished(int playerId)
    {
        if (playerId < 0 || playerId >= playerManager_.GetNumberOfPlayers())
        {
            return false;
        }
        return playerManager_.IsPlayerFinished(playerId);
    }

    /// <summary>
    /// Check if the game is complete (all players finished).
    /// </summary>
    public bool IsGameComplete()
    {
        for (int i = 0; i < playerManager_.GetNumberOfPlayers(); i++)
        {
            if (!playerManager_.IsPlayerFinished(i))
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
        if (playerId < 0 || playerId >= playerManager_.GetNumberOfPlayers())
        {
            Debug.LogError($"TurnManager: Invalid player ID {playerId}");
            return;
        }
        
        if (playerManager_.IsPlayerFinished(playerId))
        {
            Debug.LogWarning($"TurnManager: Player {playerId + 1} has already finished.");
            return;
        }
        
        playerManager_.SetCurrentPlayer(playerId);
        Debug.Log($"TurnManager: Forced turn to Player {playerId + 1}.");
        OnTurnChanged?.Invoke(playerId);
    }

    /// <summary>
    /// Reset the game state (useful for testing).
    /// </summary>
    public void ResetGame()
    {
        playerManager_.ResetAllPlayers();
        gameStarted_ = true;
        OnTurnChanged?.Invoke(playerManager_.GetCurrentPlayerId());
        Debug.Log("TurnManager: Game reset.");
    }

    /// <summary>
    /// Get how many players have finished.
    /// </summary>
    public int GetFinishedPlayerCount()
    {
        int count = 0;
        for (int i = 0; i < playerManager_.GetNumberOfPlayers(); i++)
        {
            if (playerManager_.IsPlayerFinished(i))
            {
                count++;
            }
        }
        return count;
    }
}
