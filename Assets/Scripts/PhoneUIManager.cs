using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PhoneUIManager : MonoBehaviour
{
    [Header("--- Main Screen ---")]
    public TextMeshProUGUI logText;
    public ScrollRect scrollRect;

    [Header("--- Menu Buttons ---")]
    public Button btnFriend;
    public Button btnItem;
    public Button btnInfo;
    public Button btnBoy;

    [Header("--- Sub Panels ---")]
    public GameObject subPanelRoot;
    public GameObject friendListPanel;
    public GameObject itemListPanel;
    public GameObject infoPanel;
    public TextMeshProUGUI infoText;

    [Header("--- Prefabs & Roots ---")]
    public GameObject listItemPrefab;
    public Transform listContentRoot;

    // ダミーのItemData作成用（移動カード使用時）
    // ScriptableObject.CreateInstanceを使うため参照不要ですが、枠組みとして
    private GameManager gameManager;

    public void Initialize(GameManager gm)
    {
        gameManager = gm;
        btnFriend.onClick.AddListener(() => { OpenSubPanel(friendListPanel); RefreshFriendList(); });
        btnItem.onClick.AddListener(() => { OpenSubPanel(itemListPanel); RefreshItemList(); });
        btnInfo.onClick.AddListener(() => { OpenSubPanel(infoPanel); UpdateInfoText(); });
        btnBoy.onClick.AddListener(() => AddLog("男子・彼氏機能は準備中です。"));
        CloseSubPanels();
    }

    public void AddLog(string message)
    {
        if (logText == null) return;
        logText.text = $"> {message}\n" + logText.text;
        if (logText.text.Length > 2000) logText.text = logText.text.Substring(0, 2000);
    }

    public void CloseSubPanels()
    {
        if (subPanelRoot) subPanelRoot.SetActive(false);
        if (friendListPanel) friendListPanel.SetActive(false);
        if (itemListPanel) itemListPanel.SetActive(false);
        if (infoPanel) infoPanel.SetActive(false);
    }

    void OpenSubPanel(GameObject targetPanel)
    {
        CloseSubPanels();
        if (subPanelRoot) subPanelRoot.SetActive(true);
        if (targetPanel) targetPanel.SetActive(true);
    }

    void ClearList()
    {
        foreach (Transform child in listContentRoot) Destroy(child.gameObject);
    }

    // --- リスト表示修正部分 ---

    void RefreshFriendList()
    {
        ClearList();
        if (gameManager == null || gameManager.allFriends == null) return;

        foreach (var friend in gameManager.allFriends)
        {
            if (friend.isRecruited)
            {
                GameObject obj = Instantiate(listItemPrefab, listContentRoot);
                TextMeshProUGUI txt = obj.GetComponentInChildren<TextMeshProUGUI>();
                txt.text = $"★ {friend.characterName}\n<size=70%>{friend.introduction}</size>";
                obj.GetComponent<Button>().interactable = false;
            }
        }
        CreateCloseButton();
    }

    void RefreshItemList()
    {
        ClearList();
        PlayerStats stats = PlayerStats.Instance;

        // 1. 移動カードの表示
        for (int i = 0; i < stats.moveCards.Count; i++)
        {
            int steps = stats.moveCards[i];
            GameObject obj = Instantiate(listItemPrefab, listContentRoot);
            TextMeshProUGUI txt = obj.GetComponentInChildren<TextMeshProUGUI>();
            txt.text = $"移動カード ({steps})";

            Button btn = obj.GetComponent<Button>();
            btn.onClick.AddListener(() => {
                // 移動カードを使用する処理
                // ここで一時的なItemDataを作ってGameManagerに渡す
                ItemData tempItem = ScriptableObject.CreateInstance<ItemData>();
                tempItem.itemName = $"移動カード({steps})";
                tempItem.itemType = ItemType.MoveCard;
                tempItem.moveSteps = steps;

                gameManager.UseItem(tempItem);
                CloseSubPanels();
            });
        }

        // 2. その他のアイテムの表示
        foreach (var item in stats.otherItems)
        {
            GameObject obj = Instantiate(listItemPrefab, listContentRoot);
            TextMeshProUGUI txt = obj.GetComponentInChildren<TextMeshProUGUI>();
            txt.text = item.itemName;

            Button btn = obj.GetComponent<Button>();
            btn.onClick.AddListener(() => {
                gameManager.UseItem(item);
                CloseSubPanels();
            });
        }

        if (stats.moveCards.Count == 0 && stats.otherItems.Count == 0)
        {
            GameObject emptyObj = Instantiate(listItemPrefab, listContentRoot);
            emptyObj.GetComponentInChildren<TextMeshProUGUI>().text = "アイテムを持っていません";
            emptyObj.GetComponent<Button>().interactable = false;
        }

        CreateCloseButton();
    }

    void UpdateInfoText()
    {
        PlayerStats s = PlayerStats.Instance;
        if (infoText)
        {
            infoText.text = $"現在: {s.currentGrade}年生 {s.currentMonth}月\n" +
                            $"GP: {s.gp:N0}\n友達: {s.friends}人\n" +
                            $"移動カード所持: {s.moveCards.Count}/{PlayerStats.MaxMoveCards}";
        }
        CreateCloseButton();
    }

    void CreateCloseButton()
    {
        GameObject obj = Instantiate(listItemPrefab, listContentRoot);
        obj.GetComponentInChildren<TextMeshProUGUI>().text = "閉じる";
        obj.GetComponent<Button>().onClick.AddListener(CloseSubPanels);
    }
}