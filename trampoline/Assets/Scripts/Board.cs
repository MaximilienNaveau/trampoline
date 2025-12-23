using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Collections.Generic;

public class Board : ScrollableGrid
{
    private void Start()
    {
        // Configure the scrollable grid layout.
        ConfigureLayout();
        // Get the GridLayoutGroup component.
        ClearGrid();
        // Create 2 new lines of tiles.
        ResizeGrid(2);
        Assert.AreEqual(GetNbRows(), 2);

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
        // Ensure there are at least 2 rows with tiles and 1 empty row
        if (GetNbRows() < 2)
        {
            ResizeGrid(2);
            hasResized = true;
        }
        else if (GetNbRows() == 2)
        {
            if (!IsRowEmpty(GetNbRows() - 1))
            {
                AddNewRow();
                hasResized = true;
            }
        } else
        {
            // Remove all extra empty rows.
            while (GetNbRows() > 2 && IsRowEmpty(GetNbRows() - 1))
            {
                RemoveLastRow();
                hasResized = true;
            }
            // Ensure there is always at least one empty row at the bottom.
            if(GetNbRows() < rows_)
            {
                AddNewRow();
                hasResized = true;
            }
        }

        Assert.IsTrue(grid_.transform.childCount % cols_ == 0);
        Assert.IsGreaterOrEqual(GetNbRows(), 2);
        Assert.IsLessOrEqual(GetNbRows(), rows_);
        return hasResized;
    }

    // Update is called once per frame
    public void Update()
    {
        ResizeBoardGrid();
    }
}
