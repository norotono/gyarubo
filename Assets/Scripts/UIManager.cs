using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI assetText;
    public TextMeshProUGUI statusText;
    public PhoneUIManager phoneUI;

    public void UpdateDisplay(PlayerStats s)
    {
        if (dateText) dateText.text = $"{s.currentGrade}年目 {s.currentMonth}月";
        if (assetText) assetText.text = $"友: {s.friends}人\nGP: {s.gp:N0}";
        if (statusText) statusText.text = $"コミュ: {s.commuLv}\nギャル: {s.galLv}\nレモン: {s.lemonLv}";
    }

    public void Log(string msg)
    {
        Debug.Log(msg);
        if (phoneUI) phoneUI.AddLog(msg);
    }
}