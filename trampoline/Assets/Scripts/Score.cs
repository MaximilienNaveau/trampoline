using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Score : MonoBehaviour
{
    TMPro.TextMeshProUGUI score_text_;

    private int score_ = 0;

    // Start is called before the first frame update
    void Awake()
    {
        score_text_ = GetComponent<TMPro.TextMeshProUGUI>();
    }

    public void SetScore(int score)
    {
        score_ = score;
    }

    public int GetScore()
    {
        return score_;
    }

    // Update is called once per frame
    void Update()
    {
        score_text_.text = score_.ToString();
    }
}
