using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TokenPool : MonoBehaviour
{
    private GameObject SpawnNewTokenObject()
    {
        Vector3 position = new Vector3();
        Quaternion orientation = new Quaternion();
        return Instantiate(tilePrefab_, position, orientation, GameObject.FindGameObjectWithTag("GameCanvas").transform);
    }

    private BasicToken GetBasicTokenFromObject(GameObject new_token_object)
    {
        BasicToken newToken = new_token_object.GetComponent<BasicToken>();
        RectTransform newTokenRect = newToken.GetComponent<RectTransform>();
        RectTransform a_tile_rect = FindObjectOfType<Tile>().GetComponent<RectTransform>();
        Debug.Log("a_tile_rect.sizeDelta = " + a_tile_rect.sizeDelta.ToString());
        newTokenRect.sizeDelta = a_tile_rect.sizeDelta;
        return newToken;
    }

    public List<BasicToken> GetPool()
    {
        return tokenPool_;
    }

    public List<GameObject> GetGameObjectPool()
    {
        return tokenObjPool_;
    }

    public void DeactivateAllInactiveTokens()
    {
        for (int i = 0; i < tokenObjPool_.Count; i++) 
        {
            tokenObjPool_[i].SetActive(tokenPool_[i].isOnTile());
        }
    }

    private void start()
    {
        DeactivateAllInactiveTokens();
    }

    private void Awake()
    {
        tokenPool_.Clear();
        tokenObjPool_.Clear();
        for (int i = 0; i < _nbLetter * _nbWord; i++)
        // for (int i = 0; i < 1; i++)
        {
            GameObject newBasicTokenObject = SpawnNewTokenObject();
            BasicToken newBasicToken = GetBasicTokenFromObject(newBasicTokenObject);
            tokenObjPool_.Add(newBasicTokenObject);
            tokenPool_.Add(newBasicToken);
        }

        InitializeTokens();
    }

    private void InitializeTokens()
    {
        // Replace letters and sprites if needed
        // row 0
        tokenPool_[0].SetParameters("A", "E", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[1].SetParameters("A", "I", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[2].SetParameters("A", "M", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[3].SetParameters("A", "O", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[4].SetParameters("A", "R", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[5].SetParameters("A", "S", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[6].SetParameters("A", "T", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[7].SetParameters("A", "U", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[8].SetParameters("B", "E", MyGameColors.GetYellow(), MyGameColors.GetGreen());

        // row 1
        tokenPool_[_nbLetter + 0].SetParameters("B", "N", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[_nbLetter + 1].SetParameters("B", "T", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[_nbLetter + 2].SetParameters("C", "A", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[_nbLetter + 3].SetParameters("C", "I", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[_nbLetter + 4].SetParameters("C", "N", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[_nbLetter + 5].SetParameters("C", "T", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[_nbLetter + 6].SetParameters("D", "C", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[_nbLetter + 7].SetParameters("D", "N", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[_nbLetter + 8].SetParameters("D", "R", MyGameColors.GetYellow(), MyGameColors.GetGreen());

        // row 2
        tokenPool_[2 * _nbLetter + 0].SetParameters("E", "-", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[2 * _nbLetter + 1].SetParameters("E", "-", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[2 * _nbLetter + 2].SetParameters("E", "-", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[2 * _nbLetter + 3].SetParameters("E", "-", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[2 * _nbLetter + 4].SetParameters("E", "-", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[2 * _nbLetter + 5].SetParameters("E", "-", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[2 * _nbLetter + 6].SetParameters("E", "-", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[2 * _nbLetter + 7].SetParameters("E", "C", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[2 * _nbLetter + 8].SetParameters("E", "D", MyGameColors.GetYellow(), MyGameColors.GetGreen());

        // row 3
        tokenPool_[3 * _nbLetter + 0].SetParameters("E", "G", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[3 * _nbLetter + 1].SetParameters("E", "H", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[3 * _nbLetter + 2].SetParameters("E", "L", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[3 * _nbLetter + 3].SetParameters("E", "N", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[3 * _nbLetter + 4].SetParameters("E", "O", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[3 * _nbLetter + 5].SetParameters("E", "S", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[3 * _nbLetter + 6].SetParameters("E", "T", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[3 * _nbLetter + 7].SetParameters("E", "Y", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[3 * _nbLetter + 8].SetParameters("E", "Z", MyGameColors.GetYellow(), MyGameColors.GetGreen());

        // row 4
        tokenPool_[4 * _nbLetter + 0].SetParameters("F", "A", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[4 * _nbLetter + 1].SetParameters("F", "E", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[4 * _nbLetter + 2].SetParameters("F", "T", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[4 * _nbLetter + 3].SetParameters("G", "A", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[4 * _nbLetter + 4].SetParameters("G", "I", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[4 * _nbLetter + 5].SetParameters("H", "A", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[4 * _nbLetter + 6].SetParameters("H", "T", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[4 * _nbLetter + 7].SetParameters("I", "B", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[4 * _nbLetter + 8].SetParameters("I", "D", MyGameColors.GetYellow(), MyGameColors.GetGreen());

        // row 5
        tokenPool_[5 * _nbLetter + 0].SetParameters("I", "E", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[5 * _nbLetter + 1].SetParameters("I", "F", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[5 * _nbLetter + 2].SetParameters("I", "M", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[5 * _nbLetter + 3].SetParameters("I", "O", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[5 * _nbLetter + 4].SetParameters("I", "R", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[5 * _nbLetter + 5].SetParameters("I", "S", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[5 * _nbLetter + 6].SetParameters("I", "U", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[5 * _nbLetter + 7].SetParameters("J", "E", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[5 * _nbLetter + 8].SetParameters("K", "U", MyGameColors.GetYellow(), MyGameColors.GetYellow());

        // row 6
        tokenPool_[6 * _nbLetter + 0].SetParameters("L", "A", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[6 * _nbLetter + 1].SetParameters("L", "D", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[6 * _nbLetter + 2].SetParameters("L", "I", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[6 * _nbLetter + 3].SetParameters("L", "S", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[6 * _nbLetter + 4].SetParameters("M", "E", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[6 * _nbLetter + 5].SetParameters("M", "S", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[6 * _nbLetter + 6].SetParameters("M", "U", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[6 * _nbLetter + 7].SetParameters("N", "-", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[6 * _nbLetter + 8].SetParameters("N", "A", MyGameColors.GetYellow(), MyGameColors.GetGreen());

        // row 7
        tokenPool_[7 * _nbLetter + 0].SetParameters("N", "F", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[7 * _nbLetter + 1].SetParameters("N", "I", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[7 * _nbLetter + 2].SetParameters("N", "L", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[7 * _nbLetter + 3].SetParameters("N", "P", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[7 * _nbLetter + 4].SetParameters("N", "Q", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[7 * _nbLetter + 5].SetParameters("N", "T", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[7 * _nbLetter + 6].SetParameters("O", "-", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[7 * _nbLetter + 7].SetParameters("O", "C", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[7 * _nbLetter + 8].SetParameters("O", "F", MyGameColors.GetYellow(), MyGameColors.GetGreen());

        // row 8
        tokenPool_[8 * _nbLetter + 0].SetParameters("O", "J", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[8 * _nbLetter + 1].SetParameters("O", "R", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[8 * _nbLetter + 2].SetParameters("O", "U", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[8 * _nbLetter + 3].SetParameters("P", "A", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[8 * _nbLetter + 4].SetParameters("P", "E", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[8 * _nbLetter + 5].SetParameters("P", "I", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[8 * _nbLetter + 6].SetParameters("Q", "E", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[8 * _nbLetter + 7].SetParameters("Q", "I", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[8 * _nbLetter + 8].SetParameters("R", "B", MyGameColors.GetYellow(), MyGameColors.GetGreen());

        // row 9
        tokenPool_[9 * _nbLetter + 0].SetParameters("R", "C", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[9 * _nbLetter + 1].SetParameters("R", "E", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[9 * _nbLetter + 2].SetParameters("R", "G", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[9 * _nbLetter + 3].SetParameters("R", "H", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[9 * _nbLetter + 4].SetParameters("R", "M", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[9 * _nbLetter + 5].SetParameters("R", "N", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[9 * _nbLetter + 6].SetParameters("R", "P", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[9 * _nbLetter + 7].SetParameters("R", "V", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[9 * _nbLetter + 8].SetParameters("S", "-", MyGameColors.GetYellow(), MyGameColors.GetGreen());

        // row 10
        tokenPool_[10 * _nbLetter + 0].SetParameters("S", "-", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[10 * _nbLetter + 1].SetParameters("S", "B", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[10 * _nbLetter + 2].SetParameters("S", "N", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[10 * _nbLetter + 3].SetParameters("S", "O", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[10 * _nbLetter + 4].SetParameters("S", "R", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[10 * _nbLetter + 5].SetParameters("S", "U", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[10 * _nbLetter + 6].SetParameters("S", "X", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[10 * _nbLetter + 7].SetParameters("T", "-", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[10 * _nbLetter + 8].SetParameters("T", "I", MyGameColors.GetYellow(), MyGameColors.GetGreen());

        // row 11
        tokenPool_[11 * _nbLetter + 0].SetParameters("T", "L", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[11 * _nbLetter + 1].SetParameters("T", "O", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[11 * _nbLetter + 2].SetParameters("T", "R", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[11 * _nbLetter + 3].SetParameters("T", "S", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[11 * _nbLetter + 4].SetParameters("T", "U", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[11 * _nbLetter + 5].SetParameters("T", "V", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[11 * _nbLetter + 6].SetParameters("U", "E", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[11 * _nbLetter + 7].SetParameters("U", "L", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[11 * _nbLetter + 8].SetParameters("U", "N", MyGameColors.GetYellow(), MyGameColors.GetGreen());

        // row 12
        tokenPool_[12 * _nbLetter + 0].SetParameters("U", "P", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[12 * _nbLetter + 1].SetParameters("U", "Q", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[12 * _nbLetter + 2].SetParameters("U", "R", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[12 * _nbLetter + 3].SetParameters("V", "E", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[12 * _nbLetter + 4].SetParameters("V", "S", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[12 * _nbLetter + 5].SetParameters("W", "S", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[12 * _nbLetter + 6].SetParameters("X", "E", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[12 * _nbLetter + 7].SetParameters("Y", "O", MyGameColors.GetYellow(), MyGameColors.GetGreen());
        tokenPool_[12 * _nbLetter + 8].SetParameters("Z", "R", MyGameColors.GetYellow(), MyGameColors.GetGreen());
    }

    [SerializeField] private GameObject tilePrefab_;
    private readonly List<GameObject> tokenObjPool_ = new List<GameObject>();
    private readonly List<BasicToken> tokenPool_ = new List<BasicToken>();
    private const int _nbWord = 13;
    private const int _nbLetter = 9;
}
