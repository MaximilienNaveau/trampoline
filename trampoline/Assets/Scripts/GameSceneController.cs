using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

public class GameSceneController: MonoBehaviour
{
    private PlayerCounter playerCounter_;

    public enum GameScene {
        introduction_scene,
        solo_game_scene,
        multi_game_scene,
    }

    public void LoadScene()
    {
        // Retrieve the number of players
        playerCounter_ = FindAnyObjectByType<PlayerCounter>();
        Assert.IsTrue(playerCounter_ != null, "GameSceneController: PlayerCounter component is missing.");
        PlayerPrefs.SetInt("NumberOfPlayers", playerCounter_.GetNumberOfPlayer()); // Default to 1 if not set
        if (playerCounter_.GetNumberOfPlayer() == 1)
        {
            SceneManager.LoadScene(GameScene.solo_game_scene.ToString());
        }
        else
        {
            /// @todo: handle the multiplayer case.
            // SceneManager.LoadScene(GameScene.multi_game_scene.ToString());
        }
    }

    public static void LoadIntroductionScene()
    {
        SceneManager.LoadScene(GameScene.introduction_scene.ToString());
    }

    public static void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit!");
    }
}
