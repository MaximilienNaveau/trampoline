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
            header_.sizeDelta.y);

        // Place the header at the top left of the screen with a padding.
        header_.anchoredPosition = new Vector2(spacing_, -spacing_);
    }

    void PlaceBoard()
    {
        // Set the pivot to [0, 1] (top-left corner)
        board_.pivot = new Vector2(0, 1);

        // Resize the board to fit the width of the screen with a padding.
        board_.sizeDelta = new Vector2(
            Screen.width - spacing_ * 2,
            board_.sizeDelta.y);

        // Place the board below the header with a padding.
        float headerHeight = header_.rect.height;
        board_.anchoredPosition =
            new Vector2(spacing_, -spacing_ * 2 - headerHeight);
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

    void PlaceStoreGrid()
    {
        // Set the cell size to be a square that fits 9 columns with spacing.
        float cellSize = (store_.rect.width - spacing_ * 8) / 9;
        GridLayoutGroup gridLayoutGroup = store_.GetComponent<GridLayoutGroup>();
        gridLayoutGroup.cellSize = new Vector2(cellSize, cellSize);
        gridLayoutGroup.spacing = new Vector2(spacing_, spacing_);

        // Set the "tiles" position and size to be the same as the store object.
        RectTransform tiles = (RectTransform)store_.Find("Tiles");
        tiles.pivot = new Vector2(0, 1);
        tiles.sizeDelta = new Vector2(
            store_.rect.width,
            store_.rect.height
        );
        tiles.anchoredPosition = new Vector2(
            store_.anchoredPosition.x,
            store_.anchoredPosition.y
        );

        // Place the scroll bar at the right of the store.
        RectTransform scrollbar = (RectTransform)store_.Find("Scrollbar");
        scrollbar.pivot = new Vector2(1, 1);
        scrollbar.sizeDelta = new Vector2(
            scrollbar.sizeDelta.x,
            store_.rect.height
        );
        scrollbar.anchoredPosition = new Vector2(
            store_.anchoredPosition.x + store_.rect.width,
            store_.anchoredPosition.y
        );
    }

    // Update is called once per frame
    void Update()
    {
        PlaceHeader();
        PlaceBoard();
        PlaceStore();
    }
}
