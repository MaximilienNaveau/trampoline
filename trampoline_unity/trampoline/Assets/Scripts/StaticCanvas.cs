using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticCanvas : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        RectTransform trans = GetComponent<RectTransform>();
        trans.anchoredPosition = new Vector2(
            transform.parent.GetComponent<RectTransform>().sizeDelta.x / 2.0f - trans.sizeDelta.x / 2.0f,
            0.0f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
