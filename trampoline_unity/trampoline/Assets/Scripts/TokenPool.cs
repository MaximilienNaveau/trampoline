using System.Collections.Generic;
using UnityEngine;

public class TokenPool : MonoBehaviour
{
    private void Awake()
    {
        ComputeGridParams();
        _tokenPool.Clear();
        Vector3 position = Vector3.zero;
        Quaternion orientation = Quaternion.identity;

        for (int i = 0; i < _nbLetter * _nbWord; i++)
        {
            GameObject newObj = Instantiate(_tokenType, position, orientation);
            newObj.GetComponent<Token>().Resize(_tokenSize);
            _tokenPool.Add(newObj);
        }

        // Replace letters and sprites if needed


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

    private List<GameObject> _tokenPool = new List<GameObject>();

    // Grid params
    private Rect _grid;
    private Vector2 _topLeft;
    private float _tokenSize = 0;
    private const int _nbWord = 13;
    private const int _nbLetter = 9;

    [SerializeField] private GameObject _tokenType;
    [SerializeField] private GameObject _tokenYellowYellowType;
    [SerializeField] private float _interval = 0.2f;
    [SerializeField] private Vector2 _lowerLeftCorner = Vector2.zero;
    [SerializeField] private Vector2 _upperRightCorner = Vector2.zero;
}
