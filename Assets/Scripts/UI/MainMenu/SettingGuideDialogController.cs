using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Điều khiển dialog hướng dẫn. Không chứa logic âm thanh.
/// </summary>
public class GuideDialogController : MonoBehaviour
{
    [SerializeField] private GameObject dialogRoot;
    [SerializeField] private Button openbtn;
    [SerializeField] private Button closebtn;

    private void Awake()
    {
        if (openbtn != null)
        {
            openbtn.onClick.RemoveAllListeners();
            openbtn.onClick.AddListener(OpenDialog);
        }

        if (closebtn != null)
        {
            closebtn.onClick.RemoveAllListeners();
            closebtn.onClick.AddListener(CloseDialog);
        }

        if (dialogRoot != null)
            dialogRoot.SetActive(false);
    }

    public void OpenDialog()
    {
        if (dialogRoot != null)
            dialogRoot.SetActive(true);
    }

    public void CloseDialog()
    {
        if (dialogRoot != null)
            dialogRoot.SetActive(false);
    }
}
