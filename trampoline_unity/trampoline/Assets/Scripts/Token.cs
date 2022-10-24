using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Token : MonoBehaviour
{
    private void OnMouseDown()
    {
        _dragOffset = transform.position -
                      Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    private void OnMouseDrag()
    {
        if (!_BeingDragged)
        {
            _initDraggedPosition = transform.position;
        }
        _BeingDragged = true;
        _clickPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        _draggedPosition = _clickPosition + _dragOffset;
        _draggedPosition.z = 0;
        transform.position = _draggedPosition;
    }

    private void OnMouseUp()
    {
        if (_clickEnable)
        {
            _clickEnable = false;
            StartCoroutine(TrapDoubleClicks(_doubleClickTimeout));
        }
        if (_missplaced)
        {
            transform.position = _initDraggedPosition;
        }
    }

    private void Awake()
    {
        TextMeshProUGUI[] letters = GetComponentsInChildren<TextMeshProUGUI>();
        _mainLetter = letters[0];
        _secondaryLetter = letters[1];
        _spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateContent();
    }

    public void OnDoubleClick()
    {
        StartCoroutine(this.Wait(0.1f));
    }

    IEnumerator TrapDoubleClicks(float timer)
    {
        // Debug.Log("Starting to listen for double clicks");
        float endTime = Time.time + timer;
        while (Time.time < endTime)
        {
            if (Input.GetMouseButtonDown(0))
            {
                // Debug.Log("Double click!");
                OnDoubleClick();
                yield return new WaitForSeconds(0.4f);
                _clickEnable = true;
                _doubleClick = true;

            }
            yield return 0;
        }

        if (!_doubleClick)
        {
            // Debug.Log("Single click");
        }
        else
        {
            _doubleClick = false;
        }

        _clickEnable = true;
        yield return 0;
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

    [SerializeField] private List<Sprite> _sprites;
    [SerializeField] private List<string> _letters;
    [SerializeField] private float _doubleClickTimeout = 0.2f;

    private SpriteRenderer _spriteRenderer;
    private int _sideShown = 0;
    private const int _front = 1;
    private const int _back = 0;
    private TextMeshProUGUI _mainLetter;
    private TextMeshProUGUI _secondaryLetter;
    private Vector3 _dragOffset;
    private Vector3 _draggedPosition;
    private Vector3 _initDraggedPosition;
    private Vector3 _clickPosition;
    private bool _BeingDragged = false;
    private bool _clickEnable = true;
    private bool _doubleClick = false;
    private bool _missplaced = true;
}
