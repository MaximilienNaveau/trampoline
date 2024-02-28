using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Dictionnary : MonoBehaviour
{
    private HashSet<String> dictionnary_;
    // Start is called before the first frame update
    void Awake()
    {
        //Load a text file (Assets/Resources/Text/textFile01.txt)
        TextAsset textFile = Resources.Load<TextAsset>("dictionary");
        Assert.IsNotNull(textFile);
        Debug.Log("Dictionnary contains: " + textFile.text);
        // Creates the hashset data set at the start of the game.
        dictionnary_ = new HashSet<string>(textFile.text.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries));
        
        foreach (string word in dictionnary_) {
            Debug.Log("Word in dict: \"" + word + "\"");
        }
    }

    // Update is called once per frame
    public bool ValidWord(string word)
    {
        String test_word = word.ToLower();
        bool is_word_valid = dictionnary_.Contains(test_word);
        // Debug.Log("The word [" + test_word + "] is valid ? " + is_word_valid.ToString());
        return is_word_valid;
    }
}
