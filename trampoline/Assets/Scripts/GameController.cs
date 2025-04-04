using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GameController : MonoBehaviour
{    
    private FrenchDictionary frenchDictionary_;
    private Score score_;
    private Board board_;
    private Store store_;
    private TokenPool tokenPool_;

    // Compute list of valid words on the board.
    private List<Word> ComputeListOfValidWords(List<Word> listOfWords)
    {
        List<Word> listOfValidWords = new List<Word>();
        listOfValidWords.Clear();
        for (int i = 0; i < listOfWords.Count; i++)
        {
            Word word = listOfWords[i];
            if (frenchDictionary_.isWordValid(word.word_))
            {
                listOfValidWords.Add(word);
            }
        }
        return listOfValidWords;
    }

    // Compute score
    private int ComputeScore(List<Word> listOfValidWords)
    {
        int score = 0;
        for (int i = 0; i < listOfValidWords.Count; i++)
        {
            int n = listOfValidWords[i].word_.Length;
            score += n * (n + 1) / 2;
            score -= listOfValidWords[i].nb_green_letters_ * 5;
        }
        return score;
    }

    // Start is called before the first frame update
    void Start()
    {
        frenchDictionary_ = FindAnyObjectByType<Singletons>().GetFrenchDictionary();
        score_ = FindAnyObjectByType<Score>();
        score_.SetScore(0);
        board_ = FindAnyObjectByType<Board>();
        store_ = FindAnyObjectByType<Store>();
        tokenPool_ = FindAnyObjectByType<TokenPool>();
        tokenPool_.DeactivateAllInactiveTokens();
    }

    // Update is called once per frame
    void Update()
    {
        List<Word> listOfWords = board_.GetListOfWords();
        List<Word> listOfValidWords = ComputeListOfValidWords(listOfWords);
        int score = ComputeScore(listOfValidWords);
        score_.SetScore(score);
        store_.UpdateStorage();
    }
}
