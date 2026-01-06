using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text;
using System.Collections.Generic;

public class MenuManager : MonoBehaviour
{
    public GameManager gameManager;
    public ItemManager itemManager;

    [Header("UI References")]
    public TextMeshProUGUI smartphoneText; // スマホ画面の簡易表示用
    public GameObject detailPanel;         // 全画面詳細パネル
    public TextMeshProUGUI detailTitle;
    public TextMeshProUGUI detailContent;
    public Transform itemButtonContainer;  // アイテムボタンを並べる親オブジェクト
    public GameObject itemButtonPrefab;    // アイテムボタンのプレハブ

    // --- 各メニューボタンの処理 ---

    public void OnInfoBtn()
    {
        // 男子から聞いたヒント一覧
        StringBuilder sb = new StringBuilder("【秘密の噂】\n\n");
        foreach (var f in gameManager.allFriends)
        {
            if (f.isHintRevealed)
            {
                sb.AppendLine($"■ {f.friendName}");
                sb.AppendLine($"  {f.GetHintText()}");
                sb.AppendLine("");
            }
        }
        smartphoneText.text = "秘密の噂を表示中...";
        ShowDetail("秘密の情報", sb.ToString());
    }

    public void OnMaleFriendBtn()
    {
        var list = gameManager.playerStats.maleFriendsList;
        StringBuilder sb = new StringBuilder($"【男友達リスト】(計{list.Count}人)\n\n");

        foreach (var m in list)
        {
            string status = m.isBoyfriend ? "Is Boyfriend" : "Friend";
            sb.AppendLine($"{m.name}: 好感度 {m.currentAffection:F0}% [{status}]");
        }

        smartphoneText.text = "男友達リストを表示中...";
        ShowDetail("男友達・彼氏", sb.ToString());
    }

    public void OnBoyfriendBtn()
    {
        var list = gameManager.playerStats.maleFriendsList;
        StringBuilder sb = new StringBuilder("【彼氏ボーナス】\n\n");
        int bfCount = 0;
        foreach (var m in list)
        {
            if (m.isBoyfriend)
            {
                sb.AppendLine($"■ {m.name}");
                sb.AppendLine($"  効果: {m.effectType}");
                sb.AppendLine("");
                bfCount++;
            }
        }
        if (bfCount == 0) sb.Append("彼氏はまだいません。");

        smartphoneText.text = "彼氏情報を表示中...";
        ShowDetail("彼氏情報", sb.ToString());
    }

    public void OnShinyuBtn()
    {
        StringBuilder sb = new StringBuilder("【親友一覧】\n\n");
        foreach (var f in gameManager.allFriends)
        {
            if (f.isRecruited)
            {
                sb.AppendLine($"★ {f.friendName}");
                sb.AppendLine($"  能力: {f.effectType}");
            }
            else
            {
                sb.AppendLine($"・{f.friendName} (未加入)");
            }
        }
        smartphoneText.text = "親友リストを表示中...";
        ShowDetail("親友図鑑", sb.ToString());
    }

    // アイテムボタンは特別：ボタンを生成して並べる
    public void OnItemBtn()
    {
        smartphoneText.text = "アイテムを選択してください";
        ShowDetail("所持アイテム", ""); // 内容はクリアしてボタンを並べる
        RefreshItemDisplay();
    }

    // アイテムリストの再描画
    public void RefreshItemDisplay()
    {
        // 既存のボタンを削除
        foreach (Transform child in itemButtonContainer) Destroy(child.gameObject);

        // 1. 移動カードの表示
        for (int i = 0; i < itemManager.movementCards.Count; i++)
        {
            int cardVal = itemManager.movementCards[i];
            GameObject btn = Instantiate(itemButtonPrefab, itemButtonContainer);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = $"移動カード [{cardVal}]";

            // 移動カードはダイス代わりに使用（GameManagerへ連携が必要だが、今回は詳細省略）
            // Buttonコンポーネントに機能を持たせるならここ
        }

        // 2. 所持アイテムの表示
        foreach (var item in itemManager.inventory)
        {
            GameObject btn = Instantiate(itemButtonPrefab, itemButtonContainer);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = item.itemName;

            // ボタンを押したらアイテム使用
            btn.GetComponent<Button>().onClick.AddListener(() => {
                itemManager.UseItem(item);
            });
        }
    }

    void ShowDetail(string title, string content)
    {
        detailPanel.SetActive(true);
        detailTitle.text = title;
        detailContent.text = content;

        // アイテムコンテナはアイテム画面のときだけ有効化などの制御が必要
        bool isItemMode = (title == "所持アイテム");
        itemButtonContainer.gameObject.SetActive(isItemMode);
        detailContent.gameObject.SetActive(!isItemMode);
    }

    public void CloseDetail()
    {
        detailPanel.SetActive(false);
    }
}