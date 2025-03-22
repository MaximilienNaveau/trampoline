using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;

public class Word
{
    public string word_ = "";
    public int nb_green_letters_ = 0;
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

    private void Awake()
    {
        tiles_ = GetComponentsInChildren<Tile>();
        Length = tiles_.Length;
    }

    public Word ExtractCurrentWord()
    {
        Word word = new Word();
        word.word_ = "";
        word.nb_green_letters_ = 0;
        
        for(int i = 0; i < tiles_.Length; i++)
        {
            if(!tiles_[i].HasToken())
            {
                break;
            }
            word.word_ += tiles_[i].GetToken().GetLetter();
            if(tiles_[i].GetToken().IsOnGreenFace())
            {
                word.nb_green_letters_++;
            }

        }
        return word;
    }

    public Tile[] GetTiles()
    {
        return tiles_;
    }
}
