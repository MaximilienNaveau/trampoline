using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Collections.Generic;

public class Board : ScrollableGrid
{
    private TokenPool tokenPool_;
    
    private void Start()
    {
        // Configure the scrollable grid layout.
        ConfigureLayout();
        // Get the GridLayoutGroup component.
        ClearGrid();
        // Create 2 new lines of tiles.
        ResizeGrid(2);
        if (GetNbRows() != 2)
        {
            Debug.LogError($"Board: Expected 2 rows but found {GetNbRows()}.");
            throw new System.Exception($"Board: Expected 2 rows but found {GetNbRows()}.");
        }
        
        // Get the token pool reference
        tokenPool_ = FindAnyObjectByType<TokenPool>();
        if (tokenPool_ == null)
        {
            Debug.LogError("Board: TokenPool is null.");
            throw new System.Exception("Board: TokenPool is null.");
        }
    }

    public List<Word> GetListOfWords()
    {
        // Create the list of words.
        List<Word> listOfWords = new();
        listOfWords.Clear();
        // Get the list of tiles.
        List<Tile> tiles = GetTiles();
        if (tiles.Count == 0)
        {
            return listOfWords;
        }
        // Update the list of valid words.
        for (int row = 0; row < GetNbRows(); row++)
        {
            Word word = new()
            {
                word_ = "",
                nb_green_letters_ = 0
            };
            for (int col = 0; col < cols_; col++)
            {
                int i = row * cols_ + col;
                if (!tiles[i].HasToken())
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
    /// </summary>
    /// <returns>
    /// True if the grid was resized (rows added or removed), false if no changes were made.
    /// </returns>
    /// <remarks>
    /// The method performs several assertions to verify grid integrity:
    /// - Child count is divisible by number of columns
    /// - Number of rows is at least 2
    /// - Number of rows does not exceed the maximum (rows_)
    /// </remarks>
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
            // Keep removing the last row while: we have more than 2 rows AND the last TWO rows are both empty
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

        if (grid_.transform.childCount % cols_ != 0)
        {
            Debug.LogError($"Board: Child count ({grid_.transform.childCount}) is not divisible by columns ({cols_}).");
            throw new System.Exception($"Board: Child count ({grid_.transform.childCount}) is not divisible by columns ({cols_}).");
        }
        if (GetNbRows() < 2)
        {
            Debug.LogError($"Board: Number of rows ({GetNbRows()}) is less than minimum (2).");
            throw new System.Exception($"Board: Number of rows ({GetNbRows()}) is less than minimum (2).");
        }
        if (GetNbRows() > rows_)
        {
            Debug.LogError($"Board: Number of rows ({GetNbRows()}) exceeds maximum ({rows_}).");
            throw new System.Exception($"Board: Number of rows ({GetNbRows()}) exceeds maximum ({rows_}).");
        }
        return hasResized;
    }

    // Update is called once per frame
    public void Update()
    {
        // Check for screen size changes and update layout if needed
        CheckAndUpdateLayout();
        
        // Don't resize the board while any token is being dragged
        // This prevents crashes when tiles are destroyed during drag operations
        if (IsAnyTokenBeingDragged())
        {
            return;
        }
        
        if(ResizeBoardGrid())
        {
            UpdateContentSize();
        }
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
