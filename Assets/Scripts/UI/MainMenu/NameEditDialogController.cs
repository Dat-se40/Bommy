using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Popup nhỏ để sửa tên account.
/// </summary>
public class NameEditDialogController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("UI")]
    [SerializeField] private TMP_Text nameEditTitlelbl;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button closeSettingsbtn;

    [Header("Optional")]
    [SerializeField] private TMP_Text errorlbl;

    [Header("Rules")]
    [SerializeField] private int minLength = 1;
    [SerializeField] private int maxLength = 12;

    private Action<string> saveCallback;
    private bool initialized;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;

        if (root == null)
            root = gameObject;

        AutoBindIfMissing();

        if (nameEditTitlelbl != null)
            nameEditTitlelbl.text = "Edit Name";

        if (nameInput != null)
        {
            nameInput.characterLimit = maxLength;
            nameInput.onValueChanged.RemoveListener(OnInputChanged);
            nameInput.onValueChanged.AddListener(OnInputChanged);

            nameInput.onSubmit.RemoveListener(OnSubmitInput);
            nameInput.onSubmit.AddListener(OnSubmitInput);
        }

        if (saveButton != null)
        {
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(Save);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(CloseDialog);
        }

        if (closeSettingsbtn != null)
        {
            closeSettingsbtn.onClick.RemoveAllListeners();
            closeSettingsbtn.onClick.AddListener(CloseDialog);
        }
    }

    public void OpenDialog(string currentName, Action<string> onSave)
    {
        EnsureInitialized();

        SoundManager.Instance?.PlayOpenDialog();

        saveCallback = onSave;

        if (root != null)
        {
            root.SetActive(true);
            root.transform.SetAsLastSibling();
        }

        if (nameInput != null)
        {
            nameInput.text = currentName;
            nameInput.Select();
            nameInput.ActivateInputField();
        }

        ValidateInput();
    }

    public void CloseDialog()
    {
        EnsureInitialized();

        saveCallback = null;

        if (root != null)
            root.SetActive(false);
    }

    private void Save()
    {
        string cleanName = GetCleanName();

        if (!IsValidName(cleanName))
        {
            ValidateInput();
            return;
        }

        saveCallback?.Invoke(cleanName);
        CloseDialog();
    }

    private void OnInputChanged(string _)
    {
        ValidateInput();
    }

    private void OnSubmitInput(string _)
    {
        Save();
    }

    private void ValidateInput()
    {
        string cleanName = GetCleanName();
        bool valid = IsValidName(cleanName);

        if (saveButton != null)
            saveButton.interactable = valid;

        if (errorlbl != null)
        {
            if (valid)
                errorlbl.text = string.Empty;
            else
                errorlbl.text = $"Name {minLength}-{maxLength} chars";
        }
    }

    private string GetCleanName()
    {
        if (nameInput == null)
            return string.Empty;

        string value = nameInput.text;

        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        value = value.Replace("\n", string.Empty);
        value = value.Replace("\r", string.Empty);
        value = value.Trim();

        return value;
    }

    private bool IsValidName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Length >= minLength && value.Length <= maxLength;
    }

    private void AutoBindIfMissing()
    {
        if (nameEditTitlelbl == null)
            nameEditTitlelbl = FindChildComponent<TMP_Text>("NameEditTitlelbl");

        if (nameInput == null)
            nameInput = FindChildComponent<TMP_InputField>("NameInput");

        if (saveButton == null)
            saveButton = FindChildComponent<Button>("SaveButton");

        if (cancelButton == null)
            cancelButton = FindChildComponent<Button>("CancelButton");

        if (closeSettingsbtn == null)
            closeSettingsbtn = FindChildComponent<Button>("CloseSettingsbtn", "CloseButton", "Closebtn");

        if (errorlbl == null)
            errorlbl = FindChildComponent<TMP_Text>("Errorlbl", "ErrorLbl", "ErrorText");
    }

    private T FindChildComponent<T>(params string[] names) where T : Component
    {
        T[] components = GetComponentsInChildren<T>(true);

        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
                continue;

            string objectName = components[i].gameObject.name;

            for (int n = 0; n < names.Length; n++)
            {
                if (objectName == names[n])
                    return components[i];
            }
        }

        return null;
    }
}
