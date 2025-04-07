using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneController: MonoBehaviour
{
    private GameController gameController_;
    private PlayerCounter playerCounter_;

    public void Start()
    {
        gameController_ = FindAnyObjectByType<GameController>();
        playerCounter_ = FindAnyObjectByType<PlayerCounter>();
    }

    public enum GameScene {
        introduction_scene,
        solo_game_scene,
        multi_game_scene,
    }

    public void LoadScene()
    {
        gameController_.SetNumberOfPlayers(playerCounter_.GetNumberOfPlayer());
        if (playerCounter_.GetNumberOfPlayer() == 1)
        {
            SceneManager.LoadScene(GameScene.solo_game_scene.ToString());
        }
        else
        {
            SceneManager.LoadScene(GameScene.multi_game_scene.ToString());
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
