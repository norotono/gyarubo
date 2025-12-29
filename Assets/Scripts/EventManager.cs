using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class EventManager : MonoBehaviour
{
    [Header("--- Event UI ---")]
    public GameObject eventPanel;         // パネル全体
    public TextMeshProUGUI titleText;     // タイトル
    public Button[] options;              // 選択肢ボタン (Size 3 推奨)
    public TextMeshProUGUI[] optionTexts; // ボタン内のテキスト (Size 3 推奨)

    [Header("--- Roulette UI ---")]
    public GameObject roulettePanel;
    public TextMeshProUGUI rouletteText;

    /// <summary>
    /// 選択肢パネルを表示し、ボタンが押されるまで待機する仕組みを作ります。
    /// </summary>
    public void ShowChoices(string title, string[] labels, UnityAction[] actions, bool[] interactable = null)
    {
        // パネルを強制的に表示
        if (eventPanel) eventPanel.SetActive(true);
        if (titleText) titleText.text = title;

        int maxLen = (options != null) ? options.Length : 0;

        for (int i = 0; i < maxLen; i++)
        {
            if (options[i] == null) continue;

            // ラベルがある分だけボタンを表示
            if (i < labels.Length)
            {
                options[i].gameObject.SetActive(true);

                // テキスト設定
                if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null)
                {
                    optionTexts[i].text = labels[i];
                }

                // ボタンが押せるかどうか (interactable)
                bool canPush = (interactable != null && i < interactable.Length) ? interactable[i] : true;
                options[i].interactable = canPush;

                // ★重要: 前回のリスナーを消さないと、前のイベントの処理が残ってしまう
                options[i].onClick.RemoveAllListeners();

                int idx = i; // ローカル変数のキャプチャ

                // ★重要: ボタンを押した時の処理
                options[i].onClick.AddListener(() => {
                    // 1. まずパネルを閉じる
                    ClosePanel();

                    // 2. その後、各ボタンに割り当てられたアクションを実行
                    if (actions != null && idx < actions.Length && actions[idx] != null)
                    {
                        actions[idx].Invoke();
                    }
                });
            }
            else
            {
                // 使わないボタンは非表示
                options[i].gameObject.SetActive(false);
            }
        }
    }

    // パネルを閉じる関数
    public void ClosePanel()
    {
        if (eventPanel != null)
        {
            eventPanel.SetActive(false);
        }
    }

    // ルーレット演出
    public IEnumerator PlayRoulette(PlayerStats stats, bool protection)
    {
        if (roulettePanel) roulettePanel.SetActive(true);
        string[] texts = { "友+", "GP+", "GP-", "友-", "LvUP" };

        // 回転演出
        float t = 0;
        while (t < 1.0f)
        {
            if (rouletteText) rouletteText.text = texts[Random.Range(0, texts.Length)];
            yield return new WaitForSeconds(0.1f);
            t += 0.1f;
        }

        // 結果決定
        int r = Random.Range(0, 100);
        string msg = "";

        if (r < 30) { stats.friends++; msg = "友達ゲット！"; }
        else if (r < 55) { stats.gp += 300; msg = "GPゲット！"; }
        else if (r < 80)
        {
            if (protection) { stats.gp += 100; msg = "親友効果: GP+100"; }
            else { stats.gp = Mathf.Max(0, stats.gp - 200); msg = "GP減少..."; }
        }
        else if (r < 95)
        {
            if (protection) { stats.gp += 100; msg = "親友効果: GP+100"; }
            else { if (stats.friends > 0) stats.friends--; msg = "友達減少..."; }
        }
        else { stats.commuLv++; msg = "ステータスUP!"; }

        if (rouletteText) rouletteText.text = msg;

        // 結果を見せる時間
        yield return new WaitForSeconds(1.5f);

        if (roulettePanel) roulettePanel.SetActive(false);
    }
}