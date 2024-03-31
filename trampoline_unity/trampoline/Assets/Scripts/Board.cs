using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;

public class Board : MonoBehaviour
{
    private Row[] rows_;
    List<Word> listOfWords_ = new List<Word>();

    private void Awake()
    {
        listOfWords_.Clear();
        rows_ = GetComponentsInChildren<Row>();
        Assert.AreEqual(rows_.Length, 13);
        for (int i = 0 ; i < rows_.Length ; i++)
        {
            listOfWords_.Add(new Word());
        }
    }

    public List<Word> GetListOfWords()
    {
        // Update the list of valid words and update score.
        for (int i = 0; i < rows_.Length; i++)
        {
            listOfWords_[i] = rows_[i].ExtractCurrentWord();
        }
        return listOfWords_;
    }

    // Update is called once per frame
    public void Update()
    {
    }
}
