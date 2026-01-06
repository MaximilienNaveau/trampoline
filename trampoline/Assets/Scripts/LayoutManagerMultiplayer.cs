using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;

public class LayoutManagerMultiplayer : MonoBehaviour
{
    [SerializeField] float spacing_ = 8f;

    private RectTransform board_;
    private RectTransform store_;
    private RectTransform header_;
    
    void Awake()
    {
        header_ = (RectTransform)transform.Find("Header");
        board_ = (RectTransform)transform.Find("Board");
        store_ = (RectTransform)transform.Find("Store");
        
        if (header_ == null)
        {
            Debug.LogError("LayoutManager: Header component is missing.");
            throw new System.Exception("LayoutManager: Header component is missing.");
        }
        if (board_ == null)
        {
            Debug.LogError("LayoutManager: Board component is missing.");
            throw new System.Exception("LayoutManager: Board component is missing.");
        }
        if (store_ == null)
        {
            Debug.LogError("LayoutManager: Store component is missing.");
            throw new System.Exception("LayoutManager: Store component is missing.");
        }
        
        // Initialize screen size tracking
        lastScreenSize_ = new Vector2(Screen.width, Screen.height);
        
        // Perform initial layout
        PerformLayout();
    }
    
    void PerformLayout()
    {
        PlaceHeader();
        PlaceBoard();
        PlaceStore();
        CheckSize();
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

        // Calculate available height: Screen - Header - Store - Spacings
        float headerHeight = header_.rect.height;
        float storeHeight = store_.rect.height;
        float availableHeight = Screen.height - headerHeight - storeHeight - spacing_ * 4; // 4 spacings: top, after header, after board, after store
        
        // Use the available height for board
        float boardHeight = Mathf.Max(availableHeight, Screen.height * 0.15f); // Minimum 15% of screen

        // Resize the board to fit the width of the screen with a padding.
        board_.sizeDelta = new Vector2(
            Screen.width - spacing_ * 2,
            boardHeight);

        // Place the board below the header with a padding.
        board_.anchoredPosition =
            new Vector2(spacing_, -spacing_ * 2 - headerHeight);
    }

    void PlaceStore()
    {
        // Store size is already set by StoreMultiplayer.ConfigureFixedLayout()
        // Just position it below the board
        
        // Set the pivot to [0, 1] (top-left corner)
        store_.pivot = new Vector2(0, 1);

        // Place the store below the board with a padding.
        float headerHeight = header_.rect.height;
        float boardHeight = board_.rect.height;
        store_.anchoredPosition = new Vector2(
            spacing_,
            -spacing_ * 3 - headerHeight - boardHeight);
    }

    void CheckSize()
    {
        // Note: In multiplayer mode, store size is fixed by StoreMultiplayer
        // So we don't validate total height equals screen height
        float totalHeight = header_.sizeDelta.y + board_.sizeDelta.y + store_.sizeDelta.y + 4 * spacing_;
        Debug.Log($"LayoutManagerMultiplayer: Total height = {totalHeight}, Screen height = {Screen.height}");
    }

    private int lastBoardRowCount_ = -1;
    private Vector2 lastScreenSize_;
    
    void Update()
    {
        // Check for screen size changes
        Vector2 currentScreenSize = new Vector2(Screen.width, Screen.height);
        bool screenSizeChanged = currentScreenSize != lastScreenSize_;
        
        if (screenSizeChanged)
        {
            lastScreenSize_ = currentScreenSize;
            // Perform the overall layout first to resize containers
            PerformLayout();
            // Then notify grids to recalculate their internal layouts based on new sizes
            NotifyGridsToRecalculate();
            return;
        }
        
        // Only update layout when board content changes
        BoardMultiplayer boardScript = board_.GetComponent<BoardMultiplayer>();
        if (boardScript != null)
        {
            int currentRows = boardScript.GetNbRows();
            if (currentRows != lastBoardRowCount_)
            {
                lastBoardRowCount_ = currentRows;
                PerformLayout();
            }
        }
    }
    
    void NotifyGridsToRecalculate()
    {
        // Tell all ScrollableGrid components to recalculate their internal layouts
        BoardMultiplayer boardScript = board_.GetComponent<BoardMultiplayer>();
        StoreMultiplayer storeScript = store_.GetComponent<StoreMultiplayer>();
        
        if (boardScript != null)
        {
            boardScript.RecalculateGridLayout();
        }
        // StoreMultiplayer has fixed layout, no recalculation needed
    }
}
