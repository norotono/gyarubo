using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartSceneManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject rulePanel; // ルール表示用パネル

    [Header("Rule Pages")]
    public GameObject[] rulePages; // ルールの各ページ(ImageやTextをまとめたGameObject)の配列
    public Button nextButton;
    public Button backButton;
    public Button closeButton;

    private int currentPageIndex = 0;

    private void Start()
    {
        if (rulePanel) rulePanel.SetActive(false);
    }

    // --- PLAYボタン ---
    public void OnPlayButtonClicked()
    {
        SceneManager.LoadScene("GameScene");
    }

    // --- RULEボタン ---
    public void OnRuleButtonClicked()
    {
        if (rulePanel)
        {
            rulePanel.SetActive(true);
            currentPageIndex = 0;
            ShowPage(currentPageIndex);
        }
    }

    // --- ルール操作 ---
    public void OnNextPage()
    {
        if (currentPageIndex < rulePages.Length - 1)
        {
            currentPageIndex++;
            ShowPage(currentPageIndex);
        }
    }

    public void OnBackPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            ShowPage(currentPageIndex);
        }
    }

    public void OnCloseRule()
    {
        if (rulePanel) rulePanel.SetActive(false);
    }

    private void ShowPage(int index)
    {
        // 全ページ非表示 -> 対象だけ表示
        for (int i = 0; i < rulePages.Length; i++)
        {
            if (rulePages[i]) rulePages[i].SetActive(i == index);
        }

        // ボタンの有効無効切り替え
        if (nextButton) nextButton.interactable = (index < rulePages.Length - 1);
        if (backButton) backButton.interactable = (index > 0);
    }
}