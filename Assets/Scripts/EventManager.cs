using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class EventManager : MonoBehaviour
{
    [Header("--- Event UI ---")]
    public GameObject eventPanel;
    public TextMeshProUGUI titleText;
    public Button[] options;      // InspectorでSize=3にしてボタンを登録
    public TextMeshProUGUI[] optionTexts; // InspectorでSize=3にしてテキストを登録

    [Header("--- Roulette UI ---")]
    public GameObject roulettePanel;
    public TextMeshProUGUI rouletteText;

    public void ShowChoices(string title, string[] labels, UnityAction[] actions, bool[] interactable = null)
    {
        // 安全策: パネルを表示
        if (eventPanel) eventPanel.SetActive(true);
        if (titleText) titleText.text = title;

        // 配列の安全チェック
        int maxLen = (options != null) ? options.Length : 0;

        for (int i = 0; i < maxLen; i++)
        {
            if (options[i] == null) continue;

            if (i < labels.Length)
            {
                options[i].gameObject.SetActive(true);

                // テキスト設定
                if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null)
                {
                    optionTexts[i].text = labels[i];
                }

                // ボタン有効無効
                bool canPush = (interactable != null && i < interactable.Length) ? interactable[i] : true;
                options[i].interactable = canPush;

                // クリック処理設定
                options[i].onClick.RemoveAllListeners();
                int idx = i; // ローカル変数キャプチャ
                options[i].onClick.AddListener(() => {
                    ClosePanel(); // 押したら閉じる
                    if (actions != null && idx < actions.Length && actions[idx] != null)
                    {
                        actions[idx].Invoke();
                    }
                });
            }
            else
            {
                options[i].gameObject.SetActive(false);
            }
        }
    }

    public void ClosePanel()
    {
        if (eventPanel) eventPanel.SetActive(false);
    }

    public IEnumerator PlayRoulette(PlayerStats stats, bool protection)
    {
        if (roulettePanel) roulettePanel.SetActive(true);
        string[] texts = { "友+", "GP+", "GP-", "友-", "LvUP" };

        float t = 0;
        while (t < 1.0f)
        {
            if (rouletteText) rouletteText.text = texts[Random.Range(0, texts.Length)];
            yield return new WaitForSeconds(0.1f);
            t += 0.1f;
        }

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
        yield return new WaitForSeconds(1.5f);
        if (roulettePanel) roulettePanel.SetActive(false);
    }
}