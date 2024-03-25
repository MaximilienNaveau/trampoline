using UnityEngine;
using UnityEngine.Assertions;

public class Row : MonoBehaviour
{
    private Tile[] tiles_;
    public int Length;

    private void Awake()
    {
        tiles_ = GetComponentsInChildren<Tile>();
        Assert.AreEqual(tiles_.Length, 9);
    }

    public Tile this[int i]
    {
        get { return tiles_[i]; }
        set { tiles_[i] = value; }
    }

    private void Start()
    {
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
