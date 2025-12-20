using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Assertions;

public class ScrollableGrid : IDropHandler, IScrollHandler
{
    [SerializeField] private GameObject tilePrefab_;
    private GridLayoutGroup grid_;
    private ScrollRect scrollRect_;
    private RectTransform content_;
    private Scrollbar verticalScrollbar_;
    const private int rows_ = 13;
    const private int cols_ = 9;

    private void ConfigureLayout()
    {
        // Use the parent as horizontal container.
        RectTransform containerRect = GetComponent<RectTransform>();

        // Create the ScrollRect (grid).
        GameObject scrollRectObject = new GameObject("ScrollRect", typeof(RectTransform), typeof(ScrollRect));
        scrollRectObject.transform.SetParent(transform, false);
        scrollRect_ = scrollRectObject.GetComponent<ScrollRect>();
        RectTransform scrollRectTransform = scrollRectObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0, 0);
        scrollRectTransform.anchorMax = new Vector2(1, 1);
        scrollRectTransform.offsetMin = Vector2.zero;
        scrollRectTransform.offsetMax = Vector2.zero;

        // Create the grid content.
        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup));
        contentObject.transform.SetParent(scrollRectObject.transform, false);
        content_ = contentObject.GetComponent<RectTransform>();
        content_.anchorMin = new Vector2(0, 1);
        content_.anchorMax = new Vector2(1, 1);
        content_.pivot = new Vector2(0.5f, 1);

        // Configure le GridLayoutGroup.
        grid_ = contentObject.GetComponent<GridLayoutGroup>();
        float spacing = 8f;
        float gridWidth = containerRect.rect.width - 14f; // 14px pour la scrollbar
        float cellSize = (gridWidth - 10 * spacing) / cols_;
        grid_.cellSize = new Vector2(cellSize, cellSize);
        grid_.spacing = new Vector2(spacing, spacing);
        grid_.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid_.constraintCount = cols_;
        grid_.childAlignment = TextAnchor.MiddleCenter;

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
        scrollbarTransform.sizeDelta = new Vector2(14f, 0); // Largeur fine
        scrollbarTransform.offsetMin = new Vector2(-14f, 0);
        scrollbarTransform.offsetMax = new Vector2(0, 0);

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
    }

    private GetNbRows()
    {
        Assert.AreEqual(grid_.transform.childCount % cols_, 0)
        return  grid_.transform.childCount / cols_ ;
    }

    private void ResizeGrid(int nbRows)
    {
        
        int currentNbRows = GetNbRows();
        int numberOfTilesToCreate = idealNumberOfTiles - numberOfTiles;

        if (numberOfTilesToCreate == 0)
        {
            return;
        }
        else if (numberOfTilesToCreate < 0)
        {
            for (int i = numberOfTiles - 1; i >= idealNumberOfTiles; i--)
            {
                Destroy(grid_.transform.GetChild(i).gameObject);
            }
        }
        else
        {
            for (int i = 0; i < numberOfTilesToCreate; i++)
            {
                Vector3 position = new();
                Quaternion orientation = new();
                Instantiate(tilePrefab_, position, orientation, grid_.transform);
            }
        }
        Assert.AreEqual(idealNumberOfTiles % cols_, 0);
        Assert.AreEqual(grid_.transform.childCount, idealNumberOfTiles);

        // Ajustez la taille du contenu après avoir redimensionné la grille
        UpdateContentSize();
    }

    private void RemoveLastRow()
    {
        if(GetNbRows() > 0){
            Debug.Log("No more tiles to remove, doing nothing.");
            return;
        }
        // Remove the last row of tiles.
        int nbOfTiles = grid_.transform.childCount;
        for (int col = 0; col < cols_; col++)
        {
            int i = nbOfTiles - 1 - col;
            Destroy(grid_.transform.GetChild(i).gameObject);
        }
        Assert.AreEqual(grid_.transform.childCount % cols_, 0);
    }

    private void AddNewRow()
    {
        for (int col = 0; col < cols_; col++)
        {
            Vector3 position = new();
            Quaternion orientation = new();
            Instantiate(tilePrefab_, position, orientation, grid_.transform);
        }
        Assert.AreEqual(grid_.transform.childCount % cols_, 0);
    }

    private void UpdateContentSize()
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

    public List<Tile> GetTiles()
    {
        List<Tile> tiles = new();
        for (int i = 0; i < grid_.transform.childCount; i++)
        {
            tiles.Add(grid_.transform.GetChild(i).GetComponent<Tile>());
        }
        Assert.AreEqual(tiles.Count, grid_.transform.childCount);
        return tiles;
    }

    public List<BasicToken> GetTokens()
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

    private void ClearGrid()
    {
        // Delete all existing tiles.
        for (int i = grid_.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(grid_.transform.GetChild(i).gameObject);
        }
    }
}
