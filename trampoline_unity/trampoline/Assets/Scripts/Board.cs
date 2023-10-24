using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;

public class Board : MonoBehaviour
{
    [SerializeField] private bool useCase_ = false;
    private Row[] rows_;
    private HashSet<string> dictionnary_;
    

    private void Awake()
    {
        rows_ = GetComponentsInChildren<Row>();
        Assert.AreEqual(rows_.Length, 13);
        //Load a text file (Assets/Resources/Text/textFile01.txt)
        TextAsset textFile = Resources.Load<TextAsset>("dictionary");
        Assert.IsNotNull(textFile);
        // Creates the hashset data set at the start of the game.
        dictionnary_ = new HashSet<string>();
        string[] words = textFile.text.Split ( new [ ] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries );
        for ( int i = 0, count = words.Length; i < count; i++ )
        {
            dictionnary_.Add ( useCase_ ? words [ i ] : words [ i ].ToLower ( ) );
        }
    }

    public bool ValidWord ( string word )
    {
        return dictionnary_.Contains ( useCase_ ? word : word.ToLower ( ) );
    }

    // Update is called once per frame
    public void Update()
    {
        for(int i = 0; i < rows_.Length; i++)
        {
            string word = rows_[i].ExtractCurrentWord();
            if (ValidWord(word))
            {
                Debug.Log("Row["+i.ToString()+"] as a valid word: " + word);
            }
        }
    }
}
