using UnityEngine;

/// <summary>
/// Centralized manager for all player information.
/// Single source of truth for player count, colors, current turn, and metadata.
/// </summary>
[DefaultExecutionOrder(-100)]
public class PlayerManager : MonoBehaviour
{
    [Header("Player Configuration")]
    [SerializeField]
    [Tooltip("Number of players in the game (1-4)")]
    private int numberOfPlayers_ = 2;
    
    [SerializeField]
    [Tooltip("Player colors for visual distinction")]
    private Color[] playerColors_ = {
        new Color(0.13f, 0.59f, 0.95f, 0.9f),  // Blue 500 - Bleu tech fiable
        new Color(0.30f, 0.69f, 0.31f, 0.9f),  // Green 500 - Vert nature
        new Color(1f, 0.76f, 0.03f, 0.9f),     // Amber 500 - Jaune doré
        new Color(0.61f, 0.15f, 0.69f, 0.9f),  // Purple 500 - Violet créatif
        new Color(1f, 0.60f, 0f, 0.9f),        // Orange 500 - Orange action
        new Color(0f, 0.59f, 0.53f, 0.9f),     // Teal 500 - Sarcelle calme
        new Color(0.91f, 0.12f, 0.39f, 0.9f),  // Pink 500 - Rose moderne
        new Color(0.38f, 0.49f, 0.55f, 0.9f)   // Blue Grey 500 - Gris bleu
    };
    
    // Current game state
    private int currentPlayerId_ = 0;
    private int[] playerScores_;
    private bool[] playerFinished_;
    private int[] playerCompleteWords_;
    
    // Singleton instance
    private static PlayerManager instance_;
    
    public static PlayerManager Instance
    {
        get
        {
            if (instance_ == null)
            {
                instance_ = FindAnyObjectByType<PlayerManager>();
            }
            return instance_;
        }
    }
    
    void Awake()
    {
        // Singleton pattern
        if (instance_ != null && instance_ != this)
        {
            Destroy(gameObject);
            return;
        }
        instance_ = this;
        
        // Initialize arrays
        InitializePlayerData();
    }
    
    private void InitializePlayerData()
    {
        // Ensure we have enough colors
        if (playerColors_.Length < numberOfPlayers_)
        {
            Debug.LogWarning($"PlayerManager: Not enough colors defined ({playerColors_.Length}) for {numberOfPlayers_} players. Using default colors.");
        }
        
        // Initialize player state arrays
        playerScores_ = new int[numberOfPlayers_];
        playerFinished_ = new bool[numberOfPlayers_];
        playerCompleteWords_ = new int[numberOfPlayers_];
        
        for (int i = 0; i < numberOfPlayers_; i++)
        {
            playerScores_[i] = 0;
            playerFinished_[i] = false;
            playerCompleteWords_[i] = 0;
        }
        
        currentPlayerId_ = 0;
    }
    
    // Getters for player information
    public int GetNumberOfPlayers() => numberOfPlayers_;
    public int GetCurrentPlayerId() => currentPlayerId_;
    public Color[] GetPlayerColors() => playerColors_;
    public Color GetPlayerColor(int playerId)
    {
        if (playerId >= 0 && playerId < playerColors_.Length)
        {
            return playerColors_[playerId];
        }
        return Color.white;
    }
    
    // Score management
    public int[] GetAllPlayerScores() => playerScores_;
    public int GetPlayerScore(int playerId)
    {
        if (playerId >= 0 && playerId < playerScores_.Length)
        {
            return playerScores_[playerId];
        }
        return 0;
    }
    
    public void SetPlayerScore(int playerId, int score)
    {
        if (playerId >= 0 && playerId < playerScores_.Length)
        {
            playerScores_[playerId] = score;
        }
    }
    
    public void AddToPlayerScore(int playerId, int points)
    {
        if (playerId >= 0 && playerId < playerScores_.Length)
        {
            playerScores_[playerId] += points;
        }
    }
    
    // Complete words tracking
    public int GetPlayerCompleteWordCount(int playerId)
    {
        if (playerId >= 0 && playerId < playerCompleteWords_.Length)
        {
            return playerCompleteWords_[playerId];
        }
        return 0;
    }
    
    public void SetPlayerCompleteWordCount(int playerId, int count)
    {
        if (playerId >= 0 && playerId < playerCompleteWords_.Length)
        {
            playerCompleteWords_[playerId] = count;
        }
    }
    
    // Player finished state
    public bool IsPlayerFinished(int playerId)
    {
        if (playerId >= 0 && playerId < playerFinished_.Length)
        {
            return playerFinished_[playerId];
        }
        return false;
    }
    
    public void SetPlayerFinished(int playerId, bool finished)
    {
        if (playerId >= 0 && playerId < playerFinished_.Length)
        {
            playerFinished_[playerId] = finished;
        }
    }
    
    // Turn management
    public void SetCurrentPlayer(int playerId)
    {
        if (playerId >= 0 && playerId < numberOfPlayers_)
        {
            currentPlayerId_ = playerId;
        }
    }
    
    public void NextPlayer()
    {
        currentPlayerId_ = (currentPlayerId_ + 1) % numberOfPlayers_;
    }
    
    // Reset game state
    public void ResetAllPlayers()
    {
        InitializePlayerData();
    }
}
