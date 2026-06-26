using UnityEngine;

public class GameSceneSoundBootstrap : MonoBehaviour
{
    [SerializeField] SoundLibrary gameLibrary;

    void Start()
    {
        SoundManager.Instance.SetSceneLibrary(gameLibrary);
        SoundManager.Instance.PlayBgm(SoundKey.BgmInGame);
    }

    void OnDestroy()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.ClearSceneLibrary();
    }
}