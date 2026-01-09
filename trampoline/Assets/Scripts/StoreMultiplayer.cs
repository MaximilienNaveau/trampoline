using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Assertions;
using TMPro;

/// <summary>
/// Multiplayer version of Store - SINGLE DYNAMIC STORE for all players.
/// Displays current player's tokens and updates color/label when turn changes.
/// Tracks each player's unplayed tokens separately.
/// </summary>
public class StoreMultiplayer : ScrollableGrid, IDropHandler
{
  [Header("Multiplayer Settings")]
  [SerializeField]
  [Tooltip("Maximum tokens this store can hold (default 9)")]
  private int maxTokens_ = 9;
  
  [Header("Visual Settings")]
  [SerializeField]
  [Tooltip("Background image to show current player color")]
  private Image backgroundImage_;
  
  [SerializeField]
  [Tooltip("Text to display current player (e.g., 'Player 1')")]
  private TextMeshProUGUI playerLabel_;
  
  private Button nextTurnButton_;
  
  private TokenPool tokenPool_;
  private TokenDistributor tokenDistributor_;
  private TurnManager turnManager_;
  private PlayerManager playerManager_;
  
  // Track tokens for ALL players (key = playerId, value = list of tokens)
  private Dictionary<int, List<BasicToken>> playerTokens_;
  private int currentPlayerId_ = 0;
  private int numberOfPlayers_ = 2;
  private bool isInitialized_ = false;
  private bool initialTokensDrawn_ = false;
  private Color[] activePlayerColors_;
  private bool needsStorageUpdate_ = false;


  void Start()
  {
    // Configure the NON-scrollable grid layout for multiplayer
    ConfigureFixedLayout();
    
    // Setup background image
    SetupBackgroundImage();
    
    // Create Next Turn button
    CreateNextTurnButton();
    
    // Auto-create player label if not assigned
    CreatePlayerLabelIfNeeded();
    
    // Get required components
    tokenPool_ = FindAnyObjectByType<TokenPool>();
    tokenDistributor_ = FindAnyObjectByType<TokenDistributor>();
    turnManager_ = FindAnyObjectByType<TurnManager>();
    playerManager_ = PlayerManager.Instance;
    
    if (tokenPool_ == null)
    {
      Debug.LogError("StoreMultiplayer: TokenPool not found!");
      throw new System.Exception("StoreMultiplayer: TokenPool not found!");
    }
    
    if (tokenDistributor_ == null)
    {
      Debug.LogError("StoreMultiplayer: TokenDistributor not found!");
      throw new System.Exception("StoreMultiplayer: TokenDistributor not found!");
    }
    
    if (turnManager_ == null)
    {
      Debug.LogError("StoreMultiplayer: TurnManager not found!");
      throw new System.Exception("StoreMultiplayer: TurnManager not found!");
    }

    if (playerManager_ == null)
    {
      Debug.LogError("StoreMultiplayer: PlayerManager not found!");
      throw new System.Exception("StoreMultiplayer: PlayerManager not found!");
    }

    if (backgroundImage_ == null)
    {
      Debug.LogError("StoreMultiplayer: Background Image component not found!");
      throw new System.Exception("StoreMultiplayer: Background Image component not found!");
    }
    
    // Get number of players
    numberOfPlayers_ = turnManager_.GetNumberOfPlayers();
    
    // Initialize active player colors from PlayerManager
    activePlayerColors_ = playerManager_.GetPlayerColors();
    
    if (activePlayerColors_ == null)
    {
      Debug.LogError("StoreMultiplayer: PlayerManager.GetPlayerColors() returned null!");
      throw new System.Exception("StoreMultiplayer: PlayerManager.GetPlayerColors() returned null!");
    }
    
    // Initialize token storage for all players
    playerTokens_ = new Dictionary<int, List<BasicToken>>();
    for (int i = 0; i < numberOfPlayers_; i++)
    {
      playerTokens_[i] = new List<BasicToken>();
    }
    
    // Create tiles directly in the grid
    CreateTilesForStore();
    
    // Get current player
    currentPlayerId_ = turnManager_.GetCurrentPlayerId();
    
    // Subscribe to turn changes
    turnManager_.OnTurnChanged += OnTurnChanged;
    
    // Mark as initialized BEFORE calling UpdateVisualState
    isInitialized_ = true;
    
    // Apply initial visual state
    UpdateVisualState();
    
    // Note: Initial token draw happens in first Update() to ensure TokenDistributor is ready
    
    Debug.Log($"StoreMultiplayer: Initialized for {numberOfPlayers_} players.");
  }

  /// <summary>
  /// Validate that the background image is assigned.
  /// </summary>
  private void SetupBackgroundImage()
  {
    if (backgroundImage_ == null)
    {
      Debug.LogError("StoreMultiplayer: Background Image is not assigned in Inspector!");
      throw new System.Exception("StoreMultiplayer: Background Image is not assigned in Inspector!");
    }
    
    Debug.Log("StoreMultiplayer: Background image assigned and ready.");
  }

  /// <summary>
  /// Configure a fixed-size, non-scrollable grid layout for the store.
  /// Size is exactly: PlayerIndicatorHeight + GridHeight + 2 * padding
  /// </summary>
  private void ConfigureFixedLayout()
  {
    // Use this GameObject as the container
    RectTransform containerRect = GetComponent<RectTransform>();
    
    // Calculate layout parameters
    float padding = spacing_;
    float screenWidth = Screen.width;
    
    // Available width accounts for screen padding on both sides
    float availableWidth = screenWidth - padding * 2;
    
    // Set container to full width
    containerRect.sizeDelta = new Vector2(availableWidth, containerRect.sizeDelta.y);
    
    // Calculate grid dimensions for maxTokens_ tokens
    int gridRows = Mathf.CeilToInt((float)maxTokens_ / cols_);
    
    // Calculate cell size based on 9 columns with internal padding
    // Formula: availableWidth = leftPad + col1 + gap + col2 + ... + col9 + rightPad
    //        = padding + 9*cellSize + 8*padding + padding = 9*cellSize + 10*padding
    float cellSize = (availableWidth - (cols_ + 1) * padding) / cols_;
    
    // Reasonable cell size limits for letter-based game:
    // Min: 40px (readable on phones), Max: 100px (not too large on big screens)
    float minCellSize = 40f;
    float maxCellSize = 100f;
    cellSize = Mathf.Clamp(cellSize, minCellSize, maxCellSize);
    
    // Calculate total heights
    float labelHeight = Screen.height * 0.05f;
    float gridHeight = gridRows * cellSize + (gridRows + 1) * padding;
    float totalHeight = labelHeight + gridHeight + 2 * padding;  // 2 paddings: top and between
    
    // Set container height to exact fit
    containerRect.sizeDelta = new Vector2(availableWidth, totalHeight);
    
    // Create the content container with GridLayoutGroup (no ScrollRect)
    GameObject contentObject = new GameObject("Content", typeof(RectTransform));
    contentObject.transform.SetParent(transform, false);
    content_ = contentObject.GetComponent<RectTransform>();
    content_.anchorMin = new Vector2(0, 1);
    content_.anchorMax = new Vector2(1, 1);
    content_.pivot = new Vector2(0.5f, 1);
    
    // Position content just below the label area (labelHeight + padding from top)
    float contentYOffset = -(labelHeight + padding);
    content_.anchoredPosition = new Vector2(0, contentYOffset);
    content_.sizeDelta = new Vector2(0, gridHeight);
    
    // Add the GridLayoutGroup
    grid_ = contentObject.AddComponent<GridLayoutGroup>();
    
    // Configure GridLayoutGroup
    grid_.cellSize = new Vector2(cellSize, cellSize);
    grid_.spacing = new Vector2(padding, padding);
    grid_.padding = new RectOffset(
      (int)padding,  // left
      (int)padding,  // right
      (int)padding,  // top
      (int)padding); // bottom
    grid_.startCorner = GridLayoutGroup.Corner.UpperLeft;
    grid_.startAxis = GridLayoutGroup.Axis.Horizontal;
    grid_.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
    grid_.constraintCount = cols_;
    grid_.childAlignment = TextAnchor.UpperCenter;
    
    Debug.Log($"StoreMultiplayer: Fixed layout - Width: {availableWidth}px, Cell: {cellSize}px, Label: {labelHeight}px, Grid: {gridHeight}px, Total: {totalHeight}px");
  }

  /// <summary>
  /// Create a rounded rectangle sprite for buttons.
  /// </summary>
  private Sprite CreateRoundedRectSprite(int width, int height, int cornerRadius)
  {
    Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
    Color[] pixels = new Color[width * height];
    
    for (int y = 0; y < height; y++)
    {
      for (int x = 0; x < width; x++)
      {
        // Calculate distance from corners
        bool inCornerRegion = false;
        float distanceToCorner = 0f;
        
        // Top-left corner
        if (x < cornerRadius && y >= height - cornerRadius)
        {
          float dx = cornerRadius - x;
          float dy = y - (height - cornerRadius);
          distanceToCorner = Mathf.Sqrt(dx * dx + dy * dy);
          inCornerRegion = true;
        }
        // Top-right corner
        else if (x >= width - cornerRadius && y >= height - cornerRadius)
        {
          float dx = x - (width - cornerRadius);
          float dy = y - (height - cornerRadius);
          distanceToCorner = Mathf.Sqrt(dx * dx + dy * dy);
          inCornerRegion = true;
        }
        // Bottom-left corner
        else if (x < cornerRadius && y < cornerRadius)
        {
          float dx = cornerRadius - x;
          float dy = cornerRadius - y;
          distanceToCorner = Mathf.Sqrt(dx * dx + dy * dy);
          inCornerRegion = true;
        }
        // Bottom-right corner
        else if (x >= width - cornerRadius && y < cornerRadius)
        {
          float dx = x - (width - cornerRadius);
          float dy = cornerRadius - y;
          distanceToCorner = Mathf.Sqrt(dx * dx + dy * dy);
          inCornerRegion = true;
        }
        
        // Set pixel: white if inside shape, transparent if outside
        if (inCornerRegion)
        {
          pixels[y * width + x] = distanceToCorner <= cornerRadius ? Color.white : Color.clear;
        }
        else
        {
          pixels[y * width + x] = Color.white;
        }
      }
    }
    
    texture.SetPixels(pixels);
    texture.Apply();
    
    return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
  }

  /// <summary>
  /// Create a Next Turn button next to the player label.
  /// </summary>
  private void CreateNextTurnButton()
  {
    // Create button GameObject as child of store
    GameObject buttonObj = new GameObject("NextTurnButton", typeof(RectTransform));
    buttonObj.transform.SetParent(transform, false);
    
    RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
    
    // Calculate button size based on label height
    float labelHeight = Screen.height * 0.05f;
    float buttonSize = labelHeight * 0.8f;
    float padding = spacing_;
    
    // Position button at top-right, aligned with player label
    buttonRect.anchorMin = new Vector2(1, 1);
    buttonRect.anchorMax = new Vector2(1, 1);
    buttonRect.pivot = new Vector2(1, 1);
    buttonRect.anchoredPosition = new Vector2(-padding, -padding); // Top-right with padding
    buttonRect.sizeDelta = new Vector2(buttonSize, buttonSize);
    
    // Add Canvas to ensure button is on top and receives clicks
    Canvas buttonCanvas = buttonObj.AddComponent<Canvas>();
    buttonCanvas.overrideSorting = true;
    buttonCanvas.sortingOrder = 100;
    
    // Add GraphicRaycaster to enable click detection
    buttonObj.AddComponent<GraphicRaycaster>();
    
    // Add Button component
    nextTurnButton_ = buttonObj.AddComponent<Button>();
    
    // Create rounded sprite for button
    Sprite roundedSprite = CreateRoundedRectSprite(128, 128, 24);
    
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
    shadowImage.sprite = roundedSprite;
    shadowImage.color = new Color(0f, 0f, 0f, 0.3f); // Semi-transparent black
    shadowImage.raycastTarget = false;
    
    // Create main button background with rounded corners
    GameObject bgObj = new GameObject("Background", typeof(RectTransform));
    bgObj.transform.SetParent(buttonObj.transform, false);
    
    RectTransform bgRect = bgObj.GetComponent<RectTransform>();
    bgRect.anchorMin = Vector2.zero;
    bgRect.anchorMax = Vector2.one;
    bgRect.offsetMin = Vector2.zero;
    bgRect.offsetMax = Vector2.zero;
    
    Image buttonImage = bgObj.AddComponent<Image>();
    buttonImage.sprite = roundedSprite;
    buttonImage.color = new Color(0.2f, 0.8f, 0.2f, 1f); // Green
    buttonImage.raycastTarget = true;
    
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
    highlightImage.sprite = roundedSprite;
    highlightImage.color = new Color(1f, 1f, 1f, 0.2f); // Semi-transparent white on top half
    highlightImage.raycastTarget = false;
    // Set button target as the background image
    nextTurnButton_.targetGraphic = buttonImage;
    nextTurnButton_.interactable = true;
    
    // Add visual feedback
    ColorBlock colors = nextTurnButton_.colors;
    colors.normalColor = new Color(0.25f, 0.85f, 0.25f, 1f);      // Bright green
    colors.highlightedColor = new Color(0.35f, 0.95f, 0.35f, 1f); // Lighter green
    colors.pressedColor = new Color(0.15f, 0.65f, 0.15f, 1f);     // Darker green
    colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);     // Gray
    nextTurnButton_.colors = colors;
    
    // Create checkmark icon
    GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
    iconObj.transform.SetParent(buttonObj.transform, false);
    
    RectTransform iconRect = iconObj.GetComponent<RectTransform>();
    iconRect.anchorMin = Vector2.zero;
    iconRect.anchorMax = Vector2.one;
    iconRect.offsetMin = new Vector2(8, 8);
    iconRect.offsetMax = new Vector2(-8, -8);
    
    TextMeshProUGUI iconText = iconObj.AddComponent<TextMeshProUGUI>();
    
    // Try to load TextMeshPro default font
    var defaultFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    if (defaultFont != null)
    {
        iconText.font = defaultFont;
    }
    
    iconText.text = ">"; // Simple text that's guaranteed to be in the font
    iconText.fontSize = buttonSize * 0.5f;
    iconText.alignment = TextAlignmentOptions.Center;
    iconText.color = Color.white;
    iconText.fontStyle = FontStyles.Bold;
    iconText.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
    iconText.raycastTarget = false; // Don't block button clicks
    
    // Hook up button click
    nextTurnButton_.onClick.AddListener(OnNextTurnClicked);
    
    Debug.Log($"StoreMultiplayer: Created Next Turn button next to player label, size: {buttonSize}px, font: {(defaultFont != null ? "LiberationSans SDF" : "default")}");
  }
  
  /// <summary>
  /// Handle Next Turn button click.
  /// </summary>
  private void OnNextTurnClicked()
  {
    Debug.Log("=== Next Turn button CLICKED ===");
    
    if (turnManager_ == null)
    {
      Debug.LogError("Next Turn button clicked but TurnManager is null!");
      return;
    }
    
    Debug.Log($"Calling EndCurrentTurn on TurnManager (current player: {turnManager_.GetCurrentPlayerId()})");
    turnManager_.EndCurrentTurn();
    Debug.Log("EndCurrentTurn called successfully");
  }

  /// <summary>
  /// Create tiles for the store without using ScrollableGrid's ResizeGrid.
  /// </summary>
  private void CreateTilesForStore()
  {
    if (grid_ == null || tilePrefab_ == null)
    {
      Debug.LogError("StoreMultiplayer: Cannot create tiles - grid or tilePrefab is null");
      return;
    }
    
    // Create maxTokens_ tiles
    for (int i = 0; i < maxTokens_; i++)
    {
      GameObject tileObject = Instantiate(tilePrefab_, grid_.transform);
      tileObject.name = $"Tile_{i}";
    }
    
    Debug.Log($"StoreMultiplayer: Created {maxTokens_} tiles");
  }

  /// <summary>
  /// Initialize the active player colors array based on number of players.
  /// </summary>
  /// <summary>
  /// Auto-create the player label TextMeshProUGUI if not assigned in Inspector.
  /// </summary>
  private void CreatePlayerLabelIfNeeded()
  {
    if (playerLabel_ != null)
    {
      // Already assigned in Inspector
      return;
    }
    
    // Create a new GameObject for the label
    GameObject labelObject = new GameObject("PlayerLabel", typeof(RectTransform));
    labelObject.transform.SetParent(transform, false);
    
    // Add TextMeshProUGUI component
    playerLabel_ = labelObject.AddComponent<TextMeshProUGUI>();
    
    // Calculate label height as 5% of screen height
    float labelHeight = Screen.height * 0.05f;
    float padding = spacing_;
    
    // Configure the RectTransform for positioning at the top
    RectTransform labelRect = labelObject.GetComponent<RectTransform>();
    labelRect.anchorMin = new Vector2(0, 1);
    labelRect.anchorMax = new Vector2(1, 1);
    labelRect.pivot = new Vector2(0.5f, 1);
    labelRect.anchoredPosition = new Vector2(0, -padding);  // padding from top
    labelRect.sizeDelta = new Vector2(-padding * 2, labelHeight);  // scale with screen, padding from sides
    
    // Configure text appearance - font size scales with label height
    playerLabel_.text = "Player 1";
    playerLabel_.fontSize = labelHeight * 0.6f;  // 60% of label height
    playerLabel_.alignment = TextAlignmentOptions.Center;
    playerLabel_.color = new Color(0.6f, 0.3f, 0.3f, 1f);  // Darker red for Player 1 initially
    playerLabel_.fontStyle = FontStyles.Bold;
    playerLabel_.enableAutoSizing = true;
    playerLabel_.fontSizeMin = 12;
    playerLabel_.fontSizeMax = labelHeight * 0.8f;
    
    // Add black outline for better readability against colored backgrounds
    // Use material properties to set outline
    if (playerLabel_.fontMaterial != null)
    {
        playerLabel_.fontMaterial.EnableKeyword("OUTLINE_ON");
        playerLabel_.fontMaterial.SetColor("_OutlineColor", Color.black);
        playerLabel_.fontMaterial.SetFloat("_OutlineWidth", 0.15f);
    }
    
    Debug.Log($"StoreMultiplayer: Auto-created player label with height {labelHeight}px.");
  }

  void OnDestroy()
  {
    if (turnManager_ != null)
    {
      turnManager_.OnTurnChanged -= OnTurnChanged;
    }
  }

  void Update()
  {
    // Draw initial tokens on first update (after TokenDistributor is initialized)
    if (!initialTokensDrawn_ && isInitialized_)
    {
      DrawNewTokensForPlayer(currentPlayerId_);
      initialTokensDrawn_ = true;
      UpdateStorage();
    }
    
    // Update storage if needed (after drag operations complete)
    if (needsStorageUpdate_)
    {
      needsStorageUpdate_ = false;
      UpdateStorage();
    }
  }

  /// <summary>
  /// Called when the turn changes to a new player.
  /// Updates visuals and displays new player's tokens.
  /// </summary>
  private void OnTurnChanged(int newPlayerId)
  {
    currentPlayerId_ = newPlayerId;
    
    // If new player has no tokens, draw them
    if (playerTokens_[currentPlayerId_].Count == 0)
    {
      DrawNewTokensForPlayer(currentPlayerId_);
    }
    
    // Update visuals
    UpdateVisualState();

    // Update storage display
    UpdateStorage();
    
    Debug.Log($"StoreMultiplayer: Switched to Player {currentPlayerId_ + 1}");
  }

  /// <summary>
  /// Draw new tokens for a specific player.
  /// </summary>
  private void DrawNewTokensForPlayer(int playerId)
  {
    if (tokenDistributor_ == null || !playerTokens_.ContainsKey(playerId))
    {
      return;
    }
    
    // Return any existing tokens for this player first
    if (playerTokens_[playerId].Count > 0)
    {
      tokenDistributor_.ReturnTokensFromPlayer(playerId);
      playerTokens_[playerId].Clear();
    }
    
    // Draw new tokens
    List<BasicToken> newTokens = tokenDistributor_.DrawTokensForPlayer(playerId, maxTokens_);
    playerTokens_[playerId] = newTokens;
    
    Debug.Log($"StoreMultiplayer: Player {playerId + 1} drew {newTokens.Count} tokens.");
  }

  /// <summary>
  /// Update the storage display with current player's tokens.
  /// </summary>
  public void UpdateStorage()
  {
    if (!isInitialized_ || !playerTokens_.ContainsKey(currentPlayerId_))
    {
      return;
    }
    
    List<Tile> tiles = GetTiles();
    
    // Hide all tokens from ALL players that are NOT on the board
    foreach (var playerTokenList in playerTokens_.Values)
    {
      foreach (BasicToken token in playerTokenList)
      {
        if (token != null && !token.GetInBoard())
        {
          token.gameObject.SetActive(false);
        }
      }
    }
    
    // Get current player's tokens that should be displayed in store
    List<BasicToken> displayTokens = new List<BasicToken>();
    foreach (BasicToken token in playerTokens_[currentPlayerId_])
    {
      if (token != null && !token.GetInBoard())
      {
        displayTokens.Add(token);
      }
    }
    
    // Sort alphabetically
    displayTokens.Sort((a, b) => a.GetLetters().CompareTo(b.GetLetters()));
    
    // Clear all tiles and fully detach any tokens
    for (int i = 0; i < tiles.Count; i++)
    {
      if (tiles[i].HasToken())
      {
        BasicToken tileToken = tiles[i].GetToken();
        tiles[i].LetTheTokenGo();
        // Ensure token is properly detached
        if (tileToken != null)
        {
          tileToken.gameObject.SetActive(false);
        }
      }
    }
    
    // Assign current player's tokens to tiles and make them visible
    for (int i = 0; i < displayTokens.Count && i < tiles.Count; i++)
    {
      BasicToken token = displayTokens[i];
      token.gameObject.SetActive(true);
      
      // AttachToken already calls SwapTileUnder internally, so we don't need to call it twice
      tiles[i].AttachToken(token);
      
      Debug.Log($"StoreMultiplayer.UpdateStorage: Attached token '{token.GetLetters()}' to tile {i} '{tiles[i].name}', Parent={token.transform.parent?.name}, Position={((RectTransform)token.transform).anchoredPosition}");
    }
  }

  /// <summary>
  /// Update visual state: color background and label for current player.
  /// </summary>
  private void UpdateVisualState()
  {
    if (!isInitialized_ || activePlayerColors_ == null)
    {
      Debug.LogWarning($"StoreMultiplayer: UpdateVisualState skipped - initialized: {isInitialized_}, colors null: {activePlayerColors_ == null}");
      return;
    }
    
    // Update background color
    if (backgroundImage_ != null && currentPlayerId_ < activePlayerColors_.Length)
    {
      Color newColor = activePlayerColors_[currentPlayerId_];
      backgroundImage_.color = newColor;
      Debug.Log($"StoreMultiplayer: Set background color to {newColor} for Player {currentPlayerId_ + 1}");
    }
    else
    {
      Debug.LogWarning($"StoreMultiplayer: Cannot update background - image null: {backgroundImage_ == null}, playerId: {currentPlayerId_}, colors length: {activePlayerColors_?.Length}");
    }
    
    // Update player label text and color
    if (playerLabel_ != null && currentPlayerId_ < activePlayerColors_.Length)
    {
      playerLabel_.text = $"Player {currentPlayerId_ + 1}";
      
      // Use darker version of player color for text
      Color playerColor = activePlayerColors_[currentPlayerId_];
      Color darkerColor = new Color(
        playerColor.r * 0.6f,  // 60% brightness
        playerColor.g * 0.6f,
        playerColor.b * 0.6f,
        1f  // Full opacity
      );
      playerLabel_.color = darkerColor;
    }
  }

  public void OnDrop(PointerEventData eventData)
  {
    if (eventData.pointerDrag != null)
    {
      BasicToken token = eventData.pointerDrag.GetComponent<BasicToken>();
      if (token == null)
      {
        return;
      }
      
      // Only allow returning tokens that belong to current player
      if (!playerTokens_.ContainsKey(currentPlayerId_) || 
        !playerTokens_[currentPlayerId_].Contains(token))
      {
        Debug.Log($"StoreMultiplayer: Token does not belong to Player {currentPlayerId_ + 1}.");
        return;
      }
      
      Debug.Log($"StoreMultiplayer.OnDrop: Token '{token.GetLetters()}' dropped. InBoard={token.GetInBoard()}, TileUnder={token.GetTileUnder()}, Parent={token.transform.parent?.name}");
      
      // Mark token as not on board
      token.SetInBoard(false);
      
      // Force token to detach from its current tile
      if (token.GetTileUnder() != null)
      {
        Tile oldTile = token.GetTileUnder();
        oldTile.LetTheTokenGo();
        // Clear the token's tile reference too (important!)
        token.SwapTileUnder(null);
        Debug.Log($"StoreMultiplayer.OnDrop: Detached token from tile '{oldTile.name}'");
      }
      
      Debug.Log($"StoreMultiplayer.OnDrop: Before UpdateStorage - Parent={token.transform.parent?.name}, Active={token.gameObject.activeSelf}");
      
      // Update storage immediately to reposition the token BEFORE OnEndDrag completes
      // This ensures the token is properly attached to a store tile
      UpdateStorage();
      
      Debug.Log($"StoreMultiplayer.OnDrop: After UpdateStorage - TileUnder={token.GetTileUnder()?.name}, Parent={token.transform.parent?.name}, Position={((RectTransform)token.transform).anchoredPosition}");
      
      // Mark this as successfully dropped on a tile so OnEndDrag doesn't reset position
      token.SetDraggedOnTile(true);
    }
  }

  /// <summary>
  /// Get the number of tokens currently in storage for current player.
  /// </summary>
  public int GetTokenCount()
  {
    if (!playerTokens_.ContainsKey(currentPlayerId_))
    {
      return 0;
    }
    
    int count = 0;
    foreach (BasicToken token in playerTokens_[currentPlayerId_])
    {
      if (token != null && !token.GetInBoard())
      {
        count++;
      }
    }
    return count;
  }

  /// <summary>
  /// Check if a token belongs to the current player.
  /// </summary>
  public bool OwnsToken(BasicToken token)
  {
    return playerTokens_.ContainsKey(currentPlayerId_) && 
         playerTokens_[currentPlayerId_].Contains(token);
  }

  /// <summary>
  /// Get the current player ID being displayed.
  /// </summary>
  public int GetCurrentPlayerId()
  {
    return currentPlayerId_;
  }

  /// <summary>
  /// Get all tokens belonging to the current player.
  /// </summary>
  public List<BasicToken> GetCurrentPlayerTokens()
  {
    if (!playerTokens_.ContainsKey(currentPlayerId_))
    {
      return new List<BasicToken>();
    }
    return new List<BasicToken>(playerTokens_[currentPlayerId_]);
  }

  /// <summary>
  /// Get tokens for a specific player.
  /// </summary>
  public List<BasicToken> GetPlayerTokens(int playerId)
  {
    if (!playerTokens_.ContainsKey(playerId))
    {
      return new List<BasicToken>();
    }
    return new List<BasicToken>(playerTokens_[playerId]);
  }

  /// <summary>
  /// Check if current player can interact with the store.
  /// </summary>
  public bool CanInteract()
  {
    return turnManager_ != null && turnManager_.IsPlayerTurn(currentPlayerId_);
  }

  /// <summary>
  /// Get the color for a specific player.
  /// </summary>
  public Color GetPlayerColor(int playerId)
  {
    if (activePlayerColors_ != null && playerId >= 0 && playerId < activePlayerColors_.Length)
    {
      return activePlayerColors_[playerId];
    }
    return Color.white;
  }

  /// <summary>
  /// Get all player colors array.
  /// </summary>
  public Color[] GetPlayerColors()
  {
    return activePlayerColors_;
  }

  /// <summary>
  /// Get the total height of the store (for layout calculations).
  /// </summary>
  public float GetStoreHeight()
  {
    RectTransform rect = GetComponent<RectTransform>();
    return rect != null ? rect.rect.height : 0f;
  }
}
