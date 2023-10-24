using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Dictionnary : MonoBehaviour
{
    [SerializeField] private bool useCase_ = false;
    private HashSet<string> dictionnary_;
    // Start is called before the first frame update
    void Awake()
    {
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

    // Update is called once per frame
    public bool ValidWord ( string word )
    {
        return dictionnary_.Contains ( useCase_ ? word : word.ToLower ( ) );
    }
}
