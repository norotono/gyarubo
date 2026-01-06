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

    // 固定ボタン参照
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
    public void ShowChoicePanel(string title, string[] labels, UnityAction[] actions)
    {
        if (eventSelectionPanel == null) return;

        eventSelectionPanel.SetActive(true);
        if (eventTitleText != null) eventTitleText.text = title;

        ResetButton(btnOption1);
        ResetButton(btnOption2);
        ResetButton(btnOption3);

        if (labels.Length > 0) SetupButton(btnOption1, txtOption1, labels[0], (actions.Length > 0) ? actions[0] : null);
        if (labels.Length > 1) SetupButton(btnOption2, txtOption2, labels[1], (actions.Length > 1) ? actions[1] : null);
        if (labels.Length > 2) SetupButton(btnOption3, txtOption3, labels[2], (actions.Length > 2) ? actions[2] : null);
    }

    public void ClosePanel()
    {
        if (eventSelectionPanel != null) eventSelectionPanel.SetActive(false);
    }

    private void ResetButton(Button btn)
    {
        if (btn != null)
        {
            btn.gameObject.SetActive(false);
            btn.onClick.RemoveAllListeners();
        }
    }

    private void SetupButton(Button btn, TextMeshProUGUI txt, string label, UnityAction action)
    {
        if (btn == null) return;
        btn.gameObject.SetActive(true);
        if (txt != null) txt.text = label;

        if (action != null)
        {
            btn.onClick.AddListener(() => { ClosePanel(); action.Invoke(); });
        }
        else
        {
            btn.onClick.AddListener(ClosePanel);
        }
    }

    // ★重要: ここで関数名を 'PlayRouletteSequence' と定義しています
    public IEnumerator PlayRouletteSequence(PlayerStats stats, int currentGrade, bool hasProtection, System.Action onComplete)
    {
        if (roulettePanel) roulettePanel.SetActive(true);

        string[] visualTexts = { "友達できるかな？", "お金拾うかも？", "落とし穴！？", "勉強中……", "ナンパ待ち……" };

        // 演出: パラパラ表示
        float duration = 1.5f;
        float timer = 0f;
        while (timer < duration)
        {
            if (rouletteText)
                rouletteText.text = visualTexts[Random.Range(0, visualTexts.Length)];
            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;
        }

        // 判定ロジック
        int roll = Random.Range(0, 100);
        string resultMsg = "";

        if (roll < 30) // 友達+
        {
            int val = currentGrade * 1 + stats.commuLv;
            stats.friends += val;
            resultMsg = $"友達が {val}人 できた！";
        }
        else if (roll < 55) // GP+
        {
            int gain = currentGrade * 150 + stats.galLv * 100;
            stats.gp += gain;
            resultMsg = $"臨時収入！ {gain} GP ゲット！";
        }
        else if (roll < 80) // GP-
        {
            if (hasProtection)
            {
                stats.gp += 100;
                resultMsg = "親友効果: 浪費を回避して GP+100！";
            }
            else
            {
                int loss = currentGrade * 100;
                stats.gp = Mathf.Max(0, stats.gp - loss);
                resultMsg = $"無駄遣いしてしまった…… GP -{loss}";
            }
        }
        else if (roll < 95) // 友達-
        {
            if (hasProtection)
            {
                stats.gp += 100;
                resultMsg = "親友効果: 喧嘩を回避して GP+100！";
            }
            else
            {
                if (stats.friends > 0) stats.friends--;
                resultMsg = "友達と喧嘩してしまった…… 友達 -1";
            }
        }
        else // ステータスUP
        {
            stats.commuLv++;
            resultMsg = "勉強してコミュ力が上がった！";
        }

        if (rouletteText) rouletteText.text = resultMsg;
        yield return new WaitForSeconds(2.0f);

        if (roulettePanel) roulettePanel.SetActive(false);

        // 完了通知
        onComplete?.Invoke();
    }


    // EventManager.cs クラス内に以下のメソッドを追加してください

    // ★追加: ボタン1つ（OKのみ）の簡易メッセージ表示用
    public void ShowMessage(string content, UnityAction onConfirm = null)
    {
        // 既存のShowChoicePanelを流用して、ボタン1つのウィンドウを出す
        ShowChoicePanel(
            content,
            new string[] { "OK" },
            new UnityAction[] { onConfirm }
        );
    }


    public void ShowOptions(string title, string description, string btn1Txt, string btn2Txt, string btn3Txt, UnityAction act1, UnityAction act2, UnityAction act3)
    {
        // 説明文(description)を表示する場所がない場合、タイトルに改行して結合して表示します
        string finalTitle = title;
        if (!string.IsNullOrEmpty(description))
        {
            finalTitle += "\n<size=80%>" + description + "</size>";
        }

        // リストを作成して既存の ShowChoicePanel に渡す
        List<string> labelList = new List<string>();
        List<UnityAction> actionList = new List<UnityAction>();

        // ボタン1
        if (!string.IsNullOrEmpty(btn1Txt)) { labelList.Add(btn1Txt); actionList.Add(act1); }
        // ボタン2
        if (!string.IsNullOrEmpty(btn2Txt)) { labelList.Add(btn2Txt); actionList.Add(act2); }
        // ボタン3
        if (!string.IsNullOrEmpty(btn3Txt)) { labelList.Add(btn3Txt); actionList.Add(act3); }

        // 既存のメソッドを呼び出す
        ShowChoicePanel(finalTitle, labelList.ToArray(), actionList.ToArray());
    }
}