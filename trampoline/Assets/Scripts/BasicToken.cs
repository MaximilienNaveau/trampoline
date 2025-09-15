using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Assertions;

public class BasicToken : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerClickHandler
{
    // Drag and drop variables
    private Vector2 startDragPosition_ = new(0f, 0f);
    private Tile tile_under_ = null;
    private bool draggedOnTile_ = false;
    private Vector2 dragOffset_;
    private RectTransform rectTransform_;
    private CanvasGroup canvasGroup_;
    private Canvas canvas_;
    private RectTransform canvasRectTransform_;
    private bool inBoard_ = false;

    // Double click management.
    private float lastTapTime_ = -1000000.0f;
    private const float doubleTapThreshold_ = 0.3f; // Adjust as needed

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
    [SerializeField] private float flipDeltaDuration_ = 0.01f;
    [SerializeField] private float flipDeltaScale_ = 0.1f;
    

    public bool GetInBoard()
    {
        return inBoard_;
    }

    public float GetFlipDeltaDuration()
    {
        return flipDeltaDuration_;
    }

    public void SetInBoard(bool inBoard)
    {
        inBoard_ = inBoard;
    }

    public Tile GetTileUnder()
    {
        return tile_under_;
    }

    private void Awake()
    {
        flipDeltaDuration_ = 0.01f;
        flipDeltaScale_ = 0.1f;
        rectTransform_ = GetComponent<RectTransform>();
        canvasGroup_ = GetComponent<CanvasGroup>();
        canvas_ = GameObject.FindGameObjectWithTag(
            "GameCanvas").GetComponent<Canvas>();
        canvasRectTransform_ = canvas_.GetComponent<RectTransform>();
        draggedOnTile_ = false;

        guiImages_ = GetComponentsInChildren<Image>();
        guiLetters_ = GetComponentsInChildren<TextMeshProUGUI>();

        LayoutElement layoutElement = GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = true;
        }

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

        if (tile_under_ != null)
        {
            tile_under_.LetTheTokenGo();
        }

        // // Calculer l'offset entre la position de la souris et la position actuelle du token
        // RectTransformUtility.ScreenPointToLocalPointInRectangle(
        //     canvasRectTransform_,
        //     eventData.position,
        //     canvas_.worldCamera,
        //     out Vector2 localMousePosition
        // );
        // dragOffset_ = rectTransform_.anchoredPosition - localMousePosition;
    }

    public bool BeingDragged()
    {
        return canvasGroup_.blocksRaycasts == false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Calculate the new position using delta and scaleFactor
        Vector2 delta = eventData.delta / canvas_.scaleFactor;
        rectTransform_.anchoredPosition += delta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup_.alpha = 1.0f;
        canvasGroup_.blocksRaycasts = true;
        if(!draggedOnTile_)
        {
            rectTransform_.anchoredPosition = startDragPosition_;
            if(tile_under_)
            {
                tile_under_.AttachToken(this);
            }
        }
    }

    public void SetDraggedOnTile(bool draggedOnTile)
    {
        draggedOnTile_ = draggedOnTile;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        float timeSinceLastTap = Mathf.Abs(Time.time - lastTapTime_);
        if (timeSinceLastTap <= doubleTapThreshold_)
        {
            StartCoroutine(this.FlipToken());
        }
        lastTapTime_ = Time.time;
    }

    public IEnumerator FlipToken()
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

    public bool IsOnGreenFace()
    {
        return colors_[sideShown_] == MyGameColors.GetGreen();
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

    public bool IsOnTile()
    {
        return tile_under_ != null;
    }

    public void SwapTileUnder(Tile tile)
    {
        if(tile_under_ != null)
        {
            tile_under_.LetTheTokenGo();
        }
        tile_under_ = tile;
    }

    public void UpdateSize(Vector2 sizeDelta)
    {
        rectTransform_.sizeDelta = sizeDelta;
    }

    public void Update()
    {
        if(!BeingDragged() && IsOnTile())
        {
            transform.position = tile_under_.transform.position;
        }
    }
}
