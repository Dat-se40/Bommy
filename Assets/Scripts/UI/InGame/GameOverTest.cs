using UnityEngine;

public class GameOverTest : MonoBehaviour
{
    [SerializeField] private GameOverUIController gameOverUIController;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            gameOverUIController.ShowDemoGameOver();
        }
    }
}
