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

    private GameManager gameManager;

    public void Initialize(GameManager gm)
    {
        gameManager = gm;
        // 各ボタンに機能を割り当て
        if (btnFriend) btnFriend.onClick.AddListener(() => { OpenSubPanel(friendListPanel); RefreshFriendList(); });
        if (btnItem) btnItem.onClick.AddListener(() => { OpenSubPanel(itemListPanel); RefreshItemList(); });
        if (btnInfo) btnInfo.onClick.AddListener(() => { OpenSubPanel(infoPanel); UpdateInfoText(); });
        if (btnBoy) btnBoy.onClick.AddListener(() => AddLog("男子・彼氏機能は準備中です。"));

        CloseSubPanels();
    }

    public void AddLog(string message)
    {
        if (logText == null) return;
        logText.text = $"> {message}\n" + logText.text;
        // 文字数制限
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

    // --- 親友リスト表示 ---
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
                obj.GetComponent<Button>().interactable = false; // 閲覧専用
            }
        }
        CreateCloseButton();
    }

    // --- アイテムリスト表示 (修正箇所) ---
    void RefreshItemList()
    {
        ClearList();
        PlayerStats stats = PlayerStats.Instance;

        // 1. 移動カード (List<ItemData> として処理)
        foreach (var card in stats.moveCards)
        {
            GameObject obj = Instantiate(listItemPrefab, listContentRoot);
            TextMeshProUGUI txt = obj.GetComponentInChildren<TextMeshProUGUI>();
            txt.text = card.itemName; // データ名をそのまま表示

            Button btn = obj.GetComponent<Button>();
            btn.onClick.AddListener(() => {
                // アイテム使用
                gameManager.UseItem(card);
                CloseSubPanels();
            });
        }

        // 2. その他のアイテム
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

        // 何も持っていない場合
        if (stats.moveCards.Count == 0 && stats.otherItems.Count == 0)
        {
            GameObject emptyObj = Instantiate(listItemPrefab, listContentRoot);
            emptyObj.GetComponentInChildren<TextMeshProUGUI>().text = "アイテムを持っていません";
            emptyObj.GetComponent<Button>().interactable = false;
        }

        CreateCloseButton();
    }

    // --- ステータス情報表示 ---
    void UpdateInfoText()
    {
        PlayerStats s = PlayerStats.Instance;
        if (infoText)
        {
            infoText.text = $"現在: {s.currentGrade}年生 {s.currentMonth}月\n" +
                            $"GP: {s.gp:N0}\n友達: {s.friends}人\n\n" +
                            $"[ステータス]\n" +
                            $"コミュ: {s.commuLv}  ギャル: {s.galLv}  レモン: {s.lemonLv}\n\n" +
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