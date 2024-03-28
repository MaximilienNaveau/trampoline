using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;

public class Row : MonoBehaviour
{
    private Tile[] tiles_;
    public int Length;

    private void Awake()
    {
        // Force the update of the layout at the beginning of the game
        // to compute the anchor_position properly.
        HorizontalLayoutGroup layout = GetComponent<HorizontalLayoutGroup>();
        layout.CalculateLayoutInputHorizontal();
        layout.CalculateLayoutInputVertical();
        layout.SetLayoutHorizontal();
        layout.SetLayoutVertical();
    }

    public Tile this[int i]
    {
        get { return tiles_[i]; }
        set { tiles_[i] = value; }
    }

    private void Start()
    {
        tiles_ = GetComponentsInChildren<Tile>();
        Assert.AreEqual(tiles_.Length, 9);
        Length = tiles_.Length;
    }

    public string ExtractCurrentWord()
    {
        string word = "";
        
        for(int i = 0; i < tiles_.Length; i++)
        {
            if(!tiles_[i].HasToken())
            {
                break;
            }
            word += tiles_[i].GetToken().GetLetter();
        }
        return word;
    }

    public Tile[] GetTiles()
    {
        return tiles_;
    }
}
