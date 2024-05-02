using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneController: MonoBehaviour
{
    public enum GameScene {
        introduction_scene,
        rules_scenes,
        solo_game_scene,
        multiplayer_scene,
    }

    public static void Load(GameScene scene)
    {
        SceneManager.LoadScene(scene.ToString());
    }

    public static void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit!");
    }
}
