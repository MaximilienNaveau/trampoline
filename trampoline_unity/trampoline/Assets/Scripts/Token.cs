using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Token : MonoBehaviour
{
    private void OnMouseDown()
    {
        StartCoroutine(this.Wait(0.1f));
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

    public void Resize(float size)
    {
        transform.localScale *= size / _spriteRenderer.size.x;
    }

    public void SetLetters(string mainLetter, string secondaryLetter)
    {
        _letters[0] = mainLetter;
        _letters[1] = secondaryLetter;
        UpdateContent();
    }

    public void SetSprites(Sprite mainSprite, Sprite secondarySprite)
    {
        _sprites[0] = mainSprite;
        _sprites[1] = secondarySprite;
        UpdateContent();
    }

    IEnumerator Wait(float duration)
    {
        float delta_test = 0.1f;
        Vector3 initScale = transform.localScale;
        float size = initScale.x;
        while (size > 0.1)
        {
            size -= delta_test;
            transform.localScale = new Vector3(size, initScale.y, initScale.z);
            yield return new WaitForSeconds(duration);
        }
        SwapSide();
        while (size < initScale.x)
        {
            size += delta_test;
            transform.localScale = new Vector3(size, initScale.y, initScale.z);
            yield return new WaitForSeconds(duration);
        }
        transform.localScale = initScale;
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
