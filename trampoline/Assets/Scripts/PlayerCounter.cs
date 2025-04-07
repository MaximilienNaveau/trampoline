using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerCounter: MonoBehaviour
{
    private int numberOfPlayer_ = 1;
    private const int minNumberOfPlayer_ = 1;
    private const int maxNumberOfPlayer_ = 4;

    private TextMeshProUGUI playerCountUI_;
    
    public void IncreasePlayerCount()
    {
        numberOfPlayer_ = numberOfPlayer_ + 1;
        if (numberOfPlayer_ >= maxNumberOfPlayer_)
        {
            numberOfPlayer_ = maxNumberOfPlayer_;
        }
    }

    public void DecreasePlayerCount()
    {
        numberOfPlayer_ = numberOfPlayer_ - 1;
        if (numberOfPlayer_ <= minNumberOfPlayer_)
        {
            numberOfPlayer_ = minNumberOfPlayer_;
        }
    }

    public int GetNumberOfPlayer()
    {
        return numberOfPlayer_;
    }
}
