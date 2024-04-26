using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Token : MonoBehaviour
{
    private void OnMouseDown()
    {
        StartCoroutine(this.Wait(0.01f, 1.0f));
    }

    private void Awake()
    {
        TextMeshProUGUI[] letters = GetComponentsInChildren<TextMeshProUGUI>();
        _mainLetter = letters[0];
        _secondaryLetter = letters[1];
        _spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateContent();
    }

    private void UpdateContent()
    {
        _spriteRenderer.sprite = _sprites[_sideShown];
        _mainLetter.text = _letters[_sideShown];
        _secondaryLetter.text = _letters[getOppositeSide()];
    }

    private void SwapSide()
    {
        _sideShown = getOppositeSide();
        UpdateContent();
    }

    private int getOppositeSide()
    {
        if (_sideShown == _back)
        {
            return _front;
        }
        else
        {
            return _back;
        }
    }

    IEnumerator Wait(float duration, float size)
    {
        while (size > 0.1)
        {
            size -= 0.07f;
            transform.localScale = new Vector3(size, 1, 1);
            yield return new WaitForSeconds(duration);
        }
        SwapSide();

        while (size < 0.99)
        {
            size += 0.07f;
            transform.localScale = new Vector3(size, 1, 1);
            yield return new WaitForSeconds(duration);
        }
    }

    private SpriteRenderer _spriteRenderer;
    [SerializeField] private List<Sprite> _sprites;
    [SerializeField] private List<string> _letters;

    private int _sideShown = 0;
    private const int _front = 1;
    private const int _back = 0;
    TextMeshProUGUI _mainLetter;
    TextMeshProUGUI _secondaryLetter;
}
