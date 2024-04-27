// using System.Collections;
// using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
// using UnityEngine.TestTools;

public class ScoreTestEdit
{
    // A Test behaves as an ordinary method
    [Test]
    public void MyTest()
    {
        // Use the Assert class to test conditions
        var north = new Vector3(0, 1, 0);
        Assert.AreEqual(new Vector3(0, 1, 0), north);
        
    }
}
