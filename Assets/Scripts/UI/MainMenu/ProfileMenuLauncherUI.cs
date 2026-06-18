using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class ProfileMenuLauncherUI : MonoBehaviour
{
    [SerializeField] Button button;

    void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    void OnEnable()
    {
        button.onClick.AddListener(OpenProfile);
    }

    void OnDisable()
    {
        button.onClick.RemoveListener(OpenProfile);
    }

    void OpenProfile()
    {
        ProfileFriendsPrototypeUI screen = ProfileFriendsPrototypeUI.EnsureExists();

        if (screen != null)
            screen.Open();
    }
}
