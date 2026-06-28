using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Điều khiển ChangePasswordOverlay.
/// </summary>
public class ChangePasswordDialogController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject dialogRoot;

    [Header("Buttons")]
    [SerializeField] private Button closeChangePasswordbtn;
    [SerializeField] private Button savePasswordbtn;

    [Header("Inputs")]
    [SerializeField] private TMP_InputField oldPasswordInput;
    [SerializeField] private TMP_InputField newPasswordInput;
    [SerializeField] private TMP_InputField confirmNewPasswordInput;

    [Header("Status")]
    [SerializeField] private TMP_Text changePasswordStatuslbl;

    private void Awake()
    {
        BindButtons();

        if (dialogRoot != null)
            dialogRoot.SetActive(false);
    }

    private void BindButtons()
    {
        if (closeChangePasswordbtn != null)
        {
            closeChangePasswordbtn.onClick.RemoveAllListeners();
            closeChangePasswordbtn.onClick.AddListener(CloseDialog);
        }

        if (savePasswordbtn != null)
        {
            savePasswordbtn.onClick.RemoveAllListeners();
            savePasswordbtn.onClick.AddListener(SavePassword);
        }

        if (confirmNewPasswordInput != null)
        {
            confirmNewPasswordInput.onSubmit.RemoveAllListeners();
            confirmNewPasswordInput.onSubmit.AddListener(_ => SavePassword());
        }
    }

    public void OpenDialog()
    {
        SoundManager.Instance?.PlayOpenDialog();

        ClearInputs();
        SetStatus("");

        if (dialogRoot != null)
        {
            dialogRoot.SetActive(true);
            dialogRoot.transform.SetAsLastSibling();
        }

        if (oldPasswordInput != null)
        {
            oldPasswordInput.Select();
            oldPasswordInput.ActivateInputField();
        }
    }

    public void CloseDialog()
    {
        if (dialogRoot != null)
            dialogRoot.SetActive(false);
    }

    private void SavePassword()
    {
        string oldPassword = oldPasswordInput != null ? oldPasswordInput.text : "";
        string newPassword = newPasswordInput != null ? newPasswordInput.text : "";
        string confirmPassword = confirmNewPasswordInput != null ? confirmNewPasswordInput.text : "";

        if (string.IsNullOrEmpty(oldPassword))
        {
            SetStatus("Old password is empty.");
            return;
        }

        if (newPassword.Length < 8)
        {
            SetStatus("New password must contain at least 8 characters.");
            return;
        }

        if (newPassword != confirmPassword)
        {
            SetStatus("Passwords do not match.");
            return;
        }

        // TODO[ACCOUNT]: Gọi backend đổi mật khẩu thật.
        SetStatus("Password changed.");
        ClearInputs();
    }

    private void ClearInputs()
    {
        if (oldPasswordInput != null)
            oldPasswordInput.text = "";

        if (newPasswordInput != null)
            newPasswordInput.text = "";

        if (confirmNewPasswordInput != null)
            confirmNewPasswordInput.text = "";
    }

    private void SetStatus(string message)
    {
        if (changePasswordStatuslbl != null)
            changePasswordStatuslbl.text = message;
    }
}
