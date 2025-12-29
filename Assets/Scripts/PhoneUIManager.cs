using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class PhoneUIManager : MonoBehaviour
{
    [Header("--- Log UI ---")]
    public Transform logContent;       // ScrollViewのContent
    public GameObject logTextPrefab;   // テキストのプレハブ

    // ログを追加する機能（GameManagerやUIManagerから呼ばれる）
    public void AddLog(string message)
    {
        if (logContent == null || logTextPrefab == null) return;

        GameObject obj = Instantiate(logTextPrefab, logContent);
        TextMeshProUGUI txt = obj.GetComponent<TextMeshProUGUI>();

        // 現在時刻を取得してログに付与
        string timeStr = System.DateTime.Now.ToString("HH:mm");
        if (txt != null)
        {
            txt.text = $"<color=#888888>[{timeStr}]</color> {message}";
        }

        // 最新のログが見えるようにスクロール位置を調整（必要であれば）
        Canvas.ForceUpdateCanvases();
        ScrollRect scrollRect = logContent.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}