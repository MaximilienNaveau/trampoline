using System.Collections.Generic;
using UnityEngine;

public class TokenPool : MonoBehaviour
{
    private void Awake()
    {
        ComputeGridParams();
        _tokenPool.Clear();
        _tokenObjPool.Clear();
        Vector3 position = Vector3.zero;
        Quaternion orientation = Quaternion.identity;

        for (int i = 0; i < _nbLetter * _nbWord; i++)
        {
            GameObject newObj = Instantiate(_tokenType, position, orientation);
            Token newToken = newObj.GetComponent<Token>();
            newToken.Resize(_tokenSize);
            _tokenObjPool.Add(newObj);
            _tokenPool.Add(newToken);
        }

        // Replace letters and sprites if needed
        // row 0
        _tokenPool[0].SetLetters("A", "E");
        _tokenPool[1].SetLetters("A", "I");
        _tokenPool[2].SetLetters("A", "M");
        _tokenPool[3].SetLetters("A", "O");
        _tokenPool[4].SetLetters("A", "R");
        _tokenPool[5].SetLetters("A", "S");
        _tokenPool[6].SetLetters("A", "T");
        _tokenPool[7].SetLetters("A", "U");
        _tokenPool[8].SetLetters("B", "E");

        // row 1
        _tokenPool[_nbLetter + 0].SetLetters("B", "N");
        _tokenPool[_nbLetter + 1].SetLetters("B", "T");
        _tokenPool[_nbLetter + 2].SetLetters("C", "A");
        _tokenPool[_nbLetter + 3].SetLetters("C", "I");
        _tokenPool[_nbLetter + 4].SetLetters("C", "N");
        _tokenPool[_nbLetter + 5].SetLetters("C", "T");
        _tokenPool[_nbLetter + 6].SetLetters("D", "C");
        _tokenPool[_nbLetter + 7].SetLetters("D", "N");
        _tokenPool[_nbLetter + 8].SetLetters("D", "R");

        // row 2
        _tokenPool[2 * _nbLetter + 0].SetLetters("E", "-");
        _tokenPool[2 * _nbLetter + 1].SetLetters("E", "-");
        _tokenPool[2 * _nbLetter + 2].SetLetters("E", "-");
        _tokenPool[2 * _nbLetter + 3].SetLetters("E", "-");
        _tokenPool[2 * _nbLetter + 4].SetLetters("E", "-");
        _tokenPool[2 * _nbLetter + 5].SetLetters("E", "-");
        _tokenPool[2 * _nbLetter + 6].SetLetters("E", "-");
        _tokenPool[2 * _nbLetter + 7].SetLetters("E", "C");
        _tokenPool[2 * _nbLetter + 8].SetLetters("E", "D");

        // row 3
        _tokenPool[3 * _nbLetter + 0].SetLetters("E", "G");
        _tokenPool[3 * _nbLetter + 1].SetLetters("E", "H");
        _tokenPool[3 * _nbLetter + 2].SetLetters("E", "L");
        _tokenPool[3 * _nbLetter + 3].SetLetters("E", "N");
        _tokenPool[3 * _nbLetter + 4].SetLetters("E", "O");
        _tokenPool[3 * _nbLetter + 5].SetLetters("E", "S");
        _tokenPool[3 * _nbLetter + 6].SetLetters("E", "T");
        _tokenPool[3 * _nbLetter + 7].SetLetters("E", "Y");
        _tokenPool[3 * _nbLetter + 8].SetLetters("E", "Z");

        // row 4
        _tokenPool[4 * _nbLetter + 0].SetLetters("F", "A");
        _tokenPool[4 * _nbLetter + 1].SetLetters("F", "E");
        _tokenPool[4 * _nbLetter + 2].SetLetters("F", "T");
        _tokenPool[4 * _nbLetter + 3].SetLetters("G", "A");
        _tokenPool[4 * _nbLetter + 4].SetLetters("G", "I");
        _tokenPool[4 * _nbLetter + 5].SetLetters("H", "A");
        _tokenPool[4 * _nbLetter + 6].SetLetters("H", "T");
        _tokenPool[4 * _nbLetter + 7].SetLetters("I", "B");
        _tokenPool[4 * _nbLetter + 8].SetLetters("I", "D");

        // row 5
        _tokenPool[5 * _nbLetter + 0].SetLetters("I", "E");
        _tokenPool[5 * _nbLetter + 1].SetLetters("I", "F");
        _tokenPool[5 * _nbLetter + 2].SetLetters("I", "M");
        _tokenPool[5 * _nbLetter + 3].SetLetters("I", "O");
        _tokenPool[5 * _nbLetter + 4].SetLetters("I", "R");
        _tokenPool[5 * _nbLetter + 5].SetLetters("I", "S");
        _tokenPool[5 * _nbLetter + 6].SetLetters("I", "U");
        _tokenPool[5 * _nbLetter + 7].SetLetters("J", "E");
        _tokenPool[5 * _nbLetter + 8].SetLetters("K", "U");
        _tokenPool[5 * _nbLetter + 8].SetSprites(
            _tokenYellowYellowType, _tokenYellowYellowType
        );

        // row 6
        _tokenPool[6 * _nbLetter + 0].SetLetters("L", "A");
        _tokenPool[6 * _nbLetter + 1].SetLetters("L", "D");
        _tokenPool[6 * _nbLetter + 2].SetLetters("L", "I");
        _tokenPool[6 * _nbLetter + 3].SetLetters("L", "S");
        _tokenPool[6 * _nbLetter + 4].SetLetters("M", "E");
        _tokenPool[6 * _nbLetter + 5].SetLetters("M", "S");
        _tokenPool[6 * _nbLetter + 6].SetLetters("M", "U");
        _tokenPool[6 * _nbLetter + 7].SetLetters("N", "-");
        _tokenPool[6 * _nbLetter + 8].SetLetters("N", "A");

        // row 7
        _tokenPool[7 * _nbLetter + 0].SetLetters("N", "F");
        _tokenPool[7 * _nbLetter + 1].SetLetters("N", "I");
        _tokenPool[7 * _nbLetter + 2].SetLetters("N", "L");
        _tokenPool[7 * _nbLetter + 3].SetLetters("N", "P");
        _tokenPool[7 * _nbLetter + 4].SetLetters("N", "Q");
        _tokenPool[7 * _nbLetter + 5].SetLetters("N", "T");
        _tokenPool[7 * _nbLetter + 6].SetLetters("O", "-");
        _tokenPool[7 * _nbLetter + 7].SetLetters("O", "C");
        _tokenPool[7 * _nbLetter + 8].SetLetters("O", "F");

        // row 8
        _tokenPool[8 * _nbLetter + 0].SetLetters("O", "J");
        _tokenPool[8 * _nbLetter + 1].SetLetters("O", "R");
        _tokenPool[8 * _nbLetter + 2].SetLetters("O", "U");
        _tokenPool[8 * _nbLetter + 3].SetLetters("P", "A");
        _tokenPool[8 * _nbLetter + 4].SetLetters("P", "E");
        _tokenPool[8 * _nbLetter + 5].SetLetters("P", "I");
        _tokenPool[8 * _nbLetter + 6].SetLetters("Q", "E");
        _tokenPool[8 * _nbLetter + 7].SetLetters("Q", "I");
        _tokenPool[8 * _nbLetter + 8].SetLetters("R", "B");

        // row 9
        _tokenPool[9 * _nbLetter + 0].SetLetters("R", "C");
        _tokenPool[9 * _nbLetter + 1].SetLetters("R", "E");
        _tokenPool[9 * _nbLetter + 2].SetLetters("R", "G");
        _tokenPool[9 * _nbLetter + 3].SetLetters("R", "H");
        _tokenPool[9 * _nbLetter + 4].SetLetters("R", "M");
        _tokenPool[9 * _nbLetter + 5].SetLetters("R", "N");
        _tokenPool[9 * _nbLetter + 6].SetLetters("R", "P");
        _tokenPool[9 * _nbLetter + 7].SetLetters("R", "V");
        _tokenPool[9 * _nbLetter + 8].SetLetters("S", "-");

        // row 10
        _tokenPool[10 * _nbLetter + 0].SetLetters("S", "-");
        _tokenPool[10 * _nbLetter + 1].SetLetters("S", "B");
        _tokenPool[10 * _nbLetter + 2].SetLetters("S", "N");
        _tokenPool[10 * _nbLetter + 3].SetLetters("S", "O");
        _tokenPool[10 * _nbLetter + 4].SetLetters("S", "R");
        _tokenPool[10 * _nbLetter + 5].SetLetters("S", "U");
        _tokenPool[10 * _nbLetter + 6].SetLetters("S", "X");
        _tokenPool[10 * _nbLetter + 7].SetLetters("T", "-");
        _tokenPool[10 * _nbLetter + 8].SetLetters("T", "I");

        // row 11
        _tokenPool[11 * _nbLetter + 0].SetLetters("T", "L");
        _tokenPool[11 * _nbLetter + 1].SetLetters("T", "O");
        _tokenPool[11 * _nbLetter + 2].SetLetters("T", "R");
        _tokenPool[11 * _nbLetter + 3].SetLetters("T", "S");
        _tokenPool[11 * _nbLetter + 4].SetLetters("T", "U");
        _tokenPool[11 * _nbLetter + 5].SetLetters("T", "V");
        _tokenPool[11 * _nbLetter + 6].SetLetters("U", "E");
        _tokenPool[11 * _nbLetter + 7].SetLetters("U", "L");
        _tokenPool[11 * _nbLetter + 8].SetLetters("U", "N");

        // row 12
        _tokenPool[12 * _nbLetter + 0].SetLetters("U", "P");
        _tokenPool[12 * _nbLetter + 1].SetLetters("U", "Q");
        _tokenPool[12 * _nbLetter + 2].SetLetters("U", "R");
        _tokenPool[12 * _nbLetter + 3].SetLetters("V", "E");
        _tokenPool[12 * _nbLetter + 4].SetLetters("V", "S");
        _tokenPool[12 * _nbLetter + 5].SetLetters("W", "S");
        _tokenPool[12 * _nbLetter + 5].SetSprites(
            _tokenYellowYellowType, _tokenYellowYellowType
        );
        _tokenPool[12 * _nbLetter + 6].SetLetters("X", "E");
        _tokenPool[12 * _nbLetter + 7].SetLetters("Y", "O");
        _tokenPool[12 * _nbLetter + 8].SetLetters("Z", "R");


        _topLeft = new Vector2(_grid.xMin, _grid.yMax);
        for (int i = 0; i < _nbWord; i++)
        {
            for (int j = 0; j < _nbLetter; j++)
            {
                position = _topLeft;
                position = position +
                           new Vector3(_tokenSize * (j + 0.5f) + j * _interval,
                                       -(_tokenSize * (i + 0.5f) + i * _interval),
                                       0);
                _tokenPool[i * _nbLetter + j].transform.position = position;
            }
        }

    }

    private void ComputeGridParams()
    {
        _grid.size = _upperRightCorner - _lowerLeftCorner;
        _grid.center = 0.5f * (_lowerLeftCorner + _upperRightCorner);
        _tokenSize = Mathf.Min(ComputeTokenDim(_grid.width, _interval, _nbLetter),
                               ComputeTokenDim(_grid.height, _interval, _nbWord));
        _grid.width = _tokenSize * _nbLetter + (_nbLetter - 1) * _interval;
        _grid.height = _tokenSize * _nbWord + (_nbWord - 1) * _interval;
        _grid.center = 0.5f * (_lowerLeftCorner + _upperRightCorner);
    }

    private float ComputeTokenDim(float size, float interval, int nb_token)
    {
        float gridSize = size - (nb_token - 1) * interval;
        return gridSize / nb_token;
    }

    private List<GameObject> _tokenObjPool = new List<GameObject>();
    private List<Token> _tokenPool = new List<Token>();

    // Grid params
    private Rect _grid;
    private Vector2 _topLeft;
    private float _tokenSize = 0;
    private const int _nbWord = 13;
    private const int _nbLetter = 9;

    [SerializeField] private GameObject _tokenType;
    [SerializeField] private Sprite _tokenYellowYellowType;
    [SerializeField] private float _interval = 0.2f;
    [SerializeField] private Vector2 _lowerLeftCorner = Vector2.zero;
    [SerializeField] private Vector2 _upperRightCorner = Vector2.zero;
}
