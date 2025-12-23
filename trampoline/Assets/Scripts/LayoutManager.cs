using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;

public class LayoutManager : MonoBehaviour
{
    [SerializeField] float spacing_ = 8f;

    private RectTransform board_;
    private RectTransform store_;
    private RectTransform header_;
    private GridLayoutGroup board_grid_layout_group_;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        header_ = (RectTransform)transform.Find("Header");
        board_ = (RectTransform)transform.Find("Board");
        store_ = (RectTransform)transform.Find("Store");
        board_grid_layout_group_ = board_.GetComponent<GridLayoutGroup>();
        
        Assert.AreNotEqual(header_, null);
        Assert.AreNotEqual(board_, null);
        Assert.AreNotEqual(store_, null);
        Assert.AreNotEqual(board_grid_layout_group_, null);
    }

    void PlaceHeader()
    {
        // Set the pivot to [0, 1] (top-left corner)
        header_.pivot = new Vector2(0, 1);

        // Resize the header to fit the width of the screen with a padding.
        header_.sizeDelta = new Vector2(
            Screen.width - spacing_ * 2,
            Screen.height * 0.08f);

        // Place the header at the top left of the screen with a padding.
        header_.anchoredPosition = new Vector2(spacing_, -spacing_);

        // Resize the elements of the header to fit the header size.
        RectTransform score = (RectTransform)header_.Find("Score");
        RectTransform title = (RectTransform)header_.Find("Title");
        RectTransform quit = (RectTransform)header_.Find("Quit");

        // Set the width of the score and quit to be equal to the height of the header with a padding.
        float elementWidth = header_.rect.height - spacing_ * 2;
        score.sizeDelta = new Vector2(elementWidth, elementWidth);
        quit.sizeDelta = new Vector2(elementWidth, elementWidth);
        
        // Set the width of the title to be the remaining space.
        float titleWidth = header_.rect.width - score.rect.width - quit.rect.width - spacing_ * 4;
        title.sizeDelta = new Vector2(titleWidth, title.rect.height);
    }

    void PlaceBoard()
    {
        // Set the pivot to [0, 1] (top-left corner)
        board_.pivot = new Vector2(0, 1);

        // Resize the board to fit the width of the screen with a padding.
        board_.sizeDelta = new Vector2(
            Screen.width - spacing_ * 2,
            Mathf.Min(board_.sizeDelta.y, Screen.height * 0.6f));

        // Place the board below the header with a padding.
        float headerHeight = header_.rect.height;
        board_.anchoredPosition =
            new Vector2(spacing_, -spacing_ * 2 - headerHeight);

        // Set the cell size to be a square that fits 9 columns with spacing.
        float boardCellSize = (board_.rect.width - spacing_ * 10) / 9;
        GridLayoutGroup gridLayoutGroup = board_.GetComponent<GridLayoutGroup>();
        gridLayoutGroup.cellSize = new Vector2(boardCellSize, boardCellSize);
        gridLayoutGroup.spacing = new Vector2(spacing_, spacing_);
    }

    void PlaceStore()
    {
        // Set the pivot to [0, 1] (top-left corner)
        store_.pivot = new Vector2(0, 1);

        // Resize the store to fit the width of the screen with a padding.
        // Set the height with the remaining space.
        store_.sizeDelta = new Vector2(
            Screen.width - spacing_ * 2,
            Screen.height - spacing_ * 4 - header_.rect.height - board_.rect.height);

        // Place the store below the board with a padding.
        float headerHeight = header_.rect.height;
        float boardHeight = board_.rect.height;
        store_.anchoredPosition = new Vector2(
            spacing_,
            -spacing_ * 3 - headerHeight - boardHeight);
    }

    void CheckSize()
    {
        Assert.AreEqual(
            header_.sizeDelta.y +
            board_.sizeDelta.y +
            store_.sizeDelta.y +
            4 * spacing_,
            Screen.height);
    }

    // Update is called once per frame
    void Update()
    {
        PlaceHeader();
        PlaceBoard();
        PlaceStore();
        CheckSize();
    }
}
