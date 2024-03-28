using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GameController : MonoBehaviour
{    
    private Dictionnary dictionnary_;
    private Score score_;
    private Board board_;
    private Store store_;
    private TokenPool tokenPool_;
    private bool updateAsked_ = true;

    public void AskUpdate()
    {
        updateAsked_ = true;
    }

    // Compute list of valid words on the board.
    private List<String> ComputeListOfValidWords(List<String> listOfWords)
    {
        List<String> listOfValidWords = new List<String>();
        listOfValidWords.Clear();
        for (int i = 0; i < listOfWords.Count; i++)
        {
            String word = listOfWords[i];
            // Debug.Log("Row[" + i.ToString() + "] Testing word: \"" + word + "\"");
            if (dictionnary_.ValidWord(word))
            {
                // Debug.Log("Row[" + i.ToString() + "] has a valid word: " + word);
                listOfValidWords.Add(word);
            }
        }
        return listOfValidWords;
    }

    // Compute score
    private int ComputeScore(List<String> listOfValidWords)
    {
        int score = 0;
        for (int i = 0; i < listOfValidWords.Count; i++)
        {
            int n = listOfValidWords[i].Length;
            score += n * (n + 1) / 2;
        }
        return score;
    }

    // Start is called before the first frame update
    void Start()
    {
        dictionnary_ = FindObjectOfType<Dictionnary>();
        score_ = FindObjectOfType<Score>();
        score_.SetScore(0);
        board_ = FindObjectOfType<Board>();
        store_ = FindObjectOfType<Store>();
        tokenPool_ = FindObjectOfType<TokenPool>();
        tokenPool_.DeactivateAllInactiveTokens();
    }

    // Update is called once per frame
    void Update()
    {
        if(updateAsked_)
        {
            List<String> listOfWords = board_.GetListOfWords();
            List<String> listOfValidWords = ComputeListOfValidWords(listOfWords);
            int score = ComputeScore(listOfValidWords);
            score_.SetScore(score);
            store_.UpdateStorage();
        }
        updateAsked_ = false;
    }
}
