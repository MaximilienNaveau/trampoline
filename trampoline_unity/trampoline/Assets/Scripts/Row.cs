using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;

public class Word
{
    public string word_ = "";
    public int nb_greeen_letters_ = 0;
}

public class Row : MonoBehaviour
{
    private Tile[] tiles_;
    public int Length;

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

    public Word ExtractCurrentWord()
    {
        Word word = new Word();
        word.word_ = "";
        word.nb_greeen_letters_ = 0;
        
        for(int i = 0; i < tiles_.Length; i++)
        {
            if(!tiles_[i].HasToken())
            {
                break;
            }
            word.word_ += tiles_[i].GetToken().GetLetter();
            if(tiles_[i].GetToken().IsOnGreenFace())
            {
                word.nb_greeen_letters_++;
            }

        }
        return word;
    }

    public Tile[] GetTiles()
    {
        return tiles_;
    }
}
