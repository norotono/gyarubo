using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshProを使用
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

    // --- モード切替 ---

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
        if (itemPanel) itemPanel.SetActive(false); // ダイアログ中はアイテム欄を閉じる

        if (dialogText) dialogText.text = message;

        // 既存の選択肢をクリア
        foreach (Transform child in choiceRoot) Destroy(child.gameObject);
    }

    // 外部からログを追加するための関数
    public void AddLog(string message)
    {
        if (logText != null)
        {
            // 新しいログを上に追加していく
            logText.text = $"> {message}\n" + logText.text;

            // 文字数が多すぎたら古いものをカット（メモリ対策）
            if (logText.text.Length > 1000)
                logText.text = logText.text.Substring(0, 1000);
        }
    }

    // --- 選択肢ボタン生成 ---
    public void CreateChoiceButton(string text, UnityEngine.Events.UnityAction onClickAction)
    {
        GameObject btnObj = Instantiate(choiceButtonPrefab, choiceRoot);
        btnObj.GetComponentInChildren<TextMeshProUGUI>().text = text;
        btnObj.GetComponent<Button>().onClick.AddListener(onClickAction);
    }

    // --- ステータス選択ダイアログ ---
    public void ShowStatusSelection(UnityEngine.Events.UnityAction<string> onSelect, UnityEngine.Events.UnityAction onCancel)
    {
        ShowDialogMode("どのステータスを上げますか？");

        CreateChoiceButton("コミュ力", () => onSelect("Commu"));
        CreateChoiceButton("ギャル力", () => onSelect("Gal"));
        CreateChoiceButton("レモン力", () => onSelect("Lemon"));
        CreateChoiceButton("やめる", onCancel);
    }

    // --- アイテムメニュー表示 ---
    // stats: 所持データ, onUseCard: カード使用時のコールバック(index), onUseItem: その他アイテム使用時のコールバック(key)
    public void ShowItemMenu(PlayerStats stats, UnityEngine.Events.UnityAction<int> onUseCard, UnityEngine.Events.UnityAction<string> onUseItem)
    {
        if (itemPanel) itemPanel.SetActive(true);

        // リストをリセット
        foreach (Transform child in itemListRoot) Destroy(child.gameObject);

        // 1. 移動カードの表示 (数字ごとにボタン化)
        if (stats.moveCards.Count > 0)
        {
            for (int i = 0; i < stats.moveCards.Count; i++)
            {
                int cardValue = stats.moveCards[i];
                int listIndex = i; // クロージャキャプチャ用

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