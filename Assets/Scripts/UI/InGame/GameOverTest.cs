using UnityEngine;
using UnityEngine.InputSystem;

public class GameOverTest : MonoBehaviour
{
    [SerializeField] private GameOverUIController gameOverUIController;

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.gKey.wasPressedThisFrame)
        {
            gameOverUIController.ShowDemoGameOver();
        }
    }
}
