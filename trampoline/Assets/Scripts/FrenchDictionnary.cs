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

    public async void initialize(bool async)
    {
        if (async)
        {
            await initializeAsync();
        }
        else
        {
            initializeNonAsync();
        }
    }

    public void initializeNonAsync()
    {
        TextAsset textFile = Resources.Load<TextAsset>(dictionaryName_);
        frenchDictionary_ = LoadDictionary(textFile.text);
        frenchDictionaryLoaded_ = true;
    }

    public async Task initializeAsync()
    {
        resourceRequest_ = Resources.LoadAsync<TextAsset>(dictionaryName_);
        resourceRequest_.completed += FrenchDictionaryLoaded_Completed;
        await Task.Yield();
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
        return frenchDictionary_.Contains(word);
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
                word = word.Split('\t')[0];
                word = word.Split(' ')[0];
                string transformedWord = NormalizeWord(word);

                // Skip words that contain dashes, numbers, or superscript/subscript characters
                if (transformedWord.Contains("-") ||
                    ContainsNumber(transformedWord))
                {
                    continue;
                }
                // Add the word to the dictionary.
                dictionary.Add(transformedWord);
            }
        }
        return dictionary;
    }

    static bool ContainsSuperscriptOrSubscript(string input)
    {
        // Regular expression to match any superscript or subscript character
        string pattern = @"[\u2070-\u209F]";
        return Regex.IsMatch(input, pattern);
    }

    public HashSet<string> GetWords()
    {
        return frenchDictionary_;
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

    static public string NormalizeWord(string input)
    {
        // Replace "œ" with "oe" and "æ" with "ae" before normalization
        string normalized_input = input.Normalize(
            NormalizationForm.FormD);
        
        // Remove accents from the input string
        StringBuilder builder = new StringBuilder();
        foreach (char c in normalized_input)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }
        normalized_input = builder.ToString().Normalize(
            NormalizationForm.FormC);
        
        // Replace "œ" with "oe" and "æ" with "ae" after normalization.
        normalized_input = normalized_input.Replace("œ", "oe").Replace(
            "æ", "ae");

        // Remove any remaining superscript or subscript characters.
        normalized_input = normalized_input
            .Replace("ᵃ", "A").Replace("ᵇ", "B").Replace("ᶜ", "C")
            .Replace("ᵈ", "D").Replace("ᵉ", "E").Replace("ᶠ", "F")
            .Replace("ᵍ", "G").Replace("ʰ", "H").Replace("ⁱ", "I")
            .Replace("ʲ", "J").Replace("ᵏ", "K").Replace("ˡ", "L")
            .Replace("ᵐ", "M").Replace("ⁿ", "N").Replace("ᵒ", "O")
            .Replace("ᵖ", "P").Replace("ʳ", "R").Replace("ˢ", "S")
            .Replace("ᵗ", "T").Replace("ᵘ", "U").Replace("ᵛ", "V")
            .Replace("ʷ", "W").Replace("ˣ", "X").Replace("ʸ", "Y")
            .Replace("ᶻ", "Z")
            .Replace("ₐ", "A").Replace("ₑ", "E").Replace("ₕ", "H")
            .Replace("ᵢ", "I").Replace("ⱼ", "J").Replace("ₖ", "K")
            .Replace("ₗ", "L").Replace("ₘ", "M").Replace("ₙ", "N")
            .Replace("ₒ", "O").Replace("ₚ", "P").Replace("ᵣ", "R")
            .Replace("ₛ", "S").Replace("ₜ", "T").Replace("ᵤ", "U")
            .Replace("ᵥ", "V").Replace("ₓ", "X");
        
        // Return the normalized input string.
        return normalized_input.ToUpper();
    }

    static public bool ContainsNumber(string input)
    {
        // Regular expression to match any digit
        string pattern = @"\d+";

        // Use Regex to check if the input string contains a number
        return Regex.IsMatch(input, pattern);
    }
}