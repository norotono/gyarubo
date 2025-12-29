using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("--- HUD ---")]
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI assetText;
    public TextMeshProUGUI statusText;

    [Header("--- Phone ---")]
    public PhoneUIManager phoneUI; // 必須: Inspectorでアタッチ

    public void UpdateDisplay(PlayerStats s)
    {
        if (dateText) dateText.text = $"{s.currentGrade}年目 {s.currentMonth}月";
        if (assetText) assetText.text = $"友: {s.friends}人\nGP: {s.gp:N0}";
        if (statusText) statusText.text = $"コミュ: {s.commuLv}\nギャル: {s.galLv}\nレモン: {s.lemonLv}";
    }

    public void Log(string msg)
    {
        Debug.Log(msg);
        // スマホのログ機能へ送る
        if (phoneUI)
        {
            phoneUI.AddLog(msg);
        }
        else
        {
            Debug.LogWarning("PhoneUIManagerがUIManagerにアタッチされていません！");
        }
    }
}