using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MailRowUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image mailIcon;
    [SerializeField] private TMP_Text mailSubjectlbl;
    [SerializeField] private TMP_Text mailSenderlbl;
    [SerializeField] private TMP_Text mailDatelbl;
    [SerializeField] private GameObject unreadDot;

    [Header("Button")]
    [SerializeField] private Button rowbtn;

    private string mailId;
    private Action<string> onClick;

    public void Setup(
        string id,
        Sprite icon,
        string subject,
        string sender,
        string date,
        string expireText,
        bool unread,
        Action<string> clickCallback)
    {
        mailId = id;
        onClick = clickCallback;

        if (mailIcon != null && icon != null)
            mailIcon.sprite = icon;

        if (mailSubjectlbl != null)
            mailSubjectlbl.text = subject;

        if (mailSenderlbl != null)
            mailSenderlbl.text = sender;

        if (mailDatelbl != null)
            mailDatelbl.text = date + "\n" + expireText;

        if (unreadDot != null)
            unreadDot.SetActive(unread);

        if (rowbtn == null)
            rowbtn = GetComponent<Button>();

        if (rowbtn != null)
        {
            rowbtn.onClick.RemoveAllListeners();
            rowbtn.onClick.AddListener(() => onClick?.Invoke(mailId));
        }
    }
}
