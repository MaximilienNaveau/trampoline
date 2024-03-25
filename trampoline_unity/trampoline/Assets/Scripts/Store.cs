using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Store : MonoBehaviour
{
    private TokenPool tokenPool_;
    private Row[] rows_;

    // Start is called before the first frame update
    void Start()
    {
        rows_ = GetComponentsInChildren<Row>();
        tokenPool_ = GameObject.Find("TokenPool").GetComponent<TokenPool>();
        UpdateStorage()
    }

    public void UpdateStorage()
    {
        int rowMax = rows_.Length;
        int colMax = rows_[0].GetTiles().Length;
        int row = 0;
        int col = 0;
        foreach(BasicToken token in tokenPool_.GetPool())
        {
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
