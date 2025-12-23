using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Assertions;

public class Store : ScrollableGrid, IDropHandler
{
    private TokenPool tokenPool_;

    void Start()
    {
        // Configure the scrollable grid layout.
        ConfigureLayout();

        // Get the token pool.
        tokenPool_ = FindAnyObjectByType<TokenPool>();

        // Resize the grid to fit the tokens.
        ResizeGrid(rows_);

        // Fit the tokens in the storage.
        UpdateStorage();

        // Test the initialization.
        List<Tile> tiles = GetTiles();
        for (int i = 0; i < tiles.Count; i++)
        {
            Assert.IsTrue(tiles[i].HasToken());
        }
        Assert.IsTrue(tokenPool_ != null);
        Assert.AreNotEqual(tiles.Count, 0);
        Assert.AreEqual(NumberOfStoredToken(), 117);
    }

    void Update()
    {
        UpdateStorage();
        // Debug.Log("NumberOfStoredToken = " + NumberOfStoredToken().ToString());
    }

    public void UpdateStorage()
    {
        List<Tile> tiles = GetTiles();
        List<BasicToken> tokens = GetTokenInStorage();
        Assert.IsTrue(tiles.Count >= tokens.Count);

        // Sort the tokens alphabetically by their main letter
        tokens.Sort((a, b) => a.GetLetters().CompareTo(b.GetLetters()));

        // Store the token not currently on the board.
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