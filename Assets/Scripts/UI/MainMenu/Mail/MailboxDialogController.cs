using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Điều khiển MailboxOverlay và ReadMailOverlay.
/// Hiện tại dùng mail demo local, sau này thay bằng backend.
/// </summary>
public class MailboxDialogController : MonoBehaviour
{
    [System.Serializable]
    private class RewardData
    {
        public Sprite icon;
        public int amount;
    }

    private class MailData
    {
        public string id;
        public Sprite icon;
        public string subject;
        public string sender;
        public string date;
        public string expireText;
        public string content;
        public bool read;
        public bool claimed;
        public List<RewardData> rewards = new();
    }

    [Header("Mailbox Root")]
    [SerializeField] private GameObject mailboxOverlay;
    [SerializeField] private Button closeMailboxbtn;

    [Header("Mailbox UI")]
    [SerializeField] private TMP_Text mailCountlbl;
    [SerializeField] private Transform mailListContent;
    [SerializeField] private MailRowUI mailRowTemplate;
    [SerializeField] private Button claimAllbtn;
    [SerializeField] private Button deleteReadbtn;

    [Header("Read Mail Root")]
    [SerializeField] private GameObject readMailOverlay;
    [SerializeField] private Button closeReadMailbtn;

    [Header("Read Mail UI")]
    [SerializeField] private TMP_Text readMailTitlelbl;
    [SerializeField] private TMP_Text readMailSenderlbl;
    [SerializeField] private TMP_Text readMailDatelbl;
    [SerializeField] private TMP_Text readMailContentlbl;
    [SerializeField] private Transform rewardList;
    [SerializeField] private RewardItemUI rewardItemTemplate;
    [SerializeField] private Button claimMailbtn;
    [SerializeField] private Button deleteMailbtn;

    [Header("Demo Icons")]
    [SerializeField] private Sprite giftIcon;
    [SerializeField] private Sprite coinIcon;
    [SerializeField] private Sprite gemIcon;
    [SerializeField] private Sprite potionIcon;
    [SerializeField] private Sprite leafIcon;

    private readonly List<MailData> mails = new();
    private readonly List<MailRowUI> spawnedRows = new();
    private readonly List<RewardItemUI> spawnedRewards = new();

    private string selectedMailId;

    private void Awake()
    {
        BindButtons();
        SeedDemoMail();

        if (mailRowTemplate != null)
            mailRowTemplate.gameObject.SetActive(false);

        if (rewardItemTemplate != null)
            rewardItemTemplate.gameObject.SetActive(false);

        if (mailboxOverlay != null)
            mailboxOverlay.SetActive(false);

        if (readMailOverlay != null)
            readMailOverlay.SetActive(false);
    }

    private void BindButtons()
    {
        if (closeMailboxbtn != null)
        {
            closeMailboxbtn.onClick.RemoveAllListeners();
            closeMailboxbtn.onClick.AddListener(CloseDialog);
        }

        if (closeReadMailbtn != null)
        {
            closeReadMailbtn.onClick.RemoveAllListeners();
            closeReadMailbtn.onClick.AddListener(CloseReadMail);
        }

        if (claimAllbtn != null)
        {
            claimAllbtn.onClick.RemoveAllListeners();
            claimAllbtn.onClick.AddListener(ClaimAllMail);
        }

        if (deleteReadbtn != null)
        {
            deleteReadbtn.onClick.RemoveAllListeners();
            deleteReadbtn.onClick.AddListener(DeleteReadMail);
        }

        if (claimMailbtn != null)
        {
            claimMailbtn.onClick.RemoveAllListeners();
            claimMailbtn.onClick.AddListener(ClaimCurrentMail);
        }

        if (deleteMailbtn != null)
        {
            deleteMailbtn.onClick.RemoveAllListeners();
            deleteMailbtn.onClick.AddListener(DeleteCurrentMail);
        }

    }

    private void RefreshReadMailButtons(MailData mail)
    {
        if (mail == null)
            return;

        bool hasReward = mail.rewards.Count > 0;
        bool canClaim = hasReward && !mail.claimed;

        if (claimMailbtn != null)
        {
            claimMailbtn.interactable = canClaim;

            TMP_Text label = claimMailbtn.GetComponentInChildren<TMP_Text>();

            if (label != null)
                label.text = mail.claimed ? "CLAIMED" : hasReward ? "CLAIM" : "NO REWARD";
        }

        if (deleteMailbtn != null)
        {
            // Không cho xóa thư còn quà chưa nhận để tránh mất reward.
            deleteMailbtn.interactable = !hasReward || mail.claimed;
        }
    }

    public void OpenDialog()
    {
        SoundManager.Instance?.PlayOpenDialog();

        if (mailboxOverlay != null)
        {
            mailboxOverlay.SetActive(true);
            mailboxOverlay.transform.SetAsLastSibling();
        }

        RefreshMailbox();
    }

    public void CloseDialog()
    {
        if (mailboxOverlay != null)
            mailboxOverlay.SetActive(false);

        CloseReadMail();
    }

    private void CloseReadMail()
    {
        if (readMailOverlay != null)
            readMailOverlay.SetActive(false);
    }

    private void SeedDemoMail()
    {
        if (mails.Count > 0)
            return;

        mails.Add(new MailData
        {
            id = "mail_001",
            icon = giftIcon,
            subject = "Daily Login Gift",
            sender = "System Mail",
            date = "29/05/2024",
            expireText = "29 days",
            read = false,
            claimed = false,
            content = "Hello Bonymon!\nThanks for joining the game.\nThis is your day 1 login gift.\n\nHave fun!",
            rewards = new List<RewardData>
            {
                new RewardData { icon = gemIcon, amount = 100 },
                new RewardData { icon = coinIcon, amount = 1000 }
            }
        });

        mails.Add(new MailData
        {
            id = "mail_002",
            icon = giftIcon,
            subject = "Server Maintenance",
            sender = "System Mail",
            date = "29/05/2024",
            expireText = "29 days",
            read = false,
            claimed = false,
            content = "Server maintenance has ended.\nPlease accept this compensation reward.",
            rewards = new List<RewardData>
            {
                new RewardData { icon = potionIcon, amount = 5 },
                new RewardData { icon = leafIcon, amount = 10 }
            }
        });

        mails.Add(new MailData
        {
            id = "mail_003",
            icon = giftIcon,
            subject = "Special Event",
            sender = "System Mail",
            date = "28/05/2024",
            expireText = "28 days",
            read = true,
            claimed = false,
            content = "A special event has started.\nHere is a small gift for your adventure.",
            rewards = new List<RewardData>
            {
                new RewardData { icon = gemIcon, amount = 50 }
            }
        });
    }

    private void RefreshMailbox()
    {
        ClearMailRows();

        if (mailCountlbl != null)
            mailCountlbl.text = "MAIL: " + mails.Count + "/100";

        if (mailListContent == null || mailRowTemplate == null)
            return;

        for (int i = 0; i < mails.Count; i++)
        {
            MailData mail = mails[i];

            MailRowUI row = Instantiate(mailRowTemplate, mailListContent);
            row.gameObject.SetActive(true);

            string subject = mail.claimed
                ? mail.subject + " ✓"
                : mail.subject;

            row.Setup(
                mail.id,
                mail.icon,
                subject,
                mail.sender,
                mail.date,
                mail.expireText,
                !mail.read,
                OpenReadMail
            );

            spawnedRows.Add(row);
        }
    }

    private void OpenReadMail(string mailId)
    {
        MailData mail = FindMail(mailId);

        if (mail == null)
            return;

        selectedMailId = mailId;
        mail.read = true;

        if (readMailTitlelbl != null)
            readMailTitlelbl.text = mail.subject;

        if (readMailSenderlbl != null)
            readMailSenderlbl.text = mail.sender;

        if (readMailDatelbl != null)
            readMailDatelbl.text = mail.date;

        if (readMailContentlbl != null)
            readMailContentlbl.text = mail.content;

        RefreshRewards(mail);
        RefreshMailbox();

        if (readMailOverlay != null)
        {
            readMailOverlay.SetActive(true);
            readMailOverlay.transform.SetAsLastSibling();
        }
    }

    private void RefreshRewards(MailData mail)
    {
        ClearRewards();

        if (rewardList == null || rewardItemTemplate == null)
            return;

        for (int i = 0; i < mail.rewards.Count; i++)
        {
            RewardData reward = mail.rewards[i];

            RewardItemUI item = Instantiate(rewardItemTemplate, rewardList);
            item.gameObject.SetActive(true);
            item.Setup(reward.icon, reward.amount);

            spawnedRewards.Add(item);
        }

        RefreshReadMailButtons(mail);

    }
    private void DeleteCurrentMail()
    {
        MailData mail = FindMail(selectedMailId);

        if (mail == null)
            return;

        // Không xóa thư còn reward chưa nhận.
        if (mail.rewards.Count > 0 && !mail.claimed)
            return;

        mails.Remove(mail);
        selectedMailId = "";

        CloseReadMail();
        RefreshMailbox();
    }

    private void ClaimCurrentMail()
    {
        MailData mail = FindMail(selectedMailId);

        if (mail == null || mail.claimed || mail.rewards.Count == 0)
            return;

        // TODO[MAIL]: Cộng item/reward vào inventory thật.
        mail.claimed = true;
        mail.read = true;

        RefreshRewards(mail);
        RefreshMailbox();
    }

    private void ClaimAllMail()
    {
        for (int i = 0; i < mails.Count; i++)
        {
            if (mails[i].rewards.Count == 0)
                continue;

            // TODO[MAIL]: Cộng tất cả reward chưa nhận vào inventory thật.
            mails[i].claimed = true;
            mails[i].read = true;
        }

        RefreshMailbox();

        MailData selected = FindMail(selectedMailId);

        if (selected != null)
            RefreshRewards(selected);
    }

    private void DeleteReadMail()
    {
        for (int i = mails.Count - 1; i >= 0; i--)
        {
            if (mails[i].read && mails[i].claimed)
                mails.RemoveAt(i);
        }

        selectedMailId = "";
        CloseReadMail();
        RefreshMailbox();
    }

    private MailData FindMail(string mailId)
    {
        for (int i = 0; i < mails.Count; i++)
        {
            if (mails[i].id == mailId)
                return mails[i];
        }

        return null;
    }

    private void ClearMailRows()
    {
        for (int i = 0; i < spawnedRows.Count; i++)
        {
            if (spawnedRows[i] != null)
                Destroy(spawnedRows[i].gameObject);
        }

        spawnedRows.Clear();
    }

    private void ClearRewards()
    {
        for (int i = 0; i < spawnedRewards.Count; i++)
        {
            if (spawnedRewards[i] != null)
                Destroy(spawnedRewards[i].gameObject);
        }

        spawnedRewards.Clear();
    }
}
