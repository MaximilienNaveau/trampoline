using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Assertions;

public class ScrollableGrid : MonoBehaviour, IScrollHandler
{
    [SerializeField] protected GameObject tilePrefab_;
    protected GridLayoutGroup grid_;
    private ScrollRect scrollRect_;
    private RectTransform content_;
    private Scrollbar verticalScrollbar_;
    protected const int rows_ = 13;
    protected const int cols_ = 9;
    
    private Vector2 lastScreenSize_;
    private RectTransform containerRect_;
    private float spacing_ = 8f;

    protected void ConfigureLayout()
    {
        // Use the parent as horizontal container.
        containerRect_ = GetComponent<RectTransform>();
        
        // Get the Canvas scale factor.
        Canvas canvas = GetComponentInParent<Canvas>();
        float scaleFactor = (canvas != null) ? canvas.scaleFactor : 1f;

        // Create the ScrollRect (viewport).
        GameObject scrollRectObject = new GameObject("ScrollRect", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));
        scrollRectObject.transform.SetParent(transform, false);
        scrollRect_ = scrollRectObject.GetComponent<ScrollRect>();
        RectTransform scrollRectTransform = scrollRectObject.GetComponent<RectTransform>();
        
        // Configure GridLayoutGroup parameters.
        spacing_ = 8f;
        float scrollbarWidth = 8f;
        float scrollbarPadding = 4f;
        float scrollbarTotalWidth = scrollbarWidth + scrollbarPadding * 2;
        float maskInset = 10f; // Inset to prevent overflow on rounded corners
        float scrollbarVerticalInset = 15f; // Additional inset for scrollbar to account for rounded corners
        
        // Position ScrollRect to leave space for scrollbar on the right and add insets
        scrollRectTransform.anchorMin = new Vector2(0, 0);
        scrollRectTransform.anchorMax = new Vector2(1, 1);
        scrollRectTransform.offsetMin = new Vector2(maskInset, maskInset);
        scrollRectTransform.offsetMax = new Vector2(-scrollbarTotalWidth - maskInset, -maskInset);

        // Create the content container with GridLayoutGroup.
        GameObject contentObject = new GameObject("Content", typeof(RectTransform));
        contentObject.transform.SetParent(scrollRectObject.transform, false);
        content_ = contentObject.GetComponent<RectTransform>();
        content_.anchorMin = new Vector2(0, 1);
        content_.anchorMax = new Vector2(1, 1);
        content_.pivot = new Vector2(0.5f, 1);

        // Add the GridLayoutGroup to the content.
        grid_ = contentObject.AddComponent<GridLayoutGroup>();

        // Calculate available width for grid (ScrollRect width, which already excludes scrollbar)
        float availableWidth = scrollRectTransform.rect.width;
        
        // Calculate cell size: available width minus padding (left + right) and spacing between cells
        // For 9 columns: left_padding + 8*gaps + right_padding = spacing + 8*spacing + spacing = 10*spacing
        float cellSize = (availableWidth - (cols_ + 1) * spacing_) / cols_;
        
        grid_.cellSize = new Vector2(cellSize, cellSize);
        grid_.spacing = new Vector2(spacing_, spacing_);
        grid_.padding = new RectOffset(
            (int)spacing_,  // left
            (int)spacing_,  // right
            (int)spacing_,  // top
            (int)spacing_); // bottom
        grid_.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid_.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid_.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid_.constraintCount = cols_;
        grid_.childAlignment = TextAnchor.UpperCenter;

        // Configure the ScrollRect.
        scrollRect_.content = content_;
        scrollRect_.scrollSensitivity = 40f;
        scrollRect_.horizontal = false;
        scrollRect_.vertical = true;

        // Create the vertical scrollbar.
        GameObject scrollbarObject = new GameObject("VerticalScrollbar", typeof(RectTransform), typeof(Scrollbar));
        scrollbarObject.transform.SetParent(transform, false);
        verticalScrollbar_ = scrollbarObject.GetComponent<Scrollbar>();
        RectTransform scrollbarTransform = scrollbarObject.GetComponent<RectTransform>();
        scrollbarTransform.anchorMin = new Vector2(1, 0);
        scrollbarTransform.anchorMax = new Vector2(1, 1);
        scrollbarTransform.pivot = new Vector2(1, 0.5f);
        // Scrollbar dimensions (using previously defined variables)
        scrollbarTransform.sizeDelta = new Vector2(scrollbarWidth, 0);
        scrollbarTransform.offsetMin = new Vector2(-scrollbarWidth - scrollbarPadding * 2, scrollbarPadding + scrollbarVerticalInset);
        scrollbarTransform.offsetMax = new Vector2(-scrollbarPadding, -scrollbarPadding - scrollbarVerticalInset);

        // Configure the scrollbar direction.
        verticalScrollbar_.direction = Scrollbar.Direction.BottomToTop;
        scrollRect_.verticalScrollbar = verticalScrollbar_;

        // Add the scrollbar background.
        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(scrollbarObject.transform, false);
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0, 0);
        backgroundRect.anchorMax = new Vector2(1, 1);
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        Image backgroundImage = backgroundObject.GetComponent<Image>();
        backgroundImage.color = new Color(0.8f, 0.8f, 0.8f, 1f); // gris clair

        // Add the scrollbar handle.
        GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObject.transform.SetParent(scrollbarObject.transform, false);
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0, 0);
        handleRect.anchorMax = new Vector2(1, 1);
        handleRect.offsetMin = new Vector2(2, 2);
        handleRect.offsetMax = new Vector2(-2, -2);
        Image handleImage = handleObject.GetComponent<Image>();
        handleImage.color = Color.white;

        verticalScrollbar_.targetGraphic = handleImage;
        verticalScrollbar_.handleRect = handleRect;

        // Place the content as high as possible.
        content_.anchoredPosition = new Vector2(content_.anchoredPosition.x, 0);
        
        // Initialize screen size tracking
        lastScreenSize_ = new Vector2(Screen.width, Screen.height);
    }
    
    protected void CheckAndUpdateLayout()
    {
        // Check if screen size has changed
        Vector2 currentScreenSize = new Vector2(Screen.width, Screen.height);
        if (currentScreenSize != lastScreenSize_)
        {
            lastScreenSize_ = currentScreenSize;
            RecalculateLayout();
        }
    }
    
    public void RecalculateGridLayout()
    {
        // Public method that can be called by LayoutManager
        RecalculateLayout();
    }
    
    private void RecalculateLayout()
    {
        if (scrollRect_ == null || grid_ == null) return;
        
        // Recalculate cell size based on new viewport width
        RectTransform scrollRectTransform = scrollRect_.GetComponent<RectTransform>();
        float availableWidth = scrollRectTransform.rect.width;
        float cellSize = (availableWidth - (cols_ + 1) * spacing_) / cols_;
        
        grid_.cellSize = new Vector2(cellSize, cellSize);
        
        // Force layout rebuild
        LayoutRebuilder.ForceRebuildLayoutImmediate(content_);
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRectTransform);
        
        // Update content size
        UpdateContentSize();
    }

    public int GetNbRows()
    {
        if (grid_.transform.childCount % cols_ != 0)
        {
            Debug.LogError($"ScrollableGrid: Child count ({grid_.transform.childCount}) is not divisible by columns ({cols_}).");
            throw new System.Exception($"ScrollableGrid: Child count ({grid_.transform.childCount}) is not divisible by columns ({cols_}).");
        }
        return  grid_.transform.childCount / cols_ ;
    }

    protected void ResizeGrid(int nbRows)
    {
        int currentNbRows = GetNbRows();
        int nbRowsToCreate = nbRows - GetNbRows();

        if (nbRowsToCreate == 0)
        {
            return;
        }
        else if (nbRowsToCreate < 0)
        {
            for (int i = 0; i < -nbRowsToCreate; i++)
            {
                RemoveLastRow();
            }
        }
        else
        {
            for (int i = 0; i < nbRowsToCreate; i++)
            {
                AddNewRow();
            }
        }
        if (GetNbRows() != nbRows)
        {
            Debug.LogError($"ScrollableGrid: Expected {nbRows} rows but found {GetNbRows()}.");
            throw new System.Exception($"ScrollableGrid: Expected {nbRows} rows but found {GetNbRows()}.");
        }
        if (grid_.transform.childCount != nbRows * cols_)
        {
            Debug.LogError($"ScrollableGrid: Expected {nbRows * cols_} children but found {grid_.transform.childCount}.");
            throw new System.Exception($"ScrollableGrid: Expected {nbRows * cols_} children but found {grid_.transform.childCount}.");
        }

        // Ajustez la taille du contenu après avoir redimensionné la grille
        UpdateContentSize();
    }

    protected void RemoveLastRow()
    {
        if(GetNbRows() <= 0){
            Debug.Log("No more tiles to remove, doing nothing.");
            return;
        }
        // Remove the last row of tiles.
        int nbOfTiles = grid_.transform.childCount;
        for (int col = 0; col < cols_; col++)
        {
            int i = nbOfTiles - 1 - col;
            // Use DestroyImmediate to ensure tiles are removed immediately
            // This prevents infinite loops in resize logic that checks row counts
            DestroyImmediate(grid_.transform.GetChild(i).gameObject);
        }
        if (grid_.transform.childCount % cols_ != 0)
        {
            Debug.LogError($"ScrollableGrid: After removing row, child count ({grid_.transform.childCount}) is not divisible by columns ({cols_}).");
            throw new System.Exception($"ScrollableGrid: After removing row, child count ({grid_.transform.childCount}) is not divisible by columns ({cols_}).");
        }
    }

    protected void AddNewRow()
    {
        for (int col = 0; col < cols_; col++)
        {
            Vector3 position = new();
            Quaternion orientation = new();
            Instantiate(tilePrefab_, position, orientation, grid_.transform);
        }
        if (grid_.transform.childCount % cols_ != 0)
        {
            Debug.LogError($"ScrollableGrid: After adding row, child count ({grid_.transform.childCount}) is not divisible by columns ({cols_}).");
            throw new System.Exception($"ScrollableGrid: After adding row, child count ({grid_.transform.childCount}) is not divisible by columns ({cols_}).");
        }
    }

    protected void UpdateContentSize()
    {
        // Get parent size (ScrollRect)
        float parentWidth = ((RectTransform)scrollRect_.transform).rect.width;
        float parentHeight = ((RectTransform)scrollRect_.transform).rect.height;

        // Compute the total height of the content in function of the number of lines
        int rows = GetNbRows();
        float contentHeight = rows * (grid_.cellSize.y + grid_.spacing.y) - grid_.spacing.y;

        // Add padding on top and bottom.
        float padding = 20f;

        // Adjust content size so it fits in the parent size.
        // TODO check with the width of the parent + scroll
        content_.sizeDelta = new Vector2(parentWidth, Mathf.Max(contentHeight + padding * 2, parentHeight));
    }

    protected List<Tile> GetTiles()
    {
        List<Tile> tiles = new();
        for (int i = 0; i < grid_.transform.childCount; i++)
        {
            tiles.Add(grid_.transform.GetChild(i).GetComponent<Tile>());
        }
        if (tiles.Count != grid_.transform.childCount)
        {
            Debug.LogError($"ScrollableGrid: Tile count ({tiles.Count}) does not match child count ({grid_.transform.childCount}).");
            throw new System.Exception($"ScrollableGrid: Tile count ({tiles.Count}) does not match child count ({grid_.transform.childCount}).");
        }
        return tiles;
    }

    protected List<BasicToken> GetTokens()
    {
        List<BasicToken> tokens = new();
        List<Tile> tiles = GetTiles();
        for (int i = 0; i < tiles.Count; i++)
        {
            if (tiles[i].HasToken())
            {
                tokens.Add(tiles[i].GetToken());
            }
        }
        return tokens;
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (scrollRect_ != null)
        {
            scrollRect_.OnScroll(eventData);
        }
    }

    protected void ClearGrid()
    {
        // Delete all existing tiles.
        for (int i = grid_.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(grid_.transform.GetChild(i).gameObject);
        }
    }
}
