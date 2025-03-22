using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameSceneController
{
    public enum GameScene {
        introduction_scene,
        solo_game_scene,
        multi_game_scene,
    }

    private static void LoadScene(GameScene scene)
    {
        SceneManager.LoadScene(scene.ToString());
    }

    public static void LoadSoloGameScene()
    {
        LoadScene(GameScene.solo_game_scene);
    }

    public static void LoadMultiGameScene()
    {
        LoadScene(GameScene.multi_game_scene);
    }

    public static void LoadIntroductionScene()
    {
        LoadScene(GameScene.introduction_scene);
    }

    public static void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit!");
    }
}
