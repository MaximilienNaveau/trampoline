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
        UpdateContentSize();

        // Fit the tokens in the storage.
        UpdateStorage();

        // Test the initialization.
        List<Tile> tiles = GetTiles();
        for (int i = 0; i < tiles.Count; i++)
        {
            if (!tiles[i].HasToken())
            {
                Debug.LogError($"Store: Tile at index {i} does not have a token.");
                throw new System.Exception($"Store: Tile at index {i} does not have a token.");
            }
        }
        if (tokenPool_ == null)
        {
            Debug.LogError("Store: TokenPool is null.");
            throw new System.Exception("Store: TokenPool is null.");
        }
        if (tiles.Count == 0)
        {
            Debug.LogError("Store: No tiles found.");
            throw new System.Exception("Store: No tiles found.");
        }
        if (NumberOfStoredToken() != 117)
        {
            Debug.LogError($"Store: Expected 117 stored tokens but found {NumberOfStoredToken()}.");
            throw new System.Exception($"Store: Expected 117 stored tokens but found {NumberOfStoredToken()}.");
        }
    }

    void Update()
    {
        // Check for screen size changes and update layout if needed
        CheckAndUpdateLayout();
        
        // Continuously update storage to reflect current token state
        UpdateStorage();
    }

    public void UpdateStorage()
    {
        List<Tile> tiles = GetTiles();
        List<BasicToken> tokens = GetTokenInStorage();
        
        // Filter out tokens that should not be in storage
        List<BasicToken> validTokens = new();
        foreach (BasicToken token in tokens)
        {
            // Only include tokens that are NOT on board and NOT being dragged
            if (!token.GetInBoard() && !token.BeingDragged())
            {
                validTokens.Add(token);
            }
        }
        
        if (tiles.Count < validTokens.Count)
        {
            Debug.LogError($"Store: Not enough tiles ({tiles.Count}) for valid tokens ({validTokens.Count}).");
            throw new System.Exception($"Store: Not enough tiles ({tiles.Count}) for valid tokens ({validTokens.Count}).");
        }

        // Sort the valid tokens alphabetically
        validTokens.Sort((a, b) => a.GetLetters().CompareTo(b.GetLetters()));
        
        // Clear all tiles first
        for (int i = 0; i < tiles.Count; i++)
        {
            if (tiles[i].HasToken())
            {
                tiles[i].LetTheTokenGo();
            }
        }

        // Assign valid tokens from the beginning of the grid
        for (int i = 0; i < validTokens.Count; i++)
        {
            validTokens[i].gameObject.SetActive(true);
            tiles[i].AttachToken(validTokens[i]);
        }
    }

    public int NumberOfStoredToken()
    {
        return GetTokenInStorage().Count;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            BasicToken token = eventData.pointerDrag.GetComponent<BasicToken>();
            if (token == null)
            {
                return;
            }
            // Mark token as not on board - UpdateStorage will handle the rest
            token.SetInBoard(false);
            token.SetDraggedOnTile(true);
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