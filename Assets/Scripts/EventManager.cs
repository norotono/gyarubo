using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class EventManager : MonoBehaviour
{
    [Header("--- Event UI References ---")]
    public GameObject eventSelectionPanel;
    public TextMeshProUGUI eventTitleText;

    public Button btnOption1;
    public Button btnOption2;
    public Button btnOption3;

    public TextMeshProUGUI txtOption1;
    public TextMeshProUGUI txtOption2;
    public TextMeshProUGUI txtOption3;

    [Header("--- Roulette UI References ---")]
    public GameObject roulettePanel;
    public TextMeshProUGUI rouletteText;

    // 選択肢パネルを表示する
    // actionsにはボタン1, 2, 3が押されたときの処理（Action）を渡す
    public void ShowChoicePanel(string title, string[] labels, UnityAction[] actions)
    {
        if (eventSelectionPanel) eventSelectionPanel.SetActive(true);
        if (eventTitleText) eventTitleText.text = title;

        // ボタン1設定
        SetupButton(btnOption1, txtOption1, labels, actions, 0);
        // ボタン2設定
        SetupButton(btnOption2, txtOption2, labels, actions, 1);
        // ボタン3設定
        SetupButton(btnOption3, txtOption3, labels, actions, 2);
    }

    void SetupButton(Button btn, TextMeshProUGUI txt, string[] labels, UnityAction[] actions, int index)
    {
        if (btn == null) return;

        btn.onClick.RemoveAllListeners();

        if (index < labels.Length && !string.IsNullOrEmpty(labels[index]))
        {
            btn.gameObject.SetActive(true);
            btn.interactable = true; // 初期化
            if (txt) txt.text = labels[index];

            if (index < actions.Length && actions[index] != null)
            {
                btn.onClick.AddListener(() => {
                    ClosePanel();
                    actions[index].Invoke();
                });
            }
            else
            {
                // アクションがない場合は閉じるだけ
                btn.onClick.AddListener(ClosePanel);
            }
        }
        else
        {
            // ラベルがない場合は非表示
            btn.gameObject.SetActive(false);
        }
    }

    public void ClosePanel()
    {
        if (eventSelectionPanel) eventSelectionPanel.SetActive(false);
    }

    // 特定のボタンのInteractableを変更する（同伴可否などで使用）
    public void SetButtonInteractable(int buttonIndex, bool interactable)
    {
        if (buttonIndex == 0 && btnOption1) btnOption1.interactable = interactable;
        if (buttonIndex == 1 && btnOption2) btnOption2.interactable = interactable;
        if (buttonIndex == 2 && btnOption3) btnOption3.interactable = interactable;
    }

    // ルーレット演出
    public IEnumerator PlayRoulette(PlayerStats stats, bool hasBadEventProtection)
    {
        if (roulettePanel) roulettePanel.SetActive(true);
        string[] visualResults = { "友達+", "GP+", "GP-", "友達-", "ステUP" };

        float timer = 0f;
        while (timer < 1.5f)
        {
            if (rouletteText) rouletteText.text = visualResults[Random.Range(0, visualResults.Length)];
            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;
        }

        // 内部判定
        int roll = Random.Range(0, 100);
        string resultMsg = "";

        if (roll < 30)
        {
            stats.friends += 1;
            resultMsg = "友達ゲット！";
        }
        else if (roll < 55)
        {
            stats.gp += 300;
            resultMsg = "GPゲット！";
        }
        else if (roll < 80)
        {
            if (hasBadEventProtection)
            {
                stats.gp += 100;
                resultMsg = "親友効果: GP+100";
            }
            else
            {
                stats.gp = Mathf.Max(0, stats.gp - 200);
                resultMsg = "GP減少...";
            }
        }
        else if (roll < 95)
        {
            if (hasBadEventProtection)
            {
                stats.gp += 100;
                resultMsg = "親友効果: GP+100";
            }
            else
            {
                if (stats.friends > 0) stats.friends--;
                resultMsg = "友達減少...";
            }
        }
        else
        {
            stats.commuLv++;
            resultMsg = "ステータスUP!";
        }

        if (rouletteText) rouletteText.text = resultMsg;
        yield return new WaitForSeconds(1.5f);

        if (roulettePanel) roulettePanel.SetActive(false);
    }
}