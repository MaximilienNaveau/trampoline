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


  void Start()
  {
    // Configure the NON-scrollable grid layout for multiplayer
    ConfigureFixedLayout();
    
    // Setup background image
    SetupBackgroundImage();
    
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
    
    // Get current player's tokens that should be displayed
    List<BasicToken> displayTokens = new List<BasicToken>();
    foreach (BasicToken token in playerTokens_[currentPlayerId_])
    {
      if (token != null && !token.GetInBoard() && !token.BeingDragged())
      {
        displayTokens.Add(token);
      }
    }
    
    // Sort alphabetically
    displayTokens.Sort((a, b) => a.GetLetters().CompareTo(b.GetLetters()));
    
    // Clear all tiles
    for (int i = 0; i < tiles.Count; i++)
    {
      if (tiles[i].HasToken())
      {
        tiles[i].LetTheTokenGo();
      }
    }
    
    // Assign current player's tokens to tiles
    for (int i = 0; i < displayTokens.Count && i < tiles.Count; i++)
    {
      displayTokens[i].gameObject.SetActive(true);
      tiles[i].AttachToken(displayTokens[i]);
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
      
      // Mark token as not on board - UpdateStorage will handle the rest
      token.SetInBoard(false);
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
