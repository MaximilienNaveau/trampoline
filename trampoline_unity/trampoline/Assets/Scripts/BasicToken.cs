using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Assertions;

public class BasicToken : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerClickHandler
{
    // Drag and drop variables
    private Vector2 startDragPosition_ = new Vector2(0f, 0f);
    private Tile tile_under_ = null;
    private bool draggedOnTile_ = false;
    private RectTransform rectTransform_;
    private CanvasGroup canvasGroup_;
    private Canvas canvas_;
    private bool inBoard_ = false;

    // Display colors and letters
    
    private int sideShown_ = 0;
    private const int FRONT_ = 0;
    private const int BACK_ = 1;

    // Gui stuff.
    private Image[] guiImages_;

    private TextMeshProUGUI[] guiLetters_;

    // Colors and letters to display.
    [SerializeField] private string [] letters_ = new string [] {"A", "B"};
    [SerializeField] private Color [] colors_ =
        new Color [] {MyGameColors.GetYellow(), MyGameColors.GetGreen()};

    // Double click management
    private int clickCount_ = 0;
    [SerializeField] private float flipDeltaDuration_ = 0.01f;
    [SerializeField] private float flipDeltaScale_ = 0.1f;
    

    public bool GetInBoard()
    {
        return inBoard_;
    }

    public void SetInBoard(bool inBoard)
    {
        inBoard_ = inBoard;
    }

    private void Awake()
    {
        flipDeltaDuration_ = 0.01f;
        flipDeltaScale_ = 0.1f;
        rectTransform_ = GetComponent<RectTransform>();
        canvasGroup_ = GetComponent<CanvasGroup>();
        canvas_ = GameObject.FindGameObjectWithTag(
            "StaticCanvas").GetComponent<Canvas>();
        draggedOnTile_ = false;

        guiImages_ = GetComponentsInChildren<Image>();
        guiLetters_ = GetComponentsInChildren<TextMeshProUGUI>();

        Assert.AreEqual(guiLetters_.Length, 2);
        Assert.AreEqual(guiImages_.Length, 2);

        UpdateContent();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup_.alpha = 0.6f;
        canvasGroup_.blocksRaycasts = false;
        startDragPosition_ = rectTransform_.anchoredPosition;
        draggedOnTile_ = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform_.anchoredPosition += eventData.delta / canvas_.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup_.alpha = 1.0f;
        canvasGroup_.blocksRaycasts = true;
        if(!draggedOnTile_)
        {
            rectTransform_.anchoredPosition = startDragPosition_;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
    }

    public void SetDraggedOnTile(bool draggedOnTile)
    {
        draggedOnTile_ = draggedOnTile;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        clickCount_ = eventData.clickCount;
 
        if (clickCount_ == 2)
        {
            StartCoroutine(this.Wait());
        }
 
    }

    IEnumerator Wait()
    {
        Vector3 initScale = transform.localScale;
        float size = initScale.x;
        while (size > 0.1)
        {
            size -= flipDeltaScale_;
            transform.localScale = new Vector3(size, initScale.y, initScale.z);
            yield return new WaitForSeconds(flipDeltaDuration_);
        }
        SwapSide();
        while (size < initScale.x)
        {
            size += flipDeltaScale_;
            transform.localScale = new Vector3(size, initScale.y, initScale.z);
            yield return new WaitForSeconds(flipDeltaDuration_);
        }
        transform.localScale = initScale;
    }

    private void UpdateContent()
    {
        guiImages_[0].color = colors_[sideShown_];
        guiImages_[1].color = colors_[GetOppositeSide()];
        guiLetters_[0].text = letters_[sideShown_];
        guiLetters_[1].text = letters_[GetOppositeSide()];
    }

    public string GetLetter()
    {
        return letters_[sideShown_];
    }

    public bool IsOnYellowFace()
    {
        return colors_[sideShown_] == MyGameColors.GetYellow();
    }

    private void SwapSide()
    {
        sideShown_ = GetOppositeSide();
        UpdateContent();
    }

    private int GetOppositeSide()
    {
        if (sideShown_ == BACK_)
        {
            return FRONT_;
        }
        else
        {
            return BACK_;
        }
    }

    public void SetParameters(string mainLetter, string secondaryLetter,
                              Color mainColor, Color secondaryColor)
    {
        letters_[0] = mainLetter;
        letters_[1] = secondaryLetter;
        colors_[0] = mainColor;
        colors_[1] = secondaryColor;
        UpdateContent();
    }

    public void SwapTileUnder(Tile tile)
    {
        if(tile_under_ != null)
        {
            tile_under_.LetTheTokenGo();
        }
        tile_under_ = tile;
    }

    public void Update()
    {
    }
}
