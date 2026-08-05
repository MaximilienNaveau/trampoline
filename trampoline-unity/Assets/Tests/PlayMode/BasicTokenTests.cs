using NUnit.Framework;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;


public class BasicTokenTests
{
    private GameObject tokenObject_;
    private BasicToken basicToken_;

    [SetUp]
    public void SetUp()
    {
        // Create the main GameObject and add the BasicToken component
        tokenObject_ = new GameObject("TokenObject_");
        tokenObject_.AddComponent<RectTransform>();
        tokenObject_.AddComponent<CanvasGroup>();

        // Create child GameObjects for TextMeshProUGUI components
        var textChild1 = new GameObject("TextChild1");
        textChild1.transform.SetParent(tokenObject_.transform);
        textChild1.AddComponent<TextMeshProUGUI>();

        var textChild2 = new GameObject("TextChild2");
        textChild2.transform.SetParent(tokenObject_.transform);
        textChild2.AddComponent<TextMeshProUGUI>();

        // Create child GameObjects for Image components
        var imageChild1 = new GameObject("ImageChild1");
        imageChild1.transform.SetParent(tokenObject_.transform);
        imageChild1.AddComponent<Image>();

        var imageChild2 = new GameObject("ImageChild2");
        imageChild2.transform.SetParent(tokenObject_.transform);
        imageChild2.AddComponent<Image>();

        // Create a canvas GameObject for the MasterCanvas tag
        var canvas = new GameObject("GameCanvas") { tag = "GameCanvas" };
        canvas.AddComponent<Canvas>();

        // Add the BasicToken component and set its parameters
        basicToken_ = tokenObject_.AddComponent<BasicToken>();
        basicToken_.SetParameters(
            "A", "E", MyGameColors.GetYellow(), MyGameColors.GetGreen());
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up after each test
        Object.DestroyImmediate(tokenObject_);
    }

    [Test]
    public void TestSetInBoard()
    {
        // Test setting and getting the inBoard_ property
        basicToken_.SetInBoard(true);
        Assert.IsTrue(basicToken_.GetInBoard());

        basicToken_.SetInBoard(false);
        Assert.IsFalse(basicToken_.GetInBoard());
    }

    [Test]
    public void TestFlipTokenCoroutine()
    {
        Assert.AreEqual(basicToken_.GetLetter(), "A");
        Assert.AreEqual(basicToken_.IsOnGreenFace(), false);

        // Test the FlipToken coroutine
        IEnumerator coroutine = basicToken_.FlipToken();
        while (coroutine.MoveNext()) { }
        
        Assert.AreEqual(basicToken_.GetLetter(), "E");
        Assert.AreEqual(basicToken_.IsOnGreenFace(), true);
    }

    [Test]
    public void TestUpdateContent()
    {
        // Test the UpdateContent method
        basicToken_.SetParameters("X", "Y", Color.red, Color.blue);
        Assert.AreEqual("X", basicToken_.GetLetter());
    }

    [UnityTest]
    public IEnumerator TestDoubleClickDetection()
    {
        Assert.AreEqual("A", basicToken_.GetLetter());
        Assert.AreEqual(false, basicToken_.IsOnGreenFace());

        // Simulate a double-click
        basicToken_.OnPointerClick(new PointerEventData(EventSystem.current));
        basicToken_.OnPointerClick(new PointerEventData(EventSystem.current));

        // Wait for the FlipToken coroutine to complete
        yield return new WaitForSeconds(
            50 * basicToken_.GetFlipDeltaDuration());

        // Assert that the FlipToken coroutine was triggered and completed
        Assert.AreEqual("E", basicToken_.GetLetter());
        Assert.AreEqual(true, basicToken_.IsOnGreenFace());
    }
}
