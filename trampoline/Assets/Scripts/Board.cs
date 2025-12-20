using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Collections.Generic;

public class Board : MonoBehaviour, ScrollableGrid
{
    public readonly int rows_ = 13;
    private readonly float fractionOfScreenHeight_ = 0.75f;

    private void Awake()
    {
        // Configure the scrollable grid layout.
        ConfigureLayout()
        // Get the GridLayoutGroup component.
        clearGrid();
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

    private void RemoveLastRow()
    {
        int nbOfTiles = grid_.transform.childCount;
        Assert.IsTrue(nbOfTiles >= cols_);
        // Remove the last row of tiles.
        for (int col = 0; col < cols_; col++)
        {
            int i = nbOfTiles - 1 - col;
            Destroy(grid_.transform.GetChild(i).gameObject);
        }
        Assert.IsTrue(grid_.transform.childCount % cols_ == 0);
    }

    private void AddNewRow()
    {
        for (int col = 0; col < cols_; col++)
        {
            Vector3 position = new();
            Quaternion orientation = new();
            Instantiate(tilePrefab_, position, orientation, grid_.transform);
        }
        Assert.IsTrue(grid_.transform.childCount % cols_ == 0);
    }

    public bool ResizeGrid()
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
            if (!IsRowEmpty(existingRows - 1))
            {
                AddNewRow();
                hasResized = true;
            }
        } else
        {
            // Remove all extra empty rows.
            while (GetNbRows() >= 2 && IsRowEmpty(existingRows - 1))
            {
                RemoveLastRow();
                hasResized = true;
            }
            // Ensure there is always at least one empty row at the bottom.
            if(GetNbRows() < rows_)
            {
                AddNewRow();
                return hasResized;
            }
        }

        Assert.IsTrue(grid_.transform.childCount % cols_ == 0);
        return hasResized;
    }

    // Update is called once per frame
    public void Update()
    {
        ResizeGrid();
    }
}
