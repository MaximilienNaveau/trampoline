using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using TMPro;

public class LayoutManagerMultiplayer : MonoBehaviour
{
    [SerializeField] float spacing_ = 8f;
    
    [Header("Header Settings")]
    [SerializeField]
    [Tooltip("Show the title in the header (uncheck to give more space to scores)")]
    private bool showTitle_ = false;
    
    [SerializeField]
    [Tooltip("Minimum font size for player scores (important for mobile readability)")]
    private float minScoreFontSize_ = 12f;
    
    [SerializeField]
    [Tooltip("Maximum font size for player scores")]
    private float maxScoreFontSize_ = 24f;
    
    [Header("Score Box 9-Slice Settings")]
    [SerializeField]
    [Tooltip("Sprite to use for score boxes (should have 9-slice borders configured)")]
    private Sprite scoreBoxSprite_;
    
    [SerializeField]
    [Tooltip("Sprite to use for score box borders (should have 9-slice borders configured)")]
    private Sprite scoreBoxBorderSprite_;

    private RectTransform board_;
    private RectTransform store_;
    private RectTransform header_;
    
    void Awake()
    {
        header_ = (RectTransform)transform.Find("Header");
        board_ = (RectTransform)transform.Find("Board");
        store_ = (RectTransform)transform.Find("Store");
        
        if (header_ == null)
        {
            Debug.LogError("LayoutManager: Header component is missing.");
            throw new System.Exception("LayoutManager: Header component is missing.");
        }
        if (board_ == null)
        {
            Debug.LogError("LayoutManager: Board component is missing.");
            throw new System.Exception("LayoutManager: Board component is missing.");
        }
        if (store_ == null)
        {
            Debug.LogError("LayoutManager: Store component is missing.");
            throw new System.Exception("LayoutManager: Store component is missing.");
        }
        
        // Initialize screen size tracking
        lastScreenSize_ = new Vector2(Screen.width, Screen.height);
        
        // Perform initial layout
        PerformLayout();
    }
    
    void PerformLayout()
    {
        PlaceHeader();
        PlaceBoard();
        PlaceStore();
        CheckSize();
    }

    void PlaceHeader()
    {
        // Set the pivot to [0, 1] (top-left corner)
        header_.pivot = new Vector2(0, 1);

        // Resize the header - slightly taller on mobile for readability
        float headerHeightPercent = Screen.width < 800 ? 0.10f : 0.08f; // 10% on mobile, 8% on desktop
        header_.sizeDelta = new Vector2(
            Screen.width - spacing_ * 2,
            Screen.height * headerHeightPercent);

        // Place the header at the top left of the screen with a padding.
        header_.anchoredPosition = new Vector2(spacing_, -spacing_);

        // Get header elements
        RectTransform title = (RectTransform)header_.Find("Title");
        RectTransform quit = (RectTransform)header_.Find("Quit");
        RectTransform scoreContainer = (RectTransform)header_.Find("Score");

        // Quit button: square on the right
        float quitSize = header_.rect.height - spacing_ * 2;
        if (quit != null)
        {
            quit.sizeDelta = new Vector2(quitSize, quitSize);
            quit.anchorMin = new Vector2(1, 0.5f);
            quit.anchorMax = new Vector2(1, 0.5f);
            quit.pivot = new Vector2(1, 0.5f);
            quit.anchoredPosition = new Vector2(-spacing_, 0);
            
            // Apply 3D styling to quit button
            StyleButton3D(quit, new Color(0.8f, 0.2f, 0.2f, 1f)); // Red color
        }
        
        // Calculate available width for score area
        float availableWidth = header_.rect.width - quitSize - spacing_ * 2;
        float scoreStartX = spacing_;
        
        // Calculate score box size to match quit button
        float scoreBoxSize = quitSize;
        
        // Handle title visibility
        if (title != null)
        {
            if (showTitle_)
            {
                // Give title 20% of available space
                float titleWidth = availableWidth * 0.2f;
                title.sizeDelta = new Vector2(titleWidth, header_.rect.height);
                title.anchorMin = new Vector2(0, 0);
                title.anchorMax = new Vector2(0, 1);
                title.pivot = new Vector2(0, 0.5f);
                title.anchoredPosition = new Vector2(scoreStartX, 0);
                title.gameObject.SetActive(true);
                
                // Adjust score start position
                scoreStartX += titleWidth + spacing_;
            }
            else
            {
                // Hide title to give more space to scores
                title.gameObject.SetActive(false);
            }
        }
        
        // Score container: calculate width based on number of players and box size
        // Layout: 1 column for 1-2 players, 2 columns for 3-4 players
        if (scoreContainer != null)
        {
            int numberOfPlayers = PlayerManager.Instance != null ? PlayerManager.Instance.GetNumberOfPlayers() : 2;
            int columns = (numberOfPlayers <= 2) ? numberOfPlayers : 2;
            float scoreSpacing = 8f;
            float scoreContainerWidth = columns * scoreBoxSize + (columns - 1) * scoreSpacing;
            
            float topPadding = 8f; // Add padding from top
            scoreContainer.sizeDelta = new Vector2(scoreContainerWidth, header_.rect.height - topPadding * 2);
            scoreContainer.anchorMin = new Vector2(0, 0);
            scoreContainer.anchorMax = new Vector2(0, 1);
            scoreContainer.pivot = new Vector2(0, 0.5f);
            scoreContainer.anchoredPosition = new Vector2(scoreStartX, 0);
            
            // Setup ScoreDisplay component and pass the box size
            SetupScoreDisplay(scoreContainer, scoreBoxSize);
        }
    }
    
    /// <summary>
    /// Setup the multiplayer score display in the header score container.
    /// Now the score display creates its own grid automatically.
    /// </summary>
    private void SetupScoreDisplay(RectTransform scoreContainer, float boxSize)
    {
        ScoreDisplay scoreDisplay = scoreContainer.GetComponent<ScoreDisplay>();
        if (scoreDisplay == null)
        {
            scoreDisplay = scoreContainer.gameObject.AddComponent<ScoreDisplay>();
        }
        
        // Set the box size using reflection
        var boxSizeField = typeof(ScoreDisplay).GetField("fixedBoxSize_", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (boxSizeField != null)
        {
            boxSizeField.SetValue(scoreDisplay, boxSize);
        }
        
        // Set the sprites and settings using reflection or direct assignment
        var spacingField = typeof(ScoreDisplay).GetField("spacing_", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (spacingField != null)
        {
            spacingField.SetValue(scoreDisplay, spacing_);
        }
        
        var boxSpriteField = typeof(ScoreDisplay).GetField("scoreBoxSprite_", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (boxSpriteField != null)
        {
            boxSpriteField.SetValue(scoreDisplay, scoreBoxSprite_);
        }
        
        var borderSpriteField = typeof(ScoreDisplay).GetField("scoreBoxBorderSprite_", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (borderSpriteField != null)
        {
            borderSpriteField.SetValue(scoreDisplay, scoreBoxBorderSprite_);
        }
        
        var minFontField = typeof(ScoreDisplay).GetField("minFontSize_", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (minFontField != null)
        {
            minFontField.SetValue(scoreDisplay, minScoreFontSize_);
        }
        
        var maxFontField = typeof(ScoreDisplay).GetField("maxFontSize_", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (maxFontField != null)
        {
            maxFontField.SetValue(scoreDisplay, maxScoreFontSize_);
        }
        
        Debug.Log("LayoutManagerMultiplayer: ScoreDisplay component added and configured");
    }

    void PlaceBoard()
    {
        // Set the pivot to [0, 1] (top-left corner)
        board_.pivot = new Vector2(0, 1);

        // Calculate available height: Screen - Header - Store - Spacings
        float headerHeight = header_.rect.height;
        float storeHeight = store_.rect.height;
        float availableHeight = Screen.height - headerHeight - storeHeight - spacing_ * 4; // 4 spacings: top, after header, after board, after store
        
        // Use the available height for board
        float boardHeight = Mathf.Max(availableHeight, Screen.height * 0.15f); // Minimum 15% of screen

        // Resize the board to fit the width of the screen with a padding.
        board_.sizeDelta = new Vector2(
            Screen.width - spacing_ * 2,
            boardHeight);

        // Place the board below the header with a padding.
        board_.anchoredPosition =
            new Vector2(spacing_, -spacing_ * 2 - headerHeight);
    }

    void PlaceStore()
    {
        // Store size is already set by StoreMultiplayer.ConfigureFixedLayout()
        // Just position it below the board
        
        // Set the pivot to [0, 1] (top-left corner)
        store_.pivot = new Vector2(0, 1);

        // Place the store below the board with a padding.
        float headerHeight = header_.rect.height;
        float boardHeight = board_.rect.height;
        store_.anchoredPosition = new Vector2(
            spacing_,
            -spacing_ * 3 - headerHeight - boardHeight);
    }

    void CheckSize()
    {
        // Note: In multiplayer mode, store size is fixed by StoreMultiplayer
        // So we don't validate total height equals screen height
        float totalHeight = header_.sizeDelta.y + board_.sizeDelta.y + store_.sizeDelta.y + 4 * spacing_;
        Debug.Log($"LayoutManagerMultiplayer: Total height = {totalHeight}, Screen height = {Screen.height}");
    }

    private int lastBoardRowCount_ = -1;
    private Vector2 lastScreenSize_;
    
    void Update()
    {
        // Check for screen size changes
        Vector2 currentScreenSize = new Vector2(Screen.width, Screen.height);
        bool screenSizeChanged = currentScreenSize != lastScreenSize_;
        
        if (screenSizeChanged)
        {
            lastScreenSize_ = currentScreenSize;
            // Perform the overall layout first to resize containers
            PerformLayout();
            // Then notify grids to recalculate their internal layouts based on new sizes
            NotifyGridsToRecalculate();
            return;
        }
        
        // Only update layout when board content changes
        BoardMultiplayer boardScript = board_.GetComponent<BoardMultiplayer>();
        if (boardScript != null)
        {
            int currentRows = boardScript.GetNbRows();
            if (currentRows != lastBoardRowCount_)
            {
                lastBoardRowCount_ = currentRows;
                PerformLayout();
            }
        }
    }
    
    void NotifyGridsToRecalculate()
    {
        // Tell all ScrollableGrid components to recalculate their internal layouts
        BoardMultiplayer boardScript = board_.GetComponent<BoardMultiplayer>();
        StoreMultiplayer storeScript = store_.GetComponent<StoreMultiplayer>();
        
        if (boardScript != null)
        {
            boardScript.RecalculateGridLayout();
        }
        // StoreMultiplayer has fixed layout, no recalculation needed
    }
    
    /// <summary>
    /// Apply 3D button styling to a button GameObject.
    /// </summary>
    private void StyleButton3D(RectTransform buttonRect, Color baseColor)
    {
        GameObject buttonObj = buttonRect.gameObject;
        
        // Get or add Button component
        Button button = buttonObj.GetComponent<Button>();
        if (button == null)
        {
            button = buttonObj.AddComponent<Button>();
        }
        
        // Clear existing children and components
        for (int i = buttonObj.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(buttonObj.transform.GetChild(i).gameObject);
        }
        Image[] existingImages = buttonObj.GetComponents<Image>();
        foreach (Image img in existingImages)
        {
            DestroyImmediate(img);
        }
        
        // Create shadow layer for 3D depth effect
        GameObject shadowObj = new GameObject("Shadow", typeof(RectTransform));
        shadowObj.transform.SetParent(buttonObj.transform, false);
        shadowObj.transform.SetAsFirstSibling(); // Draw behind
        
        RectTransform shadowRect = shadowObj.GetComponent<RectTransform>();
        shadowRect.anchorMin = Vector2.zero;
        shadowRect.anchorMax = Vector2.one;
        shadowRect.offsetMin = new Vector2(0, -4); // Offset down
        shadowRect.offsetMax = new Vector2(4, 0);  // Offset right
        
        Image shadowImage = shadowObj.AddComponent<Image>();
        shadowImage.color = new Color(0f, 0f, 0f, 0.3f); // Semi-transparent black
        
        // Add Outline to shadow for rounded effect
        Outline shadowOutline = shadowObj.AddComponent<Outline>();
        shadowOutline.effectColor = new Color(0f, 0f, 0f, 0.3f);
        shadowOutline.effectDistance = new Vector2(2, -2);
        
        // Create main button background with rounded corners
        GameObject bgObj = new GameObject("Background", typeof(RectTransform));
        bgObj.transform.SetParent(buttonObj.transform, false);
        
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        Image buttonImage = bgObj.AddComponent<Image>();
        buttonImage.color = baseColor;
        
        // Add Outline for rounded corners effect
        Outline bgOutline = bgObj.AddComponent<Outline>();
        bgOutline.effectColor = baseColor;
        bgOutline.effectDistance = new Vector2(1, -1);
        
        // Create highlight layer for 3D top light effect
        GameObject highlightObj = new GameObject("Highlight", typeof(RectTransform));
        highlightObj.transform.SetParent(bgObj.transform, false);
        
        RectTransform highlightRect = highlightObj.GetComponent<RectTransform>();
        highlightRect.anchorMin = new Vector2(0, 0.5f);
        highlightRect.anchorMax = new Vector2(1, 1);
        highlightRect.offsetMin = Vector2.zero;
        highlightRect.offsetMax = Vector2.zero;
        
        Image highlightImage = highlightObj.AddComponent<Image>();
        highlightImage.color = new Color(1f, 1f, 1f, 0.2f); // Semi-transparent white on top half
        
        // Set button target as the background image
        button.targetGraphic = buttonImage;
        button.interactable = true;
        
        // Add CanvasGroup to ensure button is clickable
        CanvasGroup canvasGroup = buttonObj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = buttonObj.AddComponent<CanvasGroup>();
        }
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        // Add visual feedback with lighter/darker colors
        ColorBlock colors = button.colors;
        colors.normalColor = baseColor;
        colors.highlightedColor = new Color(
            Mathf.Min(baseColor.r + 0.1f, 1f),
            Mathf.Min(baseColor.g + 0.1f, 1f),
            Mathf.Min(baseColor.b + 0.1f, 1f),
            1f);
        colors.pressedColor = new Color(
            Mathf.Max(baseColor.r - 0.1f, 0f),
            Mathf.Max(baseColor.g - 0.1f, 0f),
            Mathf.Max(baseColor.b - 0.1f, 0f),
            1f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        button.colors = colors;
        
        // Add X icon if this is the quit button
        if (buttonObj.name == "Quit")
        {
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
            iconObj.transform.SetParent(bgObj.transform, false);
            
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(8, 8);
            iconRect.offsetMax = new Vector2(-8, -8);
            
            TMPro.TextMeshProUGUI iconText = iconObj.AddComponent<TMPro.TextMeshProUGUI>();
            
            // Try to load TextMeshPro default font
            var defaultFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (defaultFont != null)
            {
                iconText.font = defaultFont;
            }
            
            iconText.text = "X"; // Simple X character
            iconText.fontSize = buttonRect.sizeDelta.x * 0.5f;
            iconText.alignment = TMPro.TextAlignmentOptions.Center;
            iconText.color = Color.white;
            iconText.fontStyle = TMPro.FontStyles.Bold;
            iconText.enableWordWrapping = false;
        }
    }
}
