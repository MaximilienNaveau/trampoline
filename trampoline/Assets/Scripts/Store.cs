using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Assertions;

public class Store : MonoBehaviour, IDropHandler
{
    private TokenPool tokenPool_;
    private Tile[] tiles_;
    private Board board_;
    private GameController gameController_;

    private int numberOfTile_ = 0;
    private int startingIndex_ = 0;

    // Start is called before the first frame update
    void Start()
    {
        tiles_ = GetComponentsInChildren<Tile>();
        tokenPool_ = FindAnyObjectByType<TokenPool>();
        gameController_ = FindAnyObjectByType<GameController>();
        board_ = FindAnyObjectByType<Board>();
        numberOfTile_ = tiles_.Length;
        Assert.AreNotEqual(numberOfTile_, 0);
    }

    public void OnDrop(PointerEventData eventData)
    {
        List<GameObject> hoveredList = eventData.hovered;
        foreach (var GO in hoveredList)
        {
            Debug.Log("Hovering over: " + GO.name);
            if(GO.name == "Store")
            {
                BasicToken token = eventData.pointerDrag.GetComponent<BasicToken>();
                token.SetDraggedOnTile(false);
                token.SwapTileUnder(null);
                token.SetInBoard(false); 
                // Update the game status.
                gameController_.AskUpdate();
            }
        }
    }

    public List<BasicToken> GetTokenInStorage()
    {
        List<BasicToken> ret = new List<BasicToken>();
        int N = tokenPool_.GetPool().Count;
        for(int i = 0 ; i < N ; i++)
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
        List<BasicToken> tokens = GetTokenInStorage();
        // First Deactivate all.
        for(int i = 0 ; i < tokens.Count ; i++)
        {
            tokens[i].gameObject.SetActive(false);
        }
        // Then reactivate only the ones displayed in the store.
        int tile_id = 0;
        for(int i = startingIndex_ ; i < tokens.Count ; i++)
        {
            if(tile_id < tiles_.Length)
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
            tile_id = tile_id + 1;
            if (tile_id >= tiles_.Length)
            {
                break;
            }
        }
    }

    public int numberOfStoredToken()
    {
        return GetTokenInStorage().Count;
    }

    public void IncreaseStartingIndex()
    {
        startingIndex_ += numberOfTile_;
        int nbToken = numberOfStoredToken();
        if (startingIndex_ > nbToken - numberOfTile_)
        {
            startingIndex_ = nbToken - numberOfTile_;
        }
        UpdateStorage();
        // Debug.Log("IncreaseStartingIndex: " + startingIndex_.ToString());
    }
    
    public void DecreaseStartingIndex()
    {
        startingIndex_ -= numberOfTile_;
        if (startingIndex_ < 0)
        {
            startingIndex_ = 0;
        }
        UpdateStorage();
        // Debug.Log("DecreaseStartingIndex: " + startingIndex_.ToString());
    }
}
