using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PhoneUIManager : MonoBehaviour
{
    [Header("Areas")]
    public GameObject diceArea;       // ダイスボタンなどがあるエリア
    public GameObject dialogScroll;   // 会話や選択肢を表示するエリア
    public GameObject menuArea;       // 下部メニューエリア

    [Header("Item Menu Elements")]
    public GameObject itemPanel;      // アイテム一覧のパネル全体
    public Transform itemListRoot;    // アイテムボタンを並べる親オブジェクト
    public GameObject itemButtonPrefab; // 生成するボタンのプレハブ

    [Header("Text Components")]
    public TextMeshProUGUI logText;
    public TextMeshProUGUI dialogText;
    public Transform choiceRoot;      // 選択肢ボタンの親オブジェクト
    public GameObject choiceButtonPrefab;

    // ★追加: ステータス表示・ログ制御用
    [Header("Status & Log Settings")]
    public TextMeshProUGUI headerDateText; // 画面上部の日付
    public TextMeshProUGUI statusText;     // ステータス表示
    public ScrollRect logScrollRect;       // ログのスクロールビュー
    public PlayerStats playerStats;        // ステータス参照用

    private void Start()
    {
        if (playerStats == null) playerStats = PlayerStats.Instance;
        UpdateStatusUI();
    }

    // --- ★追加: GameManagerから呼ばれるステータス更新 ---
    public void UpdateStatusUI()
    {
        if (playerStats == null) playerStats = PlayerStats.Instance;
        if (playerStats == null) return;

        // 日付の更新
        if (headerDateText != null)
        {
            headerDateText.text = $"{playerStats.currentGrade}年 {playerStats.currentMonth}月";
        }

        // ステータスの更新
        if (statusText != null)
        {
            // 必要な情報を表示（レイアウトに合わせて調整してください）
            statusText.text = $"GP: {playerStats.gp:N0}\n" +
                              $"友: {playerStats.friends}人\n" +
                              $"コミュ: {playerStats.GetEffectiveCommuLv()}\n" +
                              $"ギャル: {playerStats.GetEffectiveGalLv()}\n" +
                              $"レモン: {playerStats.GetEffectiveLemonLv()}";
        }
    }

    // --- ★追加: GameManagerから呼ばれるログ追加 ---
    public void AddLog(string message)
    {
        if (logText != null)
        {
            logText.text += $"\n{message}";

            // ログの自動スクロール（必要であれば）
            if (logScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                logScrollRect.verticalNormalizedPosition = 0f;
            }
        }
        else
        {
            Debug.Log($"[PhoneLog] {message}");
        }
    }

    // --- 以下、既存のモード切替・アイテム表示処理 ---

    public void ShowDiceMode()
    {
        if (diceArea) diceArea.SetActive(true);
        if (dialogScroll) dialogScroll.SetActive(false);
        if (itemPanel) itemPanel.SetActive(false);
        if (menuArea) menuArea.SetActive(true);
    }

    public void ShowDialogMode(string message)
    {
        if (diceArea) diceArea.SetActive(false);
        if (dialogScroll) dialogScroll.SetActive(true);
        if (itemPanel) itemPanel.SetActive(false);

        if (dialogText) dialogText.text = message;

        // 既存の選択肢をクリア
        foreach (Transform child in choiceRoot) Destroy(child.gameObject);
    }

    // 選択肢ボタン生成
    public void CreateChoiceButton(string label, UnityEngine.Events.UnityAction onClick)
    {
        if (!choiceButtonPrefab) return;
        GameObject btn = Instantiate(choiceButtonPrefab, choiceRoot);
        btn.GetComponentInChildren<TextMeshProUGUI>().text = label;
        btn.GetComponent<Button>().onClick.AddListener(onClick);
    }

    // アイテムメニュー表示（コールバック付き）
    public void ShowItemMenu(PlayerStats stats, System.Action<string> onUseItem, System.Action<int> onUseCard)
    {
        if (!itemPanel) return;

        itemPanel.SetActive(true);
        // 中身をクリア
        foreach (Transform child in itemListRoot) Destroy(child.gameObject);

        // 1. 移動カード表示
        if (stats.moveCards.Count > 0)
        {
            for (int i = 0; i < stats.moveCards.Count; i++)
            {
                int cardValue = stats.moveCards[i];
                int listIndex = cardValue; // カードの値を使用

                GameObject btn = Instantiate(itemButtonPrefab, itemListRoot);
                btn.GetComponentInChildren<TextMeshProUGUI>().text = $"移動 [{cardValue}]";
                btn.GetComponent<Button>().onClick.AddListener(() => {
                    itemPanel.SetActive(false); // 閉じてから実行
                    onUseCard(listIndex);
                });
            }
        }
        else
        {
            // カードがない時の表示
            GameObject btn = Instantiate(itemButtonPrefab, itemListRoot);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = "移動カードなし";
            btn.GetComponent<Button>().interactable = false;
        }

        // 2. その他の所持アイテム表示
        if (stats.studentIdCount > 0) CreateItemBtn($"生徒手帳 x{stats.studentIdCount}", () => { itemPanel.SetActive(false); onUseItem("StudentId"); });
        if (stats.present > 0) CreateItemBtn($"プレゼント x{stats.present}", () => { itemPanel.SetActive(false); onUseItem("Present"); });
        if (stats.eventForce > 0) CreateItemBtn($"イベント強制 x{stats.eventForce}", () => { itemPanel.SetActive(false); onUseItem("EventForce"); });

        // 閉じるボタン
        CreateItemBtn("閉じる", () => itemPanel.SetActive(false));
    }

    void CreateItemBtn(string label, UnityEngine.Events.UnityAction action)
    {
        GameObject btn = Instantiate(itemButtonPrefab, itemListRoot);
        btn.GetComponentInChildren<TextMeshProUGUI>().text = label;
        btn.GetComponent<Button>().onClick.AddListener(action);
    }
}