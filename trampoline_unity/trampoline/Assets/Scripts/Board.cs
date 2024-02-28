using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;

public class Board : MonoBehaviour
{
    [SerializeField] private GameObject score_object_;
    private Row[] rows_;
    private Dictionnary dictionnary_;
    private Score score_;
    List<String> list_of_valid_words_ = new List<String>();


    private void Awake()
    {
        list_of_valid_words_.Clear();
        dictionnary_ = FindObjectOfType<Dictionnary>();
        rows_ = GetComponentsInChildren<Row>();
        Assert.AreEqual(rows_.Length, 13);
        score_ = score_object_.GetComponent<Score>();
        score_.SetScore(0);
    }

    // Compute score
    private int ComputeScore()
    {
        int score = 0;
        for (int i = 0; i < list_of_valid_words_.Count; i++)
        {
            int n = list_of_valid_words_[i].Length;
            score += n * (n + 1) / 2;
        }
        return score;
    }

    // Update is called once per frame
    public void Update()
    {
        list_of_valid_words_.Clear();
        for (int i = 0; i < rows_.Length; i++)
        {
            String word = rows_[i].ExtractCurrentWord();
            // Debug.Log("Row[" + i.ToString() + "] Testing word: \"" + word + "\"");
            if (dictionnary_.ValidWord(word))
            {
                // Debug.Log("Row[" + i.ToString() + "] has a valid word: " + word);
                list_of_valid_words_.Add(word);
            }
        }
        int new_score = ComputeScore();
        // Debug.Log("Current score is: " + new_score.ToString());
        score_.SetScore(new_score);
    }
}
