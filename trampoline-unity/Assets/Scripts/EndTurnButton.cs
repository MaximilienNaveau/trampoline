using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Component for "End Turn" button in multiplayer mode.
/// Handles turn progression and displays current player info.
/// </summary>
[RequireComponent(typeof(Button))]
public class EndTurnButton : MonoBehaviour
{
    [Header("Optional References")]
    [SerializeField]
    [Tooltip("Optional: Text component to show button label")]
    private TextMeshProUGUI buttonText_;
    
    [Header("Display Settings")]
    [SerializeField]
    [Tooltip("Format for button text. Use {0} for current player number")]
    private string buttonTextFormat_ = "End Turn (Player {0})";
    
    [SerializeField]
    [Tooltip("Show button text or just use fixed text")]
    private bool updateButtonText_ = true;
    
    private Button button_;
    private GameControllerMultiplayer gameController_;
    private TurnManager turnManager_;

    void Start()
    {
        button_ = GetComponent<Button>();
        
        gameController_ = FindAnyObjectByType<GameControllerMultiplayer>();
        turnManager_ = FindAnyObjectByType<TurnManager>();
        
        if (gameController_ == null)
        {
            Debug.LogError("EndTurnButton: GameControllerMultiplayer not found!");
            button_.interactable = false;
            return;
        }
        
        if (turnManager_ == null)
        {
            Debug.LogError("EndTurnButton: TurnManager not found!");
            button_.interactable = false;
            return;
        }
        
        // Get button text if not assigned
        if (buttonText_ == null)
        {
            buttonText_ = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        // Hook up button click
        button_.onClick.AddListener(OnEndTurnClicked);
        
        // Subscribe to turn changes
        turnManager_.OnTurnChanged += OnTurnChanged;
        turnManager_.OnGameCompleted += OnGameCompleted;
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
        UpdateButtonState();
    }

    /// <summary>
    /// Called when the button is clicked.
    /// </summary>
    private void OnEndTurnClicked()
    {
        if (gameController_ == null || turnManager_ == null)
        {
            return;
        }
        
        // Check if current player has placed at least one word
        int currentPlayer = turnManager_.GetCurrentPlayerId();
        int completeWords = gameController_.GetPlayerCompleteWordCount(currentPlayer);
        
        // You can add validation here if needed
        // For example: require at least one valid word before ending turn
        
        Debug.Log($"EndTurnButton: Player {currentPlayer + 1} ended their turn.");
        gameController_.EndCurrentPlayerTurn();
    }

    /// <summary>
    /// Update button text and interactability.
    /// </summary>
    private void UpdateButtonState()
    {
        if (turnManager_ == null)
        {
            return;
        }
        
        // Update button text to show current player
        if (updateButtonText_ && buttonText_ != null)
        {
            int currentPlayer = turnManager_.GetCurrentPlayerId();
            buttonText_.text = string.Format(buttonTextFormat_, currentPlayer + 1);
        }
        
        // Disable button if game is complete
        if (button_ != null)
        {
            button_.interactable = !turnManager_.IsGameComplete();
        }
    }

    /// <summary>
    /// Called when turn changes to a new player.
    /// </summary>
    private void OnTurnChanged(int newPlayerId)
    {
        Debug.Log($"EndTurnButton: Now Player {newPlayerId + 1}'s turn.");
        UpdateButtonState();
    }

    /// <summary>
    /// Called when all players have finished.
    /// </summary>
    private void OnGameCompleted()
    {
        Debug.Log("EndTurnButton: Game completed - button disabled.");
        if (button_ != null)
        {
            button_.interactable = false;
        }
        
        if (buttonText_ != null)
        {
            buttonText_.text = "Game Complete!";
        }
    }
}
