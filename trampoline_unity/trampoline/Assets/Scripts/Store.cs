using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Store : MonoBehaviour
{
    private TokenPool tokenPool_;
    private Row[] rows_;

    // Start is called before the first frame update
    void Start()
    {
        rows_ = GetComponentsInChildren<Row>();
        tokenPool_ = FindObjectOfType<TokenPool>();
    }

    // Update is called once per frame
    void Update()
    {
        int row = 0;
        int col = 0;
        foreach(BasicToken token in tokenPool_.tokenPool_)
        {
            if (count)
        }
    }
}
