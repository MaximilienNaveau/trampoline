using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using System.Linq;


public class Board : MonoBehaviour
{
    [SerializeField] private GameObject tilePrefab_;
    private GridLayoutGroup grid_;
    private List<Tile> tiles_;
    private int rows_ = 13;
    private int cols_ = 9;
    private int numberOfTile_ = 0;

    private void Awake()
    {
        // Get the GridLayoutGroup component.
        grid_ = GetComponent<GridLayoutGroup>();
        // Delete all existing tiles.
        for (int i = grid_.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(grid_.transform.GetChild(i).gameObject);
        }
        // Create 2 new lines of tiles.
        ResizeGrid();
        Assert.IsTrue(grid_.transform.childCount == cols_ * 2);
        // Resize the grid to fit the number of columns and rows.
        grid_.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid_.constraintCount = cols_;
        numberOfTile_ = grid_.transform.childCount;
    }


    public List<Tile> GetTiles()
    {
        List<Tile> tiles = new();
        if (numberOfTile_ != grid_.transform.childCount)
        {
            numberOfTile_ = grid_.transform.childCount;
            for (int i = 0; i < numberOfTile_; i++)
            {
                tiles.Add(grid_.transform.GetChild(i).GetComponent<Tile>());
            }
        }
        return tiles;
    }

    public List<Word> GetListOfWords()
    {
        // Create the list of words.
        List<Word> listOfWords = new();
        listOfWords.Clear();
        // Get the list of tiles.
        tiles_ = GetTiles();
        if (tiles_.Count == 0)
        {
            return listOfWords;
        }
        // compute the available numer of rows there is in the grid.
        int existingRows = tiles_.Count / cols_;
        // Update the list of valid words.
        for (int row = 0; row < existingRows; row++)
        {
            Word word = new()
            {
                word_ = "",
                nb_green_letters_ = 0
            };
            for (int col = 0; col < cols_; col++)
            {
                int i = row * cols_ + col;
                if (!tiles_[i].HasToken())
                {
                    break;
                }
                word.word_ += tiles_[i].GetToken().GetLetter();
                if (tiles_[i].GetToken().IsOnGreenFace())
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
            int tileIndex = (rowIndex - 1) * cols_ + col;
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

    public bool ResizeGrid()
    {
        bool hasResized = false;

        // Calculate the number of existing rows
        int existingRows = grid_.transform.childCount / cols_;

        // Ensure there are at least 2 rows with tiles and 1 empty row
        if (existingRows < 2)
        {
            int rowsToAdd = 2 - existingRows;
            for (int i = 0; i < rowsToAdd; i++)
            {
                for (int col = 0; col < cols_; col++)
                {
                    Vector3 position = new();
                    Quaternion orientation = new();
                    Instantiate(tilePrefab_, position, orientation, grid_.transform);
                    hasResized = true;
                }
            }
        }
        else
        {
            // Check if the last row is empty
            if (!IsRowEmpty(existingRows - 1))
            {
                // Add an extra row if the last row is not empty
                if (existingRows <= rows_) // Ensure we don't exceed the maximum number of rows
                {
                    for (int col = 0; col < cols_; col++)
                    {
                        Vector3 position = new();
                        Quaternion orientation = new();
                        Instantiate(tilePrefab_, position, orientation, grid_.transform);
                        hasResized = true;
                    }
                }
            }
            else
            {
                // Remove extra rows if there are more than 2 rows with tiles and 1 empty row
                while (existingRows > 2 && IsRowEmpty(existingRows - 2))
                {
                    for (int col = 0; col < cols_; col++)
                    {
                        int i = grid_.transform.childCount - 1;
                        Destroy(grid_.transform.GetChild(i).gameObject);
                        hasResized = true;
                    }
                    existingRows--; // Update the row count after removing a row
                }
            }
        }

        return hasResized;
    }

    // Update is called once per frame
    public void Update()
    {
        ResizeGrid();
    }
}
