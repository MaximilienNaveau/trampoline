using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;

public class StoreMultiplayerTests
{
    private GameObject storeObject_;
    private StoreMultiplayer store_;
    private GameObject tokenPoolObject_;
    private TokenPool tokenPool_;
    private GameObject turnManagerObject_;
    private TurnManager turnManager_;
    private GameObject tokenDistributorObject_;
    private TokenDistributor tokenDistributor_;
    private GameObject playerManagerObject_;
    private PlayerManager playerManager_;
    private GameObject canvasObject_;
    private Canvas canvas_;

    [SetUp]
    public void SetUp()
    {
        // Create canvas
        canvasObject_ = new GameObject("GameCanvas");
        canvasObject_.tag = "GameCanvas";
        canvas_ = canvasObject_.AddComponent<Canvas>();
        canvasObject_.AddComponent<CanvasScaler>();
        canvasObject_.AddComponent<GraphicRaycaster>();
        
        // Create EventSystem
        var eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<EventSystem>();
        eventSystemObj.AddComponent<StandaloneInputModule>();

        // Note: We skip creating TokenPool, TurnManager, TokenDistributor, and PlayerManager
        // because they require prefabs and full game setup. This is a simplified unit test
        // that only tests the token drop positioning logic.

        // Create Store container (without StoreMultiplayer component for this simple test)
        storeObject_ = new GameObject("Store");
        storeObject_.transform.SetParent(canvasObject_.transform);
        var rectTransform = storeObject_.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(400, 400);

        // Create some tiles for the store
        for (int i = 0; i < 9; i++)
        {
            var tileObj = new GameObject($"Tile_{i}");
            tileObj.transform.SetParent(storeObject_.transform);
            var tileRect = tileObj.AddComponent<RectTransform>();
            tileRect.sizeDelta = new Vector2(40, 40);
            tileRect.anchorMin = new Vector2(0.5f, 0.5f);
            tileRect.anchorMax = new Vector2(0.5f, 0.5f);
            tileRect.pivot = new Vector2(0.5f, 0.5f);
            tileObj.AddComponent<Tile>();
        }
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(storeObject_);
        Object.Destroy(canvasObject_);
    }

    [UnityTest]
    public IEnumerator TestTokenAttachesToTileProperly()
    {
        // This is a simplified test that focuses on the core positioning logic:
        // When a token is attached to a tile, it should be centered properly.
        
        yield return null;

        // Create a test token
        var tokenObj = CreateTestToken("A", "B");
        var token = tokenObj.GetComponent<BasicToken>();

        // Create a test tile
        var storeTileObj = new GameObject("StoreTile");
        storeTileObj.transform.SetParent(canvasObject_.transform);
        var storeTileRect = storeTileObj.AddComponent<RectTransform>();
        storeTileRect.sizeDelta = new Vector2(50, 50);
        storeTileRect.anchorMin = new Vector2(0.5f, 0.5f);
        storeTileRect.anchorMax = new Vector2(0.5f, 0.5f);
        storeTileRect.pivot = new Vector2(0.5f, 0.5f);
        storeTileRect.anchoredPosition = new Vector2(100, 100);
        var storeTile = storeTileObj.AddComponent<Tile>();

        Debug.Log($"TEST: Before attach - Token parent={token.transform.parent?.name}, Position={((RectTransform)token.transform).anchoredPosition}");

        // Attach token to tile (this is what happens in UpdateStorage)
        storeTile.AttachToken(token);

        Debug.Log($"TEST: After attach - Token parent={token.transform.parent?.name}, Position={((RectTransform)token.transform).anchoredPosition}");

        yield return null;

        // Verify token is properly centered on tile
        Assert.AreEqual(storeTile, token.GetTileUnder(), "Token should be under store tile");
        Assert.AreEqual(storeTileObj.transform, token.transform.parent, "Token should be parented to tile");
        
        var tokenRect = (RectTransform)token.transform;
        Assert.AreEqual(Vector2.zero, tokenRect.anchoredPosition, "Token should be centered at (0,0) relative to tile");
        Assert.AreEqual(new Vector2(0.5f, 0.5f), tokenRect.anchorMin, "Token anchors should be centered");
        Assert.AreEqual(new Vector2(0.5f, 0.5f), tokenRect.anchorMax, "Token anchors should be centered");
        Assert.AreEqual(new Vector2(0.5f, 0.5f), tokenRect.pivot, "Token pivot should be centered");
        
        Debug.Log($"TEST: SUCCESS - Token is properly centered on tile");

        // Cleanup
        Object.Destroy(tokenObj);
        Object.Destroy(storeTileObj);
    }

    private GameObject CreateTestToken(string letter1, string letter2)
    {
        var tokenObj = new GameObject("TestToken");
        tokenObj.transform.SetParent(canvasObject_.transform);
        tokenObj.AddComponent<RectTransform>();
        tokenObj.AddComponent<CanvasGroup>();

        // Create child GameObjects for TextMeshProUGUI components
        var textChild1 = new GameObject("TextChild1");
        textChild1.transform.SetParent(tokenObj.transform);
        textChild1.AddComponent<TextMeshProUGUI>();

        var textChild2 = new GameObject("TextChild2");
        textChild2.transform.SetParent(tokenObj.transform);
        textChild2.AddComponent<TextMeshProUGUI>();

        // Create child GameObjects for Image components
        var imageChild1 = new GameObject("ImageChild1");
        imageChild1.transform.SetParent(tokenObj.transform);
        imageChild1.AddComponent<Image>();

        var imageChild2 = new GameObject("ImageChild2");
        imageChild2.transform.SetParent(tokenObj.transform);
        imageChild2.AddComponent<Image>();

        // Add the BasicToken component
        var token = tokenObj.AddComponent<BasicToken>();
        token.SetParameters(letter1, letter2, MyGameColors.GetYellow(), MyGameColors.GetGreen());

        return tokenObj;
    }
}
