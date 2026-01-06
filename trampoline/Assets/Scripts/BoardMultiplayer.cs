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
    private int[] rowOwnership_;  // -1 = unowned, 0-3 = player ID
    private List<Image> rowIndicators_;  // Visual indicators for each row

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
        
        // Initialize row ownership tracking
        rowOwnership_ = new int[rows_];
        rowIndicators_ = new List<Image>();
        for (int i = 0; i < rows_; i++)
        {
            rowOwnership_[i] = -1;  // -1 means unowned
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
        
        // Color the tiles in this row with a subtle background
        List<Tile> tiles = GetTiles();
        int startIndex = rowIndex * cols_;
        int endIndex = Mathf.Min(startIndex + cols_, tiles.Count);
        
        Color playerColor = playerId < playerColors_.Length ? 
            playerColors_[playerId] : 
            Color.white;
        
        for (int i = startIndex; i < endIndex; i++)
        {
            Image tileImage = tiles[i].GetComponent<Image>();
            if (tileImage != null)
            {
                // Apply a subtle tint to show ownership
                Color tintedColor = new Color(
                    playerColor.r,
                    playerColor.g,
                    playerColor.b,
                    0.2f  // Low alpha for subtle effect
                );
                tileImage.color = tintedColor;
            }
        }
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

    public void Update()
    {
        // Check for screen size changes and update layout if needed
        CheckAndUpdateLayout();
        
        // Don't resize the board while any token is being dragged
        if (IsAnyTokenBeingDragged())
        {
            return;
        }
        
        if(ResizeBoardGrid())
        {
            UpdateContentSize();
        }
        
        // Update row ownership based on current state
        UpdateRowOwnership();
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
