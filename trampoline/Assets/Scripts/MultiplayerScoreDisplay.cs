using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// UI component to display scores for all players in multiplayer mode.
/// Updates automatically each frame to show current scores and turn indicator.
/// </summary>
public class MultiplayerScoreDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    [Tooltip("Text elements for each player's score (array of 4)")]
    private TextMeshProUGUI[] playerScoreTexts_;
    
    [SerializeField]
    [Tooltip("Optional: Image/Panel to highlight current player")]
    private Image[] playerHighlights_;
    
    [Header("Display Settings")]
    [SerializeField]
    [Tooltip("Format string for score display. Use {0} for player#, {1} for score, {2} for complete words")]
    private string scoreFormat_ = "Player {0}: {1} pts ({2}/13)";
    
    [SerializeField]
    [Tooltip("Color for current player's text")]
    private Color currentPlayerColor_ = Color.yellow;
    
    [SerializeField]
    [Tooltip("Color for other players' text")]
    private Color normalPlayerColor_ = Color.white;
    
    private GameControllerMultiplayer gameController_;
    private TurnManager turnManager_;
    private int numberOfPlayers_;

    void Start()
    {
        gameController_ = FindAnyObjectByType<GameControllerMultiplayer>();
        turnManager_ = FindAnyObjectByType<TurnManager>();
        
        if (gameController_ == null)
        {
            Debug.LogError("MultiplayerScoreDisplay: GameControllerMultiplayer not found!");
            enabled = false;
            return;
        }
        
        if (turnManager_ == null)
        {
            Debug.LogError("MultiplayerScoreDisplay: TurnManager not found!");
            enabled = false;
            return;
        }
        
        numberOfPlayers_ = turnManager_.GetNumberOfPlayers();
        
        // Validate we have enough text elements
        if (playerScoreTexts_ == null || playerScoreTexts_.Length < numberOfPlayers_)
        {
            Debug.LogError($"MultiplayerScoreDisplay: Need at least {numberOfPlayers_} text elements!");
            enabled = false;
            return;
        }
        
        // Hide unused player displays
        for (int i = numberOfPlayers_; i < playerScoreTexts_.Length; i++)
        {
            if (playerScoreTexts_[i] != null)
            {
                playerScoreTexts_[i].gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (gameController_ == null || turnManager_ == null)
        {
            return;
        }
        
        UpdateScoreDisplay();
        UpdateTurnHighlight();
    }

    /// <summary>
    /// Update the score text for all players.
    /// </summary>
    private void UpdateScoreDisplay()
    {
        int[] scores = gameController_.GetAllPlayerScores();
        int currentPlayer = turnManager_.GetCurrentPlayerId();
        
        for (int i = 0; i < numberOfPlayers_; i++)
        {
            if (playerScoreTexts_[i] == null)
            {
                continue;
            }
            
            int score = scores[i];
            int completeWords = gameController_.GetPlayerCompleteWordCount(i);
            
            // Format the text
            string displayText = string.Format(scoreFormat_, i + 1, score, completeWords);
            playerScoreTexts_[i].text = displayText;
            
            // Highlight current player
            playerScoreTexts_[i].color = (i == currentPlayer) ? currentPlayerColor_ : normalPlayerColor_;
            
            // Add finished indicator
            if (turnManager_.IsPlayerFinished(i))
            {
                playerScoreTexts_[i].text += " ✓";
            }
        }
    }

    /// <summary>
    /// Update visual highlight for current player's turn.
    /// </summary>
    private void UpdateTurnHighlight()
    {
        if (playerHighlights_ == null || playerHighlights_.Length == 0)
        {
            return;
        }
        
        int currentPlayer = turnManager_.GetCurrentPlayerId();
        
        for (int i = 0; i < playerHighlights_.Length && i < numberOfPlayers_; i++)
        {
            if (playerHighlights_[i] != null)
            {
                // Enable highlight only for current player
                playerHighlights_[i].enabled = (i == currentPlayer);
            }
        }
    }

    /// <summary>
    /// Manually refresh the display (useful after major changes).
    /// </summary>
    public void RefreshDisplay()
    {
        UpdateScoreDisplay();
        UpdateTurnHighlight();
    }
}
