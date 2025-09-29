using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Assertions;

public class Store : MonoBehaviour, IDropHandler, IScrollHandler
{
    private TokenPool tokenPool_;
    [SerializeField] private GameObject tilePrefab_;
    private GridLayoutGroup grid_;
    private List<Tile> tiles_;

    private ScrollRect scrollRect_;
    private RectTransform content_;
    private Scrollbar verticalScrollbar_;

    void Start()
    {
        // Configure le ScrollRect et ses composants
        ConfigureScrollRect();

        tokenPool_ = FindAnyObjectByType<TokenPool>();
        Assert.IsTrue(tokenPool_ != null);
        UpdateStorage();
        tiles_ = GetTiles();
        Assert.AreNotEqual(tiles_.Count, 0);
        Assert.AreEqual(GetTokenInStorage().Count, 117);
    }

    void Update()
    {
        UpdateStorage();
    }

    private void ConfigureScrollRect()
    {
        // Crée un objet parent pour le ScrollRect
        GameObject scrollRectObject = new GameObject("ScrollRect", typeof(RectTransform), typeof(ScrollRect));
        scrollRectObject.transform.SetParent(transform, false);
        scrollRect_ = scrollRectObject.GetComponent<ScrollRect>();

        // Configure le RectTransform du ScrollRect
        RectTransform scrollRectTransform = scrollRectObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = Vector2.zero;
        scrollRectTransform.offsetMax = Vector2.zero;

        // Crée un objet pour le contenu
        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup));
        contentObject.transform.SetParent(scrollRectObject.transform, false);
        content_ = contentObject.GetComponent<RectTransform>();

        // Configure le RectTransform du contenu
        content_.anchorMin = new Vector2(0, 1);
        content_.anchorMax = new Vector2(1, 1);
        content_.pivot = new Vector2(0.5f, 1);

        // Configure le GridLayoutGroup
        grid_ = contentObject.GetComponent<GridLayoutGroup>();
        grid_.cellSize = new Vector2(65, 65); // Taille des cellules
        grid_.spacing = new Vector2(10, 10); // Espacement entre les cellules
        grid_.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid_.constraintCount = 9; // Nombre de colonnes
        grid_.childAlignment = TextAnchor.MiddleCenter;

        // Configure le ScrollRect
        scrollRect_.content = content_;
        scrollRect_.scrollSensitivity = 40f;
        scrollRect_.horizontal = false;
        scrollRect_.vertical = true;

        // Crée une barre de défilement verticale
        GameObject scrollbarObject = new GameObject("VerticalScrollbar",
            typeof(RectTransform), typeof(Scrollbar));
        scrollbarObject.transform.SetParent(scrollRectObject.transform, false);
        verticalScrollbar_ = scrollbarObject.GetComponent<Scrollbar>();

        // Configure le RectTransform de la barre de défilement
        RectTransform scrollbarTransform =
            scrollbarObject.GetComponent<RectTransform>();
        scrollbarTransform.anchorMin = new Vector2(1, 0);
        scrollbarTransform.anchorMax = new Vector2(1, 1);
        scrollbarTransform.offsetMin = new Vector2(-20, 0);
        scrollbarTransform.offsetMax = new Vector2(0, 0);

        // Configure la barre de défilement
        verticalScrollbar_.direction = Scrollbar.Direction.BottomToTop;
        scrollRect_.verticalScrollbar = verticalScrollbar_;

        // Ajoutez un objet pour le fond de la barre de défilement
        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(scrollbarObject.transform, false);
        Image backgroundImage = backgroundObject.GetComponent<Image>();
        backgroundImage.color = Color.gray;

        // Ajoutez un objet pour le handle de la barre de défilement
        GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObject.transform.SetParent(scrollbarObject.transform, false);
        Image handleImage = handleObject.GetComponent<Image>();
        handleImage.color = Color.white;

        // Configure le handle de la barre de défilement
        verticalScrollbar_.targetGraphic = handleImage;
        verticalScrollbar_.handleRect = handleObject.GetComponent<RectTransform>();

        // Positionne le contenu au maximum en haut
        content_.anchoredPosition = new Vector2(content_.anchoredPosition.x, 0);
    }

    private void ResizeGrid()
    {
        int numberOfTokens = NumberOfStoredToken();
        int numberOfTiles = grid_.transform.childCount;
        int idealNumberOfTiles = ((numberOfTokens + 8) / 9) * 9;
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
        Assert.AreEqual(idealNumberOfTiles % 9, 0);
        Assert.AreEqual(grid_.transform.childCount, idealNumberOfTiles);

        // Ajustez la taille du contenu après avoir redimensionné la grille
        UpdateContentSize();
    }

    private void UpdateContentSize()
    {
        // Obtenez la largeur et la hauteur du parent (ScrollRect)
        float parentWidth = ((RectTransform)scrollRect_.transform).rect.width;
        float parentHeight = ((RectTransform)scrollRect_.transform).rect.height;

        // Calculez la hauteur totale du contenu en fonction des lignes
        int rows = Mathf.CeilToInt((float)grid_.transform.childCount / grid_.constraintCount);
        float contentHeight = rows * (grid_.cellSize.y + grid_.spacing.y) - grid_.spacing.y;

        // Ajoutez un padding en haut et en bas (par exemple, 20 unités)
        float padding = 20f;

        // Ajustez la taille du contenu pour qu'elle corresponde au parent en largeur
        content_.sizeDelta = new Vector2(parentWidth, Mathf.Max(contentHeight + padding * 2, parentHeight));
    }

    public void UpdateStorage()
    {
        ResizeGrid();
        tiles_ = GetTiles();
        List<BasicToken> tokens = GetTokenInStorage();
        Assert.IsTrue(tiles_.Count >= tokens.Count);

        for (int i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].BeingDragged())
            {
                continue;
            }
            tokens[i].gameObject.SetActive(false);
        }

        int tile_id = 0;
        for (int i = 0; i < tokens.Count; i++)
        {
            if (tile_id < tiles_.Count)
            {
                if (tokens[i].BeingDragged())
                {
                    continue;
                }
                tokens[i].SwapTileUnder(tiles_[tile_id]);
                tokens[i].gameObject.SetActive(true);
                tile_id++;
            }
            if (tile_id >= tiles_.Count)
            {
                break;
            }
        }

        UpdateContentSize();
    }

    public int NumberOfStoredToken()
    {
        return GetTokenInStorage().Count;
    }

    public List<Tile> GetTiles()
    {
        List<Tile> tiles = new();
        for (int i = 0; i < grid_.transform.childCount; i++)
        {
            tiles.Add(grid_.transform.GetChild(i).GetComponent<Tile>());
        }
        return tiles;
    }

    public void OnDrop(PointerEventData eventData)
    {
        List<GameObject> hoveredList = eventData.hovered;
        foreach (var GO in hoveredList)
        {
            Debug.Log("Hovering over: " + GO.name);
            if (GO.name == "Store")
            {
                BasicToken token = eventData.pointerDrag.GetComponent<BasicToken>();
                if (token == null)
                {
                    return;
                }
                token.SetDraggedOnTile(false);
                token.SwapTileUnder(null);
                token.SetInBoard(false);
                token.gameObject.SetActive(false);
            }
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (scrollRect_ != null)
        {
            scrollRect_.OnScroll(eventData);
        }
    }

    public List<BasicToken> GetTokenInStorage()
    {
        List<BasicToken> ret = new();
        int N = tokenPool_.GetPool().Count;
        for (int i = 0; i < N; i++)
        {
            BasicToken token = tokenPool_.GetPool()[i];
            if (token.GetInBoard())
            {
                continue;
            }
            ret.Add(token);
        }
        return ret;
    }
}