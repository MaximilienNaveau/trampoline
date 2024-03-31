using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecondImage : MonoBehaviour
{
    RectTransform rect_transform_;
    RectTransform parent_rect_transform_;
    // Start is called before the first frame update
    void Start()
    {
        rect_transform_ = GetComponent<RectTransform>();
        parent_rect_transform_ = rect_transform_.parent.gameObject.GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        rect_transform_.sizeDelta = parent_rect_transform_.sizeDelta;
    }
}
