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
    private ScrollRect scrollRect_;
    private RectTransform content_;
    private Scrollbar verticalScrollbar_;

    void Start()
    {
        ConfigureScrollRect();
        tokenPool_ = FindAnyObjectByType<TokenPool>();
        Assert.IsTrue(tokenPool_ != null);

        ResizeGrid();
        UpdateStorage();

        List<Tile> tiles = GetTiles();
        for (int i = 0; i < tiles.Count; i++)
        {
            Assert.IsTrue(tiles[i].HasToken());
        }
        Assert.AreNotEqual(tiles.Count, 0);
        Assert.AreEqual(GetTokenInStorage().Count, 117);
    }

    void Update()
    {
        UpdateStorage();
        // Debug.Log("NumberOfStoredToken = " + NumberOfStoredToken().ToString());
    }

    private void ConfigureScrollRect()
    {
        // Utilise le parent existant comme conteneur horizontal
        RectTransform containerRect = GetComponent<RectTransform>();

        // Crée le ScrollRect (grille)
        GameObject scrollRectObject = new GameObject("ScrollRect", typeof(RectTransform), typeof(ScrollRect));
        scrollRectObject.transform.SetParent(transform, false);
        scrollRect_ = scrollRectObject.GetComponent<ScrollRect>();
        RectTransform scrollRectTransform = scrollRectObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0, 0);
        scrollRectTransform.anchorMax = new Vector2(1, 1);
        scrollRectTransform.offsetMin = Vector2.zero;
        scrollRectTransform.offsetMax = Vector2.zero;

        // Crée le contenu de la grille
        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup));
        contentObject.transform.SetParent(scrollRectObject.transform, false);
        content_ = contentObject.GetComponent<RectTransform>();
        content_.anchorMin = new Vector2(0, 1);
        content_.anchorMax = new Vector2(1, 1);
        content_.pivot = new Vector2(0.5f, 1);

        // Configure le GridLayoutGroup
        grid_ = contentObject.GetComponent<GridLayoutGroup>();
        float spacing = 8f;
        float gridWidth = containerRect.rect.width - 14f; // 14px pour la scrollbar
        float cellSize = (gridWidth - 10 * spacing) / 9;
        grid_.cellSize = new Vector2(cellSize, cellSize);
        grid_.spacing = new Vector2(spacing, spacing);
        grid_.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid_.constraintCount = 9;
        grid_.childAlignment = TextAnchor.MiddleCenter;

        // Configure le ScrollRect
        scrollRect_.content = content_;
        scrollRect_.scrollSensitivity = 40f;
        scrollRect_.horizontal = false;
        scrollRect_.vertical = true;

        // Crée la scrollbar verticale
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

        // Configure la barre de défilement
        verticalScrollbar_.direction = Scrollbar.Direction.BottomToTop;
        scrollRect_.verticalScrollbar = verticalScrollbar_;

        // Ajoute le background de la scrollbar
        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(scrollbarObject.transform, false);
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0, 0);
        backgroundRect.anchorMax = new Vector2(1, 1);
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        Image backgroundImage = backgroundObject.GetComponent<Image>();
        backgroundImage.color = new Color(0.8f, 0.8f, 0.8f, 1f); // gris clair

        // Ajoute le handle de la scrollbar
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

        // Positionne le contenu au maximum en haut
        content_.anchoredPosition = new Vector2(content_.anchoredPosition.x, 0);
    }

    private void ResizeGrid()
    {
        int numberOfTokens = NumberOfStoredToken();
        int numberOfTiles = grid_.transform.childCount;
        int idealNumberOfTiles = 13 * 9; // ((numberOfTokens + 8) / 9) * 9;
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
        List<Tile> tiles = GetTiles();
        List<BasicToken> tokens = GetTokenInStorage();
        Assert.IsTrue(tiles.Count >= tokens.Count);

        // Sort the tokens alphabetically by their main letter
        tokens.Sort((a, b) => a.GetMainLetter().CompareTo(b.GetMainLetter()));

        // Store the token not in the board in the storage.
        for (int i = 0; i < tokens.Count; i++)
        {
            if (i < tiles.Count)
            {
                if (tokens[i].BeingDragged())
                {
                    continue;
                }
                tokens[i].gameObject.SetActive(true);
                tiles[i].AttachToken(tokens[i]);
            }
        }
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
        // List<GameObject> hoveredList = eventData.hovered;
        // foreach (var GO in hoveredList)
        // {
        //     if (GO.name == "Store")
        //     {
        //         BasicToken token = eventData.pointerDrag.GetComponent<BasicToken>();
        //         if (token == null)
        //         {
        //             return;
        //         }
        //         token.SwapTileUnder(null);
        //         token.SetInBoard(false);
        //         token.gameObject.SetActive(false);
        //     }
        // }
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