using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Store : MonoBehaviour, IDropHandler
{
    private TokenPool tokenPool_;
    private Row[] rows_;
    private GameController gameController_;

    void Awake()
    {
        // Force the update of the layout at the beginning of the game
        // to compute the anchor_position properly.
        VerticalLayoutGroup layout = GetComponent<VerticalLayoutGroup>();
        layout.CalculateLayoutInputHorizontal();
        layout.CalculateLayoutInputVertical();
        layout.SetLayoutHorizontal();
        layout.SetLayoutVertical();
    }

    // Start is called before the first frame update
    void Start()
    {
        rows_ = GetComponentsInChildren<Row>();
        tokenPool_ = FindObjectOfType<TokenPool>();
        gameController_ = FindObjectOfType<GameController>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {   
            // Store a reference.
            BasicToken token = eventData.pointerDrag.GetComponent<BasicToken>();
            token.SetDraggedOnTile(true);
            token.SwapTileUnder(null);
            token.SetInBoard(false);

            // Update the game status.
            gameController_.AskUpdate();
        }
    }

    public void UpdateStorage()
    {
        int rowMax = rows_.Length;
        int colMax = rows_[0].GetTiles().Length;
        int row = 0;
        int col = 0;
        int N = tokenPool_.GetPool().Count;
        for(int i = 0 ; i < N ; i++)
        {
            BasicToken token = tokenPool_.GetPool()[i];
            GameObject token_object = tokenPool_.GetGameObjectPool()[i];
            if (token.GetInBoard())
            {
                continue;
            }
            if(row < rowMax && col < colMax)
            {
                // Relocate the Token.
                rows_[row][col].UpdateAbsolutePosition();
                token.GetComponent<RectTransform>().
                    anchoredPosition = rows_[row][col].GetAbsolutePosition();
                // Store a reference.
                token.SetDraggedOnTile(true);
                token.SwapTileUnder(rows_[row][col]);
                // activate it
                token_object.SetActive(true);
            }
            col = col + 1;
            if (col >= colMax)
            {
                col = 0;
                row = row + 1;
            }
            if(row >= rowMax)
            {
                break;
            }
        }
    }
}
