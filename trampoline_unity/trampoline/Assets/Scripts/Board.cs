using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Board : MonoBehaviour
{
    private Row[] rows_;

    private void Awake()
    {
        rows_ = GetComponentsInChildren<Row>();
        Assert.AreEqual(rows_.Length, 13);
    }

    // Update is called once per frame
    public void Update()
    {
        // Debug.Log("Number of rows = " + rows_.Length);
        // for(int i = 0; i < rows_.Length; i++)
        // {
        //     Debug.Log("Row["+i.ToString()+"] = " +rows_[i].ExtractCurrentWord());
        // }
    }
}
