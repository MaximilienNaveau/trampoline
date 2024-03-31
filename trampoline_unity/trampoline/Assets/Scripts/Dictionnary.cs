using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Dictionnary : MonoBehaviour
{
    private HashSet<String> dictionnary_;
    private ResourceRequest resourceRequest_;
    private bool dictionaryLoaded_ = false;

    // Start is called before the first frame update
    void Awake()
    {
        //Load a text file (Assets/Resources/Text/textFile01.txt)
        resourceRequest_ = Resources.LoadAsync<TextAsset>("dictionary");
        resourceRequest_.completed += DictionnaryLoaded_Completed;
    }

    // Update is called once per frame
    public bool ValidWord(string word)
    {
        if(!dictionaryLoaded_)
        {
            return false;
        }
        String test_word = word.ToLower();
        bool is_word_valid = dictionnary_.Contains(test_word);
        return is_word_valid;
    }

    private void DictionnaryLoaded_Completed(AsyncOperation handle)
    {   
        Assert.IsTrue(handle.isDone);
        Assert.IsTrue(resourceRequest_.isDone);
        Assert.IsNotNull(resourceRequest_.asset);

        // Creates the hashset data set at the start of the game.
        dictionnary_ = new HashSet<string>(
            (resourceRequest_.asset as TextAsset).text.Split(new[] { "\n" },
            StringSplitOptions.RemoveEmptyEntries));

        dictionaryLoaded_ = true;
    }
}
