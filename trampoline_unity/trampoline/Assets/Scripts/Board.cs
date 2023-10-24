using UnityEngine;
using UnityEngine.Assertions;

public class Board : MonoBehaviour
{
    private Row[] rows_;
    private Dictionnary dictionnary_;
    

    private void Awake()
    {
        dictionnary_ = FindObjectOfType<Dictionnary>();
        rows_ = GetComponentsInChildren<Row>();
        Assert.AreEqual(rows_.Length, 13);
    }

    

    // Update is called once per frame
    public void Update()
    {
        for(int i = 0; i < rows_.Length; i++)
        {
            string word = rows_[i].ExtractCurrentWord();
            if (dictionnary_.ValidWord(word))
            {
                Debug.Log("Row["+i.ToString()+"] as a valid word: " + word);
            }
        }
    }
}
