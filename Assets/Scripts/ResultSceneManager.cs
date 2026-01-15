using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ResultSceneManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI scoreText;

    private void Start()
    {
        // ★修正: 静的変数から結果を取得
        int friendCount = PlayerStats.FinalFriendsCount;

        // GPは持ち越していないので表示しないか、別途静的変数を用意する必要があります
        // ここでは友達数のみ表示します
        scoreText.text = $"結果発表！\n\n友達: <size=150%>{friendCount}人</size>";
    }

    public void OnRetryButtonClicked()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void OnTitleButtonClicked()
    {
        SceneManager.LoadScene("Start");
    }
}