using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Multiplayer version of Board that tracks which player owns which row.
/// Adds visual indicators to show player ownership of words.
/// </summary>
public class BoardMultiplayer : ScrollableGrid
{
    [Header("Multiplayer Settings")]
    [SerializeField]
    [Tooltip("Prefab for player indicator (placed at start of each row)")]
    private GameObject playerIndicatorPrefab_;
    
    [SerializeField]
    [Tooltip("Player colors for row ownership visualization")]
    private Color[] playerColors_ = new Color[] {
        new Color(1f, 0.5f, 0.5f, 1f),  // Red
        new Color(0.5f, 0.5f, 1f, 1f),  // Blue
        new Color(0.5f, 1f, 0.5f, 1f),  // Green
        new Color(1f, 1f, 0.5f, 1f)     // Yellow
    };
    
    private TokenPool tokenPool_;
    private TurnManager turnManager_;
    private PlayerManager playerManager_;
    private int[] rowOwnership_;  // -1 = unowned, 0-3 = player ID
    private List<Image> rowIndicators_;  // Visual indicators for each row
    private RowLetterHistory[] rowHistories_;  // Track letter placement history for progressive scoring
    private List<GameObject> wordOutlines_;  // Visual outlines around complete words

    private void Start()
    {
        // Configure the scrollable grid layout
        ConfigureLayout();
        
        // Get the GridLayoutGroup component
        ClearGrid();
        
        // Create 13 lines of tiles from the start (full game board)
        ResizeGrid(13);
        if (GetNbRows() != 13)
        {
            Debug.LogError($"BoardMultiplayer: Expected 13 rows but found {GetNbRows()}.");
            throw new System.Exception($"BoardMultiplayer: Expected 13 rows but found {GetNbRows()}.");
        }
        
        // Get the token pool reference
        tokenPool_ = FindAnyObjectByType<TokenPool>();
        if (tokenPool_ == null)
        {
            Debug.LogError("BoardMultiplayer: TokenPool is null.");
            throw new System.Exception("BoardMultiplayer: TokenPool is null.");
        }
        
        // Get turn manager
        turnManager_ = FindAnyObjectByType<TurnManager>();
        if (turnManager_ == null)
        {
            Debug.LogError("BoardMultiplayer: TurnManager is null.");
            throw new System.Exception("BoardMultiplayer: TurnManager is null.");
        }
        
        // Get player manager
        playerManager_ = FindAnyObjectByType<PlayerManager>();
        if (playerManager_ == null)
        {
            Debug.LogError("BoardMultiplayer: PlayerManager is null.");
            throw new System.Exception("BoardMultiplayer: PlayerManager is null.");
        }
        
        // Initialize row ownership tracking
        rowOwnership_ = new int[rows_];
        rowIndicators_ = new List<Image>();
        rowHistories_ = new RowLetterHistory[rows_];
        wordOutlines_ = new List<GameObject>();
        for (int i = 0; i < rows_; i++)
        {
            rowOwnership_[i] = -1;  // -1 means unowned
            rowHistories_[i] = new RowLetterHistory(i);
        }
        
        Debug.Log("BoardMultiplayer: Initialized.");
    }

    /// <summary>
    /// Get list of words on the board with their owner information.
    /// </summary>
    public List<WordWithOwner> GetListOfWordsWithOwners()
    {
        List<WordWithOwner> listOfWords = new List<WordWithOwner>();
        listOfWords.Clear();
        
        List<Tile> tiles = GetTiles();
        if (tiles.Count == 0)
        {
            return listOfWords;
        }
        
        // Process each row
        for (int row = 0; row < GetNbRows(); row++)
        {
            WordWithOwner word = new WordWithOwner
            {
                word_ = "",
                nb_green_letters_ = 0,
                playerId_ = rowOwnership_[row],
                rowIndex_ = row
            };
            
            for (int col = 0; col < cols_; col++)
            {
                int i = row * cols_ + col;
                if (i >= tiles.Count || !tiles[i].HasToken())
                {
                    break;
                }
                
                word.word_ += tiles[i].GetToken().GetLetter();
                if (tiles[i].GetToken().IsOnGreenFace())
                {
                    word.nb_green_letters_++;
                }
            }
            
            if (word.word_ != "")
            {
                listOfWords.Add(word);
            }
        }
        
        return listOfWords;
    }

    /// <summary>
    /// Get list of words (without owner info) for compatibility.
    /// </summary>
    public List<Word> GetListOfWords()
    {
        List<WordWithOwner> wordsWithOwners = GetListOfWordsWithOwners();
        List<Word> words = new List<Word>();
        
        foreach (WordWithOwner wordWithOwner in wordsWithOwners)
        {
            words.Add(new Word
            {
                word_ = wordWithOwner.word_,
                nb_green_letters_ = wordWithOwner.nb_green_letters_
            });
        }
        
        return words;
    }

    /// <summary>
    /// Set the owner of a specific row.
    /// </summary>
    public void SetRowOwner(int rowIndex, int playerId)
    {
        if (rowIndex < 0 || rowIndex >= rowOwnership_.Length)
        {
            Debug.LogError($"BoardMultiplayer: Invalid row index {rowIndex}.");
            return;
        }
        
        if (playerId < -1 || playerId >= 4)
        {
            Debug.LogError($"BoardMultiplayer: Invalid player ID {playerId}.");
            return;
        }
        
        rowOwnership_[rowIndex] = playerId;
        UpdateRowVisuals(rowIndex);
        
        Debug.Log($"BoardMultiplayer: Row {rowIndex} now owned by Player {playerId + 1}.");
    }

    /// <summary>
    /// Get the owner of a specific row.
    /// </summary>
    public int GetRowOwner(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= rowOwnership_.Length)
        {
            return -1;
        }
        return rowOwnership_[rowIndex];
    }

    /// <summary>
    /// Check if a row is owned by a specific player.
    /// </summary>
    public bool IsRowOwnedBy(int rowIndex, int playerId)
    {
        if (rowIndex < 0 || rowIndex >= rowOwnership_.Length)
        {
            return false;
        }
        return rowOwnership_[rowIndex] == playerId;
    }

    /// <summary>
    /// Update visual indicators for a row based on ownership.
    /// </summary>
    private void UpdateRowVisuals(int rowIndex)
    {
        int playerId = rowOwnership_[rowIndex];
        
        if (playerId < 0)
        {
            // No owner - no special visuals
            return;
        }
        
        // Visual feedback removed - tiles remain their default color
        // Players can identify their rows by the token colors on the board
    }

    /// <summary>
    /// Automatically detect and set row ownership based on token placement.
    /// Called when tokens are placed on the board.
    /// </summary>
    public void UpdateRowOwnership()
    {
        if (turnManager_ == null)
        {
            return;
        }
        
        int currentPlayer = turnManager_.GetCurrentPlayerId();
        List<Tile> tiles = GetTiles();
        
        // Check each row
        for (int row = 0; row < GetNbRows(); row++)
        {
            // If row is already owned, skip
            if (rowOwnership_[row] >= 0)
            {
                continue;
            }
            
            // Check if this row has any tokens
            bool hasTokens = false;
            for (int col = 0; col < cols_; col++)
            {
                int tileIndex = row * cols_ + col;
                if (tileIndex < tiles.Count && tiles[tileIndex].HasToken())
                {
                    hasTokens = true;
                    break;
                }
            }
            
            // If row has tokens and is unowned, assign to current player
            if (hasTokens)
            {
                SetRowOwner(row, currentPlayer);
            }
        }
    }

    /// <summary>
    /// Get words owned by a specific player.
    /// </summary>
    public List<WordWithOwner> GetPlayerWords(int playerId)
    {
        List<WordWithOwner> allWords = GetListOfWordsWithOwners();
        List<WordWithOwner> playerWords = new List<WordWithOwner>();
        
        foreach (WordWithOwner word in allWords)
        {
            if (word.playerId_ == playerId)
            {
                playerWords.Add(word);
            }
        }
        
        return playerWords;
    }

    /// <summary>
    /// Count how many complete words (9 letters) a player has.
    /// </summary>
    public int GetPlayerCompleteWordCount(int playerId)
    {
        List<WordWithOwner> playerWords = GetPlayerWords(playerId);
        int completeCount = 0;
        
        foreach (WordWithOwner word in playerWords)
        {
            if (word.word_.Length == 9)
            {
                completeCount++;
            }
        }
        
        return completeCount;
    }

    private bool IsRowEmpty(int rowIndex)
    {
        for (int col = 0; col < cols_; col++)
        {
            int tileIndex = rowIndex * cols_ + col;
            if (tileIndex < grid_.transform.childCount)
            {
                if (grid_.transform.GetChild(tileIndex).GetComponent<Tile>().HasToken())
                {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Resizes the board grid to maintain proper dimensions and ensure structural constraints.
    /// The method enforces the following rules:
    /// - Minimum of 2 rows in the grid
    /// - At least one empty row at the bottom
    /// - Removes excess empty rows while maintaining minimum requirements
    /// - Grid size never exceeds the maximum defined by rows_
    /// - Preserves row ownership information when resizing
    /// </summary>
    public bool ResizeBoardGrid()
    {
        bool hasResized = false;
        
        // Ensure there are at least 2 rows
        if (GetNbRows() < 2)
        {
            ResizeGrid(2);
            hasResized = true;
        }
        else if (GetNbRows() == 2)
        {
            // With 2 rows, add a third if the last row is not empty
            if (!IsRowEmpty(GetNbRows() - 1))
            {
                AddNewRow();
                hasResized = true;
            }
        } 
        else // GetNbRows() > 2
        {
            // Remove excess empty rows, keeping at least one empty row and minimum 2 rows total
            while (GetNbRows() > 2 && IsRowEmpty(GetNbRows() - 1) && IsRowEmpty(GetNbRows() - 2))
            {
                RemoveLastRow();
                hasResized = true;
            }
            // Add a row if the last row is not empty (and we haven't reached max)
            if(GetNbRows() < rows_ && !IsRowEmpty(GetNbRows() - 1))
            {
                AddNewRow();
                hasResized = true;
            }
        }

        // Update ownership array if grid was resized
        if (hasResized)
        {
            int currentRows = GetNbRows();
            if (rowOwnership_.Length != currentRows)
            {
                int[] newOwnership = new int[currentRows];
                for (int i = 0; i < currentRows; i++)
                {
                    newOwnership[i] = i < rowOwnership_.Length ? rowOwnership_[i] : -1;
                }
                rowOwnership_ = newOwnership;
            }
        }

        if (grid_.transform.childCount % cols_ != 0)
        {
            Debug.LogError($"BoardMultiplayer: Child count ({grid_.transform.childCount}) is not divisible by columns ({cols_}).");
            throw new System.Exception($"BoardMultiplayer: Child count ({grid_.transform.childCount}) is not divisible by columns ({cols_}).");
        }
        if (GetNbRows() < 2)
        {
            Debug.LogError($"BoardMultiplayer: Number of rows ({GetNbRows()}) is less than minimum (2).");
            throw new System.Exception($"BoardMultiplayer: Number of rows ({GetNbRows()}) is less than minimum (2).");
        }
        if (GetNbRows() > rows_)
        {
            Debug.LogError($"BoardMultiplayer: Number of rows ({GetNbRows()}) exceeds maximum ({rows_}).");
            throw new System.Exception($"BoardMultiplayer: Number of rows ({GetNbRows()}) exceeds maximum ({rows_}).");
        }
        
        return hasResized;
    }
    
    /// <summary>
    /// Assign position scores to new tokens placed in rows.
    /// Called when a turn is validated to mark which letters are new.
    /// </summary>
    public void AssignPositionScoresToNewTokens(int playerId)
    {
        if (playerManager_ == null)
        {
            return;
        }
        
        Color playerColor = playerManager_.GetPlayerColor(playerId);
        List<Tile> tiles = GetTiles();
        
        // Process each row
        for (int row = 0; row < GetNbRows(); row++)
        {
            // Get tokens in this row
            List<BasicToken> tokensInRow = new List<BasicToken>();
            
            for (int col = 0; col < cols_; col++)
            {
                int tileIndex = row * cols_ + col;
                if (tileIndex < tiles.Count)
                {
                    Tile tile = tiles[tileIndex];
                    if (tile.HasToken())
                    {
                        tokensInRow.Add(tile.GetToken());
                    }
                }
            }
            
            // Assign scores to new tokens
            if (tokensInRow.Count > 0)
            {
                int newTokens = rowHistories_[row].AssignPositionScoresToNewTokens(tokensInRow, playerId, playerColor);
                if (newTokens > 0)
                {
                    Debug.Log($"BoardMultiplayer: Assigned position scores to {newTokens} new tokens in row {row} for Player {playerId + 1}");
                }
            }
        }
    }
    
    /// <summary>
    /// Assign position scores to tokens in real-time as they're placed.
    /// This provides immediate score feedback during the turn.
    /// </summary>
    private void AssignPositionScoresToUnassignedTokens(int playerId)
    {
        if (playerManager_ == null)
        {
            return;
        }
        
        Color playerColor = playerManager_.GetPlayerColor(playerId);
        List<Tile> tiles = GetTiles();
        
        // Process each row
        for (int row = 0; row < GetNbRows(); row++)
        {
            // Get tokens in this row
            List<BasicToken> tokensInRow = new List<BasicToken>();
            
            for (int col = 0; col < cols_; col++)
            {
                int tileIndex = row * cols_ + col;
                if (tileIndex < tiles.Count)
                {
                    Tile tile = tiles[tileIndex];
                    if (tile.HasToken())
                    {
                        BasicToken token = tile.GetToken();
                        // Only include tokens that are on the board
                        if (token.GetInBoard())
                        {
                            tokensInRow.Add(token);
                        }
                    }
                }
            }
            
            // Assign scores to tokens that don't have them yet
            if (tokensInRow.Count > 0)
            {
                rowHistories_[row].AssignPositionScoresToNewTokens(tokensInRow, playerId, playerColor);
            }
        }
    }
    
    /// <summary>
    /// Freeze a validated word so its tokens cannot be moved individually.
    /// </summary>
    public void FreezeValidatedWord(int rowIndex, string word)
    {
        if (rowIndex < 0 || rowIndex >= rows_)
        {
            return;
        }
        
        List<Tile> tiles = GetTiles();
        List<BasicToken> tokensInWord = new List<BasicToken>();
        
        // Get all tokens in this row that form the word
        for (int col = 0; col < cols_; col++)
        {
            int tileIndex = rowIndex * cols_ + col;
            if (tileIndex < tiles.Count)
            {
                Tile tile = tiles[tileIndex];
                if (tile.HasToken())
                {
                    tokensInWord.Add(tile.GetToken());
                }
            }
        }
        
        // Freeze the word
        if (tokensInWord.Count > 0)
        {
            rowHistories_[rowIndex].FreezeWord(tokensInWord);
            Debug.Log($"BoardMultiplayer: Froze word '{word}' in row {rowIndex} with {tokensInWord.Count} tokens");
        }
    }
    
    /// <summary>
    /// Get the progressive score for a specific player.
    /// Sum of all position scores (1+2+3...) for tokens they own.
    /// Only counts tokens that are part of valid words.
    /// </summary>
    public int GetPlayerProgressiveScore(int playerId, FrenchDictionary dictionary)
    {
        int totalScore = 0;
        
        // Get all words for this player
        List<WordWithOwner> playerWords = GetPlayerWords(playerId);
        
        // Only count scores for valid words
        foreach (WordWithOwner wordInfo in playerWords)
        {
            if (dictionary.isWordValid(wordInfo.word_))
            {
                // Get score for this row
                totalScore += rowHistories_[wordInfo.rowIndex_].GetPlayerScore(playerId);
            }
        }
        
        return totalScore;
    }
    
    /// <summary>
    /// Get the frozen score (from previous turns) for a specific player.
    /// Only counts frozen tokens that are part of valid words.
    /// </summary>
    public int GetPlayerFrozenScore(int playerId, FrenchDictionary dictionary)
    {
        int totalScore = 0;
        
        // Get all words for this player
        List<WordWithOwner> playerWords = GetPlayerWords(playerId);
        
        // Only count scores for valid words
        foreach (WordWithOwner wordInfo in playerWords)
        {
            if (dictionary.isWordValid(wordInfo.word_))
            {
                // Get frozen score for this row
                totalScore += rowHistories_[wordInfo.rowIndex_].GetPlayerFrozenScore(playerId);
            }
        }
        
        return totalScore;
    }
    
    /// <summary>
    /// Get the current turn score (unfrozen tokens) for a specific player.
    /// Only counts unfrozen tokens that are part of valid words.
    /// </summary>
    public int GetPlayerCurrentTurnScore(int playerId, FrenchDictionary dictionary)
    {
        int totalScore = 0;
        
        // Get all words for this player
        List<WordWithOwner> playerWords = GetPlayerWords(playerId);
        
        // Only count scores for valid words
        foreach (WordWithOwner wordInfo in playerWords)
        {
            if (dictionary.isWordValid(wordInfo.word_))
            {
                // Get current turn score for this row
                totalScore += rowHistories_[wordInfo.rowIndex_].GetPlayerCurrentTurnScore(playerId);
            }
        }
        
        return totalScore;
    }

    public void Update()
    {
        // Check for screen size changes and update layout if needed
        CheckAndUpdateLayout();
        
        // Don't resize the board while any token is being dragged
        if (IsAnyTokenBeingDragged())
        {
            return;
        }
        
        // Update row ownership based on current state
        UpdateRowOwnership();
        
        // Assign position scores to tokens that don't have them yet (real-time scoring)
        if (turnManager_ != null)
        {
            int currentPlayer = turnManager_.GetCurrentPlayerId();
            AssignPositionScoresToUnassignedTokens(currentPlayer);
        }
        
        // Update word outlines to show player ownership
        UpdateWordOutlines();
    }
    
    /// <summary>
    /// Update visual outlines around complete words to show player ownership.
    /// Creates/destroys outline GameObjects as needed.
    /// </summary>
    private void UpdateWordOutlines()
    {
        // Clear existing outlines
        foreach (GameObject outline in wordOutlines_)
        {
            if (outline != null)
            {
                Destroy(outline);
            }
        }
        wordOutlines_.Clear();
        
        if (playerManager_ == null)
        {
            Debug.LogWarning("BoardMultiplayer: PlayerManager is null, cannot create word outlines");
            return;
        }
        
        List<Tile> tiles = GetTiles();
        int totalTokensChecked = 0;
        int tokensWithOwners = 0;
        int frozenTokensFound = 0;
        
        // Process each row
        for (int row = 0; row < GetNbRows(); row++)
        {
            // Find continuous sequences of tokens (words)
            List<BasicToken> currentWord = new List<BasicToken>();
            int wordStartCol = -1;
            int currentOwnerId = -1;
            
            for (int col = 0; col <= cols_; col++)
            {
                BasicToken token = null;
                int tokenOwnerId = -1;
                bool isFrozen = false;
                
                // Get token at this position (if within bounds)
                if (col < cols_)
                {
                    int tileIndex = row * cols_ + col;
                    if (tileIndex < tiles.Count)
                    {
                        Tile tile = tiles[tileIndex];
                        if (tile.HasToken())
                        {
                            token = tile.GetToken();
                            totalTokensChecked++;
                            if (token.GetInBoard())
                            {
                                tokenOwnerId = token.GetOwnerId();
                                isFrozen = token.IsFrozen();
                                if (tokenOwnerId >= 0)
                                {
                                    tokensWithOwners++;
                                }
                                if (isFrozen)
                                {
                                    frozenTokensFound++;
                                }
                            }
                        }
                    }
                }
                
                // Check if we're continuing the current word (only frozen tokens!)
                if (token != null && tokenOwnerId >= 0 && isFrozen && (currentOwnerId == -1 || currentOwnerId == tokenOwnerId))
                {
                    if (currentWord.Count == 0)
                    {
                        wordStartCol = col;
                    }
                    currentWord.Add(token);
                    currentOwnerId = tokenOwnerId;
                }
                else
                {
                    // Word ended, create outline if word exists
                    if (currentWord.Count > 0 && currentOwnerId >= 0)
                    {
                        Debug.Log($"BoardMultiplayer: Creating outline for {currentWord.Count} frozen tokens owned by Player {currentOwnerId + 1} in row {row}, col {wordStartCol}");
                        CreateWordOutline(row, wordStartCol, currentWord.Count, currentOwnerId);
                    }
                    
                    // Start new word if current token exists and is frozen
                    currentWord.Clear();
                    if (token != null && tokenOwnerId >= 0 && isFrozen)
                    {
                        currentWord.Add(token);
                        currentOwnerId = tokenOwnerId;
                        wordStartCol = col;
                    }
                    else
                    {
                        currentOwnerId = -1;
                    }
                }
            }
        }
        
        if (totalTokensChecked > 0 || frozenTokensFound > 0)
        {
            Debug.Log($"BoardMultiplayer: Checked {totalTokensChecked} tokens, {tokensWithOwners} had owners, {frozenTokensFound} frozen. Created {wordOutlines_.Count} outlines.");
        }
    }
    
    /// <summary>
    /// Create an outline around a word segment.
    /// </summary>
    private void CreateWordOutline(int row, int startCol, int length, int ownerId)
    {
        if (playerManager_ == null)
        {
            return;
        }
        
        List<Tile> tiles = GetTiles();
        
        // Get the first and last tile positions
        int startTileIndex = row * cols_ + startCol;
        int endTileIndex = row * cols_ + startCol + length - 1;
        
        if (startTileIndex >= tiles.Count || endTileIndex >= tiles.Count)
        {
            return;
        }
        
        Tile startTile = tiles[startTileIndex];
        Tile endTile = tiles[endTileIndex];
        
        RectTransform startRect = (RectTransform)startTile.transform;
        RectTransform endRect = (RectTransform)endTile.transform;
        
        // Create outline GameObject and parent it to a non-grid container (grid's parent)
        GameObject outlineObj = new GameObject($"WordOutline_R{row}_C{startCol}");
        Transform gridParent = startRect.parent != null ? startRect.parent.parent : transform;
        outlineObj.transform.SetParent(gridParent, false);
        outlineObj.transform.SetAsLastSibling(); // Render on top within the parent
        outlineObj.layer = gameObject.layer;
        
        RectTransform outlineRect = outlineObj.AddComponent<RectTransform>();
        Image outlineImage = outlineObj.AddComponent<Image>();
        Outline outline = outlineObj.AddComponent<Outline>();
        
        // Create a white sprite for the image
        Texture2D whiteTexture = new Texture2D(1, 1);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
        Sprite whiteSprite = Sprite.Create(whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        outlineImage.sprite = whiteSprite;
        
        // Get player color and configure outline
        Color playerColor = playerManager_.GetPlayerColor(ownerId);
        
        // No fill: keep the image fully transparent so only the border shows
        outlineImage.color = new Color(1f, 1f, 1f, 0f);
        outlineImage.raycastTarget = false;
        
        // Configure outline (visible border only)
        outline.effectColor = playerColor;
        outline.effectDistance = new Vector2(3f, -3f); // Thinner border to avoid covering tiles
        outline.useGraphicAlpha = false;
        
        // Add CanvasGroup to prevent raycast blocking
        CanvasGroup canvasGroup = outlineObj.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.alpha = 1f;
        
        // Use center anchors in parent space
        outlineRect.anchorMin = new Vector2(0.5f, 0.5f);
        outlineRect.anchorMax = new Vector2(0.5f, 0.5f);
        outlineRect.pivot = new Vector2(0.5f, 0.5f);

        // Position outline using world coordinates to avoid space mismatch
        Vector3 startWorldPos = startRect.position;
        Vector3 endWorldPos = endRect.position;
        float centerX = (startWorldPos.x + endWorldPos.x) / 2f;
        float centerY = (startWorldPos.y + endWorldPos.y) / 2f;

        // Size using tile rects in pixels
        float width = startRect.rect.width * length;
        float height = startRect.rect.height;

        outlineRect.position = new Vector3(centerX, centerY, startWorldPos.z);
        outlineRect.sizeDelta = new Vector2(width, height);

        // Debug hierarchy path
        string HierarchyPath(Transform t)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            while (t != null)
            {
                sb.Insert(0, "/" + t.name);
                t = t.parent;
            }
            return sb.ToString();
        }
        Debug.Log($"BoardMultiplayer: Outline parent path: {HierarchyPath(outlineObj.transform.parent)}");
        
        Debug.Log($"BoardMultiplayer: Created outline with Outline component, color {playerColor}, size {width}x{height}, at local pos ({centerX}, {centerY})");
        
        // Store reference
        wordOutlines_.Add(outlineObj);
    }
    
    /// <summary>
    /// Get all tokens that belong to the same frozen word as the given token.
    /// </summary>
    public List<BasicToken> GetTokensInFrozenWord(int frozenWordId)
    {
        List<BasicToken> tokensInWord = new List<BasicToken>();
        
        if (frozenWordId < 0)
        {
            return tokensInWord;
        }
        
        List<Tile> tiles = GetTiles();
        foreach (Tile tile in tiles)
        {
            if (tile.HasToken())
            {
                BasicToken token = tile.GetToken();
                if (token.IsFrozen() && token.GetFrozenWordId() == frozenWordId)
                {
                    tokensInWord.Add(token);
                }
            }
        }
        
        return tokensInWord;
    }
    
    private bool IsAnyTokenBeingDragged()
    {
        if (tokenPool_ == null)
        {
            return false;
        }
        
        List<BasicToken> tokens = tokenPool_.GetPool();
        foreach (BasicToken token in tokens)
        {
            if (token != null && token.BeingDragged())
            {
                return true;
            }
        }
        return false;
    }
}

/// <summary>
/// Word structure with owner information for multiplayer.
/// </summary>
[System.Serializable]
public class WordWithOwner
{
    public string word_;
    public int nb_green_letters_;
    public int playerId_;  // -1 = unowned, 0-3 = player ID
    public int rowIndex_;
}
