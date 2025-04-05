using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;


public class Board : MonoBehaviour
{
    private Tile[] tiles_;
    private int rows_ = 13;
    private int cols_ = 9;

    private void Awake()
    {
        tiles_ = GetComponentsInChildren<Tile>();
        Assert.AreEqual(tiles_.Length, cols_ * rows_);
    }

    public List<Word> GetListOfWords()
    {
        List<Word> listOfWords = new List<Word>();
        listOfWords.Clear();
        // Update the list of valid words.
        for (int row = 0; row < rows_; row++)
        {
            Word word = new Word();
            word.word_ = "";
            word.nb_green_letters_ = 0;
            for (int col = 0; col < cols_; col++)
            {
                int i = row * cols_ + col;
                if(!tiles_[i].HasToken())
                {
                    continue;
                }
                word.word_ += tiles_[i].GetToken().GetLetter();
                if(tiles_[i].GetToken().IsOnGreenFace())
                {
                    word.nb_green_letters_++;
                }
            }
            if (word.word_ != "")
            {
                listOfWords.Add(word);
            }
        }
        return listOfWords;
    }

    // Update is called once per frame
    public void Update()
    {
    }
}
