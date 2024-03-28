using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;

public class Board : MonoBehaviour
{
    private Row[] rows_;
    List<String> listOfWords_ = new List<String>();

    private void Awake()
    {
        // Force the update of the layout at the beginning of the game
        // to compute the anchor_position properly.
        VerticalLayoutGroup layout = GetComponent<VerticalLayoutGroup>();
        layout.CalculateLayoutInputHorizontal();
        layout.CalculateLayoutInputVertical();
        layout.SetLayoutHorizontal();
        layout.SetLayoutVertical();

        listOfWords_.Clear();
        rows_ = GetComponentsInChildren<Row>();
        Assert.AreEqual(rows_.Length, 13);
        for (int i = 0 ; i < rows_.Length ; i++)
        {
            listOfWords_.Add("");
        }
    }

    public List<String> GetListOfWords()
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
