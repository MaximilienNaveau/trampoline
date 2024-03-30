using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ContentFitterRefresh : MonoBehaviour
{
    private void Start()
    {
        Canvas.ForceUpdateCanvases();
        RefreshContentFitters();
    }
 
    public void RefreshContentFitters()
    {
        var rectTransform = (RectTransform)transform;
        RefreshContentFitter(rectTransform);
    }
 
    private void RefreshContentFitter(RectTransform transform)
    {
        if (transform == null || !transform.gameObject.activeSelf)
        {
            return;
        }
     
        foreach (RectTransform child in transform)
        {
            RefreshContentFitter(child);
        }
 
        var layoutGroup = transform.GetComponent<LayoutGroup>();
        var contentSizeFitter = transform.GetComponent<ContentSizeFitter>();
        if (layoutGroup != null)
        {
            layoutGroup.CalculateLayoutInputHorizontal();
            layoutGroup.CalculateLayoutInputVertical();
            layoutGroup.SetLayoutHorizontal();
            layoutGroup.SetLayoutVertical();
        }
 
        if (contentSizeFitter != null)
        {
            contentSizeFitter.enabled = false;
            contentSizeFitter.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform);
        }
    }
}