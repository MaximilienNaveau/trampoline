using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;


public class Board : MonoBehaviour
{
    private Tile[] tiles_;
    private int rows_ = 9;
    private int cols_ = 13;
    List<Word> listOfWords_ = new List<Word>();

    private void Awake()
    {
        listOfWords_.Clear();
        tiles_ = GetComponentsInChildren<Tile>();
        Assert.AreEqual(tiles_.Length, cols_ * rows_);
        for (int i = 0 ; i < rows_ ; i++)
        {
            listOfWords_.Add(new Word());
        }
    }

    public List<Word> GetListOfWords()
    {
        // Update the list of valid words.
        for (int row = 0; row < rows_; row++)
        {
            listOfWords_[row].word_ = "";
            listOfWords_[row].nb_green_letters_ = 0;
            for (int col = 0; col < cols_; col++)
            {
                int i = row * cols_ + col;
                if(!tiles_[i].HasToken())
                {
                    break;
                }
                listOfWords_[row].word_ += tiles_[i].GetToken().GetLetter();
                if(tiles_[i].GetToken().IsOnGreenFace())
                {
                    listOfWords_[row].nb_green_letters_++;
                }
            }
        }
        return listOfWords_;
    }

    // Update is called once per frame
    public void Update()
    {
    }
}
