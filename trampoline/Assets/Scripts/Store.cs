using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Assertions;

public class Store : MonoBehaviour, IDropHandler
{
    private TokenPool tokenPool_;
    [SerializeField] private GameObject tilePrefab_;
    private GridLayoutGroup grid_;
    private List<Tile> tiles_;
    private int startingIndex_ = 0;

    // Start is called before the first frame update
    void Start()
    {
        // Get the GridLayoutGroup component.
        grid_ = GetComponentInChildren<GridLayoutGroup>();
        tokenPool_ = FindAnyObjectByType<TokenPool>();
        Assert.IsTrue(tokenPool_ != null);
        UpdateStorage();
        tiles_ = GetTiles();
        Assert.AreNotEqual(tiles_.Count, 0);
        startingIndex_ = 0;
        Assert.AreEqual(GetTokenInStorage().Count, 117);
    }

    private void ResizeGrid()
    {
        // We want to compute the number of tiles we need:
        // - The need the number of tile is greater than the number of token
        // - but also that number of tile must be multiple of 9.
        int numberOfTokens = NumberOfStoredToken();
        int numberOfTiles = grid_.transform.childCount;
        int idealNumberOfTiles =  ((numberOfTokens + 8) / 9) * 9;
        int numberOfTilesToCreate = idealNumberOfTiles - numberOfTiles;
        if (numberOfTilesToCreate == 0)
        {
            return;
        } else if (numberOfTilesToCreate < 0)
        {
            for (int i = numberOfTiles - 1; i >= idealNumberOfTiles; i--)
            {
                Destroy(grid_.transform.GetChild(i).gameObject);
            }
        } else
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
                token.SetDraggedOnTile(false);
                token.SwapTileUnder(null);
                token.SetInBoard(false);
            }
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

    public void UpdateStorage()
    {
        ResizeGrid();
        tiles_ = GetTiles();
        List<BasicToken> tokens = GetTokenInStorage();
        Assert.IsTrue(tiles_.Count >= tokens.Count);
        // First Deactivate all.
        for (int i = 0; i < tokens.Count; i++)
        {
            tokens[i].gameObject.SetActive(false);
        }
        // Then reactivate only the ones displayed in the store.
        int tile_id = 0;
        for(int i = startingIndex_ ; i < tokens.Count ; i++)
        {
            if(tile_id < tiles_.Count)
            {
                // Relocate the Token.
                tokens[i].transform.position =
                    tiles_[tile_id].transform.position;
                // Store a reference.
                tokens[i].SetDraggedOnTile(true);
                tokens[i].SwapTileUnder(tiles_[tile_id]);
                tokens[i].UpdateSize(
                    ((RectTransform)(tiles_[tile_id].transform)).sizeDelta);
                // activate it
                tokens[i].gameObject.SetActive(true);
            }
            tile_id++;
            if (tile_id >= tiles_.Count)
            {
                break;
            }
        }
    }

    public int NumberOfStoredToken()
    {
        return GetTokenInStorage().Count;
    }

    public void IncreaseStartingIndex()
    {
        tiles_ = GetTiles();
        startingIndex_ += tiles_.Count;
        int nbToken = NumberOfStoredToken();
        if (startingIndex_ > nbToken - tiles_.Count)
        {
            startingIndex_ = nbToken - tiles_.Count;
        }
        UpdateStorage();
        // Debug.Log("IncreaseStartingIndex: " + startingIndex_.ToString());
    }
    
    public void DecreaseStartingIndex()
    {
        tiles_ = GetTiles();
        startingIndex_ -= tiles_.Count;
        if (startingIndex_ < 0)
        {
            startingIndex_ = 0;
        }
        UpdateStorage();
        // Debug.Log("DecreaseStartingIndex: " + startingIndex_.ToString());
    }
}
