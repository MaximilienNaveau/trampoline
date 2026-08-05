using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using TMPro;

public class ScoreTest
{
    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator GetScore()
    {
        var gameObject = new GameObject();
        var text = gameObject.AddComponent<TMPro.TextMeshProUGUI>();
        var score = gameObject.AddComponent<Score>();

        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;

        Assert.AreEqual(0, score.GetScore());
    }
}
