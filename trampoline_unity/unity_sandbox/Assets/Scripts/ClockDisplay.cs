using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public class ClockDisplay : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        _clockText.text = DateTime.Now.ToString();
    }

    [SerializeField] private TMP_Text _clockText;
}
