using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Self-contained UI component to display scores for all players in multiplayer mode.
/// Creates a grid of colored boxes with scores, highlighting current player.
/// Can be placed anywhere in the hierarchy.
/// </summary>
public class ScoreDisplay : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField]
    [Tooltip("Spacing between score boxes")]
    private float spacing_ = 8f;
    
    [SerializeField]
    [Tooltip("Sprite for score boxes (leave empty to use Unity's default rounded UI sprite)")]
    private Sprite scoreBoxSprite_;
    
    [Header("Text Settings")]
    [SerializeField]
    [Tooltip("Minimum font size for scores")]
    private float minFontSize_ = 12f;
    
    [SerializeField]
    [Tooltip("Maximum font size for scores")]
    private float maxFontSize_ = 32f;
    
    // Fixed box size (set by LayoutManager)
    private float fixedBoxSize_ = 0f;
    
    // Internal references (created dynamically)
    private GameObject[] playerBoxes_;
    private Image[] backgroundImages_;
    private TextMeshProUGUI[] scoreTexts_;
    
    private PlayerManager playerManager_;
    private int numberOfPlayers_;
    private Color[] playerColors_;
    private bool isInitialized_ = false;

    void Start()
    {
        playerManager_ = PlayerManager.Instance;
        
        if (playerManager_ == null)
        {
            Debug.LogError("ScoreDisplay: PlayerManager not found!");
            enabled = false;
            return;
        }
        
        numberOfPlayers_ = playerManager_.GetNumberOfPlayers();
        playerColors_ = playerManager_.GetPlayerColors();
        
        // Add padding from top of screen
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            float topPadding = 8f;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y - topPadding);
        }
        
        // Create the score grid
        CreateScoreGrid();
        isInitialized_ = true;
    }

    /// <summary>
    /// Create the grid of score boxes dynamically.
    /// Adapts font size for mobile/PC and sizes boxes to fit content.
    /// </summary>
    private void CreateScoreGrid()
    {
        // Get the RectTransform of this GameObject (acts as container)
        RectTransform containerRect = GetComponent<RectTransform>();
        if (containerRect == null)
        {
            Debug.LogError("ScoreDisplay: GameObject must have a RectTransform!");
            return;
        }
        
        // Clear any existing children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        
        // Initialize arrays
        playerBoxes_ = new GameObject[numberOfPlayers_];
        backgroundImages_ = new Image[numberOfPlayers_];
        scoreTexts_ = new TextMeshProUGUI[numberOfPlayers_];
        
        // Calculate grid layout: 1 column for 1-2 players, 2 columns for 3-4 players
        int columns = (numberOfPlayers_ <= 2) ? numberOfPlayers_ : 2;
        int rows = Mathf.CeilToInt((float)numberOfPlayers_ / columns);
        
        // Get container size
        float containerWidth = containerRect.rect.width;
        float containerHeight = containerRect.rect.height;
        
        // Determine if this is mobile or PC based on screen size
        bool isMobile = Screen.width < 800 || Screen.height < 600;
        
        // Calculate adaptive font size based on device and available space
        // For mobile: larger font for readability
        // For PC: can use smaller font
        float baseFontSize = isMobile ? 28f : 24f;
        float calculatedMaxFontSize = Mathf.Min(maxFontSize_, baseFontSize);
        float calculatedMinFontSize = Mathf.Max(minFontSize_, isMobile ? 16f : 12f);
        
        // Use fixed box size if set by LayoutManager, otherwise calculate
        float boxSize;
        if (fixedBoxSize_ > 0f)
        {
            boxSize = fixedBoxSize_;
            // Adjust font size to fit the fixed box
            calculatedMaxFontSize = Mathf.Min(calculatedMaxFontSize, boxSize * 0.4f);
        }
        else
        {
            // Calculate squared box size based on font size
            // Box should be about the size of the font, with minimal padding
            float padding = 4f; // Minimal padding
            boxSize = calculatedMaxFontSize * 2.5f + padding * 2; // ~2.5x font size for 2 lines of text
            
            // Calculate box sizes accounting for spacing between boxes
            float totalHorizontalSpacing = (columns - 1) * spacing_;
            float totalVerticalSpacing = (rows - 1) * spacing_;
            
            // Ensure boxes don't overflow container
            float maxBoxWidthFromContainer = (containerWidth - totalHorizontalSpacing) / columns;
            float maxBoxHeightFromContainer = (containerHeight - totalVerticalSpacing) / rows;
            
            // Use the smallest constraint to keep boxes squared
            float maxBoxSize = Mathf.Min(maxBoxWidthFromContainer, maxBoxHeightFromContainer);
            boxSize = Mathf.Min(boxSize, maxBoxSize);
        }
        
        // Make boxes squared
        float boxWidth = boxSize;
        float boxHeight = boxSize;
        
        for (int i = 0; i < numberOfPlayers_; i++)
        {
            // Create box container
            GameObject boxObj = new GameObject($"Player{i}Box", typeof(RectTransform));
            boxObj.transform.SetParent(transform, false);
            
            RectTransform boxRect = boxObj.GetComponent<RectTransform>();
            
            // Calculate grid position
            int col = i % columns;
            int row = i / columns;
            float xPos = col * (boxWidth + spacing_);
            float yPos = -row * (boxHeight + spacing_);
            
            boxRect.anchorMin = new Vector2(0, 1);
            boxRect.anchorMax = new Vector2(0, 1);
            boxRect.pivot = new Vector2(0, 1);
            boxRect.anchoredPosition = new Vector2(xPos, yPos);
            boxRect.sizeDelta = new Vector2(boxWidth, boxHeight);
            
            // Create background image (colored box)
            Image bgImage = boxObj.AddComponent<Image>();
            
            // Apply sprite with 9-slice for rounded corners (if assigned)
            if (scoreBoxSprite_ != null)
            {
                bgImage.sprite = scoreBoxSprite_;
                bgImage.type = Image.Type.Sliced;
                bgImage.pixelsPerUnitMultiplier = 1f;
            }
            else
            {
                // No sprite - use solid color background (square corners)
                bgImage.color = playerColors_[i];
            }
            
            // Set color (works with or without sprite)
            if (scoreBoxSprite_ != null)
            {
                bgImage.color = playerColors_[i];
            }
            
            backgroundImages_[i] = bgImage;
            
            // Add Outline component for current player highlight
            Outline outline = boxObj.AddComponent<Outline>();
            outline.effectColor = new Color(1, 1, 0, 1f); // Yellow outline
            outline.effectDistance = new Vector2(3, -3); // Offset distance for outline visibility
            outline.useGraphicAlpha = true;
            outline.enabled = false; // Hidden by default
            
            // Create score text
            GameObject textObj = new GameObject("ScoreText", typeof(RectTransform));
            textObj.transform.SetParent(boxObj.transform, false);
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            
            // Adjust padding based on device - minimal padding
            float textPadding = 2f;
            textRect.offsetMin = new Vector2(textPadding, textPadding);
            textRect.offsetMax = new Vector2(-textPadding, -textPadding);
            
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.fontSize = calculatedMaxFontSize;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.color = Color.white;
            textComponent.enableAutoSizing = true;
            textComponent.fontSizeMin = calculatedMinFontSize;
            textComponent.fontSizeMax = calculatedMaxFontSize;
            textComponent.fontStyle = FontStyles.Bold;
            textComponent.outlineColor = Color.black;
            textComponent.outlineWidth = isMobile ? 0.25f : 0.2f; // Thicker outline on mobile for readability
            
            scoreTexts_[i] = textComponent;
            playerBoxes_[i] = boxObj;
        }
        
        Debug.Log($"ScoreDisplay: Created {numberOfPlayers_} score box(es) ({rows}x{columns} grid, {(isMobile ? "mobile" : "PC")} mode, font: {calculatedMinFontSize}-{calculatedMaxFontSize}pt, box: {boxWidth:F0}x{boxHeight:F0}px)");
    }

    void Update()
    {
        if (!isInitialized_ || playerManager_ == null)
        {
            return;
        }
        
        UpdateScoreDisplay();
    }

    /// <summary>
    /// Update the score text and borders for all players.
    /// </summary>
    private void UpdateScoreDisplay()
    {
        int[] scores = playerManager_.GetAllPlayerScores();
        int currentPlayer = playerManager_.GetCurrentPlayerId();
        
        for (int i = 0; i < numberOfPlayers_; i++)
        {
            // Update score text
            if (scoreTexts_[i] != null)
            {
                int score = scores[i];
                
                // Simple format: just the score
                string displayText = $"{score}";
                
                // Add finished indicator
                if (playerManager_.IsPlayerFinished(i))
                {
                    displayText += "\n✓";
                }
                
                scoreTexts_[i].text = displayText;
            }
            
            // Update outline for current player
            if (backgroundImages_ != null && i < backgroundImages_.Length && backgroundImages_[i] != null)
            {
                Outline outline = backgroundImages_[i].GetComponent<Outline>();
                if (outline != null)
                {
                    outline.enabled = (i == currentPlayer);
                }
            }
        }
    }

    /// <summary>
    /// Manually refresh the display (useful after major changes).
    /// </summary>
    public void RefreshDisplay()
    {
        if (isInitialized_)
        {
            UpdateScoreDisplay();
        }
    }
    
    /// <summary>
    /// Public accessor to get the RectTransform of this display.
    /// Use this to position the score display anywhere in the hierarchy.
    /// </summary>
    public RectTransform GetRectTransform()
    {
        return GetComponent<RectTransform>();
    }
    
    /// <summary>
    /// Recreate the grid (useful if parent size changes or number of players changes).
    /// </summary>
    public void RecreateGrid()
    {
        if (playerManager_ != null)
        {
            numberOfPlayers_ = playerManager_.GetNumberOfPlayers();
            playerColors_ = playerManager_.GetPlayerColors();
            CreateScoreGrid();
            isInitialized_ = true;
        }
    }
}
