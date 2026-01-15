using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ResultSceneManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI scoreText; // 結果表示用テキスト

    private void Start()
    {
        // PlayerStatsが存在するかチェック
        if (PlayerStats.Instance != null)
        {
            int friendCount = PlayerStats.Instance.friends;
            int finalGP = PlayerStats.Instance.gp;

            scoreText.text = $"結果発表！\n\n友達: <size=150%>{friendCount}人</size>\n所持金: {finalGP} G";
        }
        else
        {
            // ここが表示される場合、PlayerStatsがシーン遷移で消えています
            scoreText.text = "データ読み込みエラー\n(PlayerStatsが見つかりません)";
            Debug.LogError("PlayerStats Instance is null in Result Scene!");
        }
    }

    public void OnRetryButtonClicked()
    {
        if (PlayerStats.Instance != null) PlayerStats.Instance.ResetData();
        SceneManager.LoadScene("GameScene");
    }

    public void OnTitleButtonClicked()
    {
        if (PlayerStats.Instance != null) PlayerStats.Instance.ResetData();

        // ★修正: シーン名を "StartScene" から "Start" に変更
        SceneManager.LoadScene("Start");
    }
}