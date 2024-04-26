using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneController : MonoBehaviour
{
    private void OnEnable()
    {
        SceneManager.LoadScene("main_scene");
    }
}
