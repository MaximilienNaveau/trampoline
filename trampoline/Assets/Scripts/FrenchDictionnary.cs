using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public class FrenchDictionary
{
    private HashSet<String> frenchDictionary_;
    private ResourceRequest resourceRequest_;
    private bool frenchDictionaryLoaded_ = false;
    private string dictionaryName_ = "dictionary/libreoffice/dictionaries/fr-classique";

    public void initialize()
    {
        TextAsset textFile = Resources.Load<TextAsset>(dictionaryName_);
        frenchDictionary_ = LoadDictionary(textFile.text);
        frenchDictionaryLoaded_ = true;
    }

    public async Task<ResourceRequest> initializeAsync()
    {
        resourceRequest_ = Resources.LoadAsync<TextAsset>(dictionaryName_);
        resourceRequest_.completed += FrenchDictionaryLoaded_Completed;
        return resourceRequest_;
    }

    public bool isLoaded()
    {
        return frenchDictionaryLoaded_;
    }

    public bool isWordValid(string word)
    {
        if (!frenchDictionaryLoaded_)
        {
            return false;
        }
        String test_word = word.ToLower();
        bool isWordValid = frenchDictionary_.Contains(test_word);
        return isWordValid;
    }

    static HashSet<string> LoadDictionary(string dictionaryContent)
    {
        HashSet<string> dictionary = new HashSet<string>();
        string[] lines = dictionaryContent.Split(new char[] { '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
            {
                string word = line.Split('/')[0];
                string transformedWord = RemoveAccents(word.ToUpper());

                if (!dictionary.Contains(transformedWord) && !ContainsNumber(transformedWord))
                {
                    dictionary.Add(transformedWord);
                }
            }
        }
        // foreach (string word in dictionary)
        // {
        //     Debug.Log(word);
        // }
        return dictionary;
    }

    private void FrenchDictionaryLoaded_Completed(AsyncOperation handle)
    {
        Assert.IsTrue(handle.isDone);
        Assert.IsTrue(resourceRequest_.isDone);
        Assert.IsNotNull(resourceRequest_.asset);
        frenchDictionary_ =
            LoadDictionary(
                (resourceRequest_.asset as TextAsset).text);
        frenchDictionaryLoaded_ = true;
    }

    static string RemoveAccents(string input)
    {
        string normalized = input.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new StringBuilder();

        foreach (char c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    static bool ContainsNumber(string input)
    {
        // Regular expression to match any digit
        string pattern = @"\d+";

        // Use Regex to check if the input string contains a number
        return Regex.IsMatch(input, pattern);
    }
}
