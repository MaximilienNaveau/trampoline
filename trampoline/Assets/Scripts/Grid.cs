using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Assertions;

public class Grid : MonoBehaviour, IDropHandler, IScrollHandler
{
    [SerializeField] private GameObject tilePrefab_;
    private GridLayoutGroup grid_;
    private ScrollRect scrollRect_;
    private RectTransform content_;
    private Scrollbar verticalScrollbar_;

    private void ConfigureLayout()
    {
        // Use the parent as horizontal container.
        RectTransform containerRect = GetComponent<RectTransform>();

        // Create the ScrollRect (grid).
        GameObject scrollRectObject = new GameObject("ScrollRect", typeof(RectTransform), typeof(ScrollRect));
        scrollRectObject.transform.SetParent(transform, false);
        scrollRect_ = scrollRectObject.GetComponent<ScrollRect>();
        RectTransform scrollRectTransform = scrollRectObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0, 0);
        scrollRectTransform.anchorMax = new Vector2(1, 1);
        scrollRectTransform.offsetMin = Vector2.zero;
        scrollRectTransform.offsetMax = Vector2.zero;

        // Create the grid content.
        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup));
        contentObject.transform.SetParent(scrollRectObject.transform, false);
        content_ = contentObject.GetComponent<RectTransform>();
        content_.anchorMin = new Vector2(0, 1);
        content_.anchorMax = new Vector2(1, 1);
        content_.pivot = new Vector2(0.5f, 1);

        // Configure le GridLayoutGroup.
        grid_ = contentObject.GetComponent<GridLayoutGroup>();
        float spacing = 8f;
        float gridWidth = containerRect.rect.width - 14f; // 14px pour la scrollbar
        float cellSize = (gridWidth - 10 * spacing) / 9;
        grid_.cellSize = new Vector2(cellSize, cellSize);
        grid_.spacing = new Vector2(spacing, spacing);
        grid_.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid_.constraintCount = 9;
        grid_.childAlignment = TextAnchor.MiddleCenter;

        // Configure the ScrollRect.
        scrollRect_.content = content_;
        scrollRect_.scrollSensitivity = 40f;
        scrollRect_.horizontal = false;
        scrollRect_.vertical = true;

        // Create the vertical scrollbar.
        GameObject scrollbarObject = new GameObject("VerticalScrollbar", typeof(RectTransform), typeof(Scrollbar));
        scrollbarObject.transform.SetParent(transform, false);
        verticalScrollbar_ = scrollbarObject.GetComponent<Scrollbar>();
        RectTransform scrollbarTransform = scrollbarObject.GetComponent<RectTransform>();
        scrollbarTransform.anchorMin = new Vector2(1, 0);
        scrollbarTransform.anchorMax = new Vector2(1, 1);
        scrollbarTransform.pivot = new Vector2(1, 0.5f);
        scrollbarTransform.sizeDelta = new Vector2(14f, 0); // Largeur fine
        scrollbarTransform.offsetMin = new Vector2(-14f, 0);
        scrollbarTransform.offsetMax = new Vector2(0, 0);

        // Configure the scrollbar direction.
        verticalScrollbar_.direction = Scrollbar.Direction.BottomToTop;
        scrollRect_.verticalScrollbar = verticalScrollbar_;

        // Add the scrollbar background.
        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(scrollbarObject.transform, false);
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0, 0);
        backgroundRect.anchorMax = new Vector2(1, 1);
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        Image backgroundImage = backgroundObject.GetComponent<Image>();
        backgroundImage.color = new Color(0.8f, 0.8f, 0.8f, 1f); // gris clair

        // Add the scrollbar handle.
        GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObject.transform.SetParent(scrollbarObject.transform, false);
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0, 0);
        handleRect.anchorMax = new Vector2(1, 1);
        handleRect.offsetMin = new Vector2(2, 2);
        handleRect.offsetMax = new Vector2(-2, -2);
        Image handleImage = handleObject.GetComponent<Image>();
        handleImage.color = Color.white;

        verticalScrollbar_.targetGraphic = handleImage;
        verticalScrollbar_.handleRect = handleRect;

        // Place the content as high as possible.
        content_.anchoredPosition = new Vector2(content_.anchoredPosition.x, 0);
    }
}
