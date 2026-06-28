using UnityEngine;

public class MainMenuSoundBootstrap : MonoBehaviour
{
    [SerializeField] SoundLibrary menuLibrary;

    void Start()
    {
        SoundManager.Instance.SetSceneLibrary(menuLibrary);
        SoundManager.Instance.PlayBgm(SoundKey.BgmMenu);
    }

    void OnDestroy()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.ClearSceneLibrary();
    }
}