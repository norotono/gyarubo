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

    // ★修正: 固定ボタンの変数は不要になったため削除しました
    // (dynamic generationを使うため)

    [Header("--- Dynamic Buttons ---")]
    public Transform buttonContainer;      // ボタンを並べる親オブジェクト
    public GameObject buttonPrefab;        // ボタンのプレハブ

    [Header("--- Roulette UI References ---")]
    public GameObject roulettePanel;
    public TextMeshProUGUI rouletteText;

    // 選択肢パネルを表示する（動的生成版）
    public void ShowChoicePanel(string title, string[] labels, UnityAction[] actions)
    {
        if (eventSelectionPanel == null) return;

        eventSelectionPanel.SetActive(true);
        if (eventTitleText != null) eventTitleText.text = title;

        // 1. 既存のボタンを全て削除（クリア）
        // これにより、前のイベントのボタンが残るのを防ぎます
        if (buttonContainer != null)
        {
            foreach (Transform child in buttonContainer)
            {
                Destroy(child.gameObject);
            }
        }

        // 2. 新しいボタンを生成
        for (int i = 0; i < labels.Length; i++)
        {
            // アクションの数がラベルより少ない場合のエラー回避
            if (i >= actions.Length) break;

            if (buttonPrefab != null && buttonContainer != null)
            {
                GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);
                Button btn = btnObj.GetComponent<Button>();
                TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();

                if (txt != null) txt.text = labels[i];

                // クリック時の動作登録
                int index = i; // クロージャ用の一時変数
                btn.onClick.AddListener(() =>
                {
                    actions[index].Invoke();
                    ClosePanel();
                });
            }
        }
    }

    public void ClosePanel()
    {
        if (eventSelectionPanel != null) eventSelectionPanel.SetActive(false);
    }

    // ★修正: ここにあった余計な '}' を削除し、古い SetupButton メソッドも削除しました

    // ルーレット演出
    public IEnumerator PlayRoulette(PlayerStats stats, bool hasBadEventProtection)
    {
        if (roulettePanel) roulettePanel.SetActive(true);
        string[] visualResults = { "友達+", "GP+", "GP-", "友達-", "ステUP" };

        // パラパラ演出
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

} // ★クラスの閉じ括弧はここ（一番最後）だけにする