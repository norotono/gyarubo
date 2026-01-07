using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;

public class MenuManager : MonoBehaviour
{
    public GameManager gameManager;
    public ItemManager itemManager;

    [Header("UI Containers")]
    public Transform listContent; // ScrollViewのContent (ここにボタンを並べる)
    public GameObject listButtonPrefab; // プレハブ (Button + TextMeshProUGUI)

    [Header("Smartphone Screen")]
    public TextMeshProUGUI headerText; // スマホ画面上部のタイトル表示用

    [Header("Detail Panel")]
    public GameObject detailPanel;      // 詳細パネル全体
    public TextMeshProUGUI detailTitle; // 詳細タイトル
    public TextMeshProUGUI detailDesc;  // 詳細本文
    public Button actionButton;         // 「使う」などのアクションボタン
    public TextMeshProUGUI actionBtnText; // ボタンの文字

    // ★追加: 全画面オーバーレイ用UI (Unityエディタで設定が必要)
    [Header("--- Full Screen Overlay UI ---")]
    public GameObject fullScreenPanel;        // 画面全体を覆うパネル
    public TextMeshProUGUI fullScreenTitle;   // タイトル
    public TextMeshProUGUI fullScreenDesc;    // 説明文(数字など)
    public Transform fullScreenButtonRoot;    // ボタンの親
    public GameObject fullScreenButtonPrefab; // 大きめのボタンPrefab

    // --- 共通：リストクリア処理 ---
    void ClearList()
    {
        foreach (Transform child in listContent)
        {
            Destroy(child.gameObject);
        }
        detailPanel.SetActive(false); // 詳細パネルも閉じておく
    }

    // --- 1. 情報ボタン (男子から聞いた話) ---
    public void OnInfoBtn()
    {
        ClearList();
        headerText.text = "【秘密の情報】";

        foreach (var f in gameManager.allFriends)
        {
            // ヒントが公開されているキャラのみ表示
            if (f.isHintRevealed)
            {
                CreateListButton(f.friendName, () =>
                {
                    ShowDetail(f.friendName, $"【出現条件ヒント】\n\n{f.GetHintText()}", null);
                });
            }
        }
    }

    // --- 2. 親友ボタン ---
    public void OnShinyuBtn()
    {
        ClearList();
        headerText.text = "【親友リスト】";

        foreach (var f in gameManager.allFriends)
        {
            if (f.isRecruited)
            {
                CreateListButton(f.friendName, () =>
                {
                    ShowDetail(f.friendName, $"【親友効果】\n\nタイプ: {f.effectType}\n{GetEffectDescription(f.effectType)}", null);
                });
            }
        }
    }

    // --- 3. 男友達ボタン ---
    public void OnMaleFriendBtn()
    {
        ClearList();
        headerText.text = "【男友達】";

        foreach (var m in gameManager.playerStats.maleFriendsList)
        {
            if (!m.isBoyfriend)
            {
                string label = $"{m.name} (♡{m.currentAffection:F0})";
                CreateListButton(label, () =>
                {
                    ShowDetail(m.name, $"現在の親密度: {m.currentAffection:F0}\n\n仲良くなれば彼氏になるかも？", null);
                });
            }
        }
    }

    // --- 4. 彼氏ボタン ---
    public void OnBoyfriendBtn()
    {
        ClearList();
        headerText.text = "【彼氏】";

        foreach (var m in gameManager.playerStats.maleFriendsList)
        {
            if (m.isBoyfriend)
            {
                CreateListButton(m.name, () =>
                {
                    ShowDetail(m.name, $"【彼氏ボーナス】\nタイプ: {m.effectType}\n毎ターン終了時にボーナスをくれます。", null);
                });
            }
        }
    }

    // --- 5. アイテムボタン ---
    public void OnItemBtn()
    {
        RefreshItemList();
    }

    // 公開メソッドにしてItemManagerからも呼べるようにする
    public void RefreshItemList()
    {
        ClearList();
        headerText.text = "【所持アイテム】";

        // A. 移動カードの表示 (1~6)
        var cardCounts = itemManager.GetCardCounts();
        foreach (var kvp in cardCounts)
        {
            int num = kvp.Key;   // カードの数字
            int count = kvp.Value; // 枚数

            if (count > 0)
            {
                string label = $"移動カード [{num}]  x{count}";
                CreateListButton(label, () =>
                {
                    // カードの詳細を表示
                    ShowDetail(
                        $"移動カード [{num}]",
                        "使用するとサイコロを振らずに、この数字の分だけ進めます。",
                        () => itemManager.UseMovementCard(num), // アクション: 使用
                        "使う"
                    );
                });
            }
        }

        // B. 通常アイテムの表示
        var itemCounts = itemManager.GetItemCounts();
        foreach (var kvp in itemCounts)
        {
            string iName = kvp.Key;
            int count = kvp.Value;

            string label = $"{iName}  x{count}";
            CreateListButton(label, () =>
            {
                // 生徒手帳の場合
                if (iName == "生徒手帳")
                {
                    ShowDetail(
                        iName,
                        "【効果】\n教室マスに止まった際、行動を選択できるようになります。\n(※ここでは使用できません。教室マスで自動的に選択肢が出ます)",
                        null // アクションなし
                    );
                }
                // 強制イベントなどその他の場合
                else
                {
                    ShowDetail(
                        iName,
                        "【効果】\nこのアイテムを使用しますか？",
                        () => itemManager.UseItemByName(iName), // アクション: 使用
                        "使う"
                    );
                }
            });
        }
    }

    // --- ヘルパーメソッド ---

    // リストのボタンを生成する
    void CreateListButton(string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = Instantiate(listButtonPrefab, listContent);
        btnObj.GetComponentInChildren<TextMeshProUGUI>().text = label;
        btnObj.GetComponent<Button>().onClick.AddListener(onClick);
    }

    // 詳細パネルを表示する
    // action: ボタンを押した時の処理 (nullならボタン非表示)
    // btnLabel: ボタンのテキスト (デフォルトは"使う")
    void ShowDetail(string title, string content, UnityEngine.Events.UnityAction action, string btnLabel = "使う")
    {
        detailPanel.SetActive(true);
        detailTitle.text = title;
        detailDesc.text = content;

        if (action != null)
        {
            actionButton.gameObject.SetActive(true);
            actionBtnText.text = btnLabel;
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(action);
        }
        else
        {
            // アクションがない場合（見るだけの時や生徒手帳）はボタンを隠す
            actionButton.gameObject.SetActive(false);
        }
    }
    public void OpenCardDiscardMenu(List<int> currentCards, int newCard, UnityEngine.Events.UnityAction<int> onDecision)
    {
        ClearList(); // 既存のリストをクリア

        // ヘッダーと詳細パネルを使って状況を説明
        headerText.text = "【カードがいっぱいです】";

        detailPanel.SetActive(true);
        detailTitle.text = "どれを捨てますか？";
        detailDesc.text = $"手持ちが上限(5枚)です。\n新しく出たカード: <size=150%>{newCard}</size>\n\n捨てるカードを選んでください。\n(※選んだカードが消滅し、新しいカードが入ります)";

        // アクションボタン（「使う」など）は今回は邪魔なので消す
        if (actionButton != null) actionButton.gameObject.SetActive(false);

        // 1. 既存の手持ちカードを「捨てる」ボタンとして生成
        for (int i = 0; i < currentCards.Count; i++)
        {
            int cardVal = currentCards[i];
            int index = i; // クロージャキャプチャ用（重要）

            CreateListButton($"手持ち: [{cardVal}] を捨てる", () => {
                // 決定処理
                CloseDetail();     // メニュー詳細を閉じる
                ClearList();       // リストも消す
                headerText.text = ""; // ヘッダー戻す（必要に応じて）
                onDecision(index); // 選んだインデックスを返す
            });
        }

        // 2. 新しく引いたカードを「諦める」ボタン
        CreateListButton($"新規: [{newCard}] を諦める", () => {
            CloseDetail();
            ClearList();
            headerText.text = "";
            onDecision(currentCards.Count); // リスト外のインデックス＝新規破棄扱い
        });

        // ★レイアウト更新の強制（ボタンが表示されない対策の一つとして）
        LayoutRebuilder.ForceRebuildLayoutImmediate(listContent.GetComponent<RectTransform>());
    }

    public void CloseDetail()
    {
        detailPanel.SetActive(false);
    }
    // --- ★追加: カード入手時の確認 (Confirmation) ---
    public void ShowCardConfirmation(int cardValue, UnityEngine.Events.UnityAction onOk)
    {
        fullScreenPanel.SetActive(true);

        // 既存ボタン削除
        foreach (Transform child in fullScreenButtonRoot) Destroy(child.gameObject);

        fullScreenTitle.text = "移動カード購入";
        fullScreenDesc.text = $"手に入れた数字は\n<size=200%><color=red>{cardValue}</color></size>\nです！";

        // OKボタン生成
        GameObject btn = Instantiate(fullScreenButtonPrefab, fullScreenButtonRoot);
        btn.GetComponentInChildren<TextMeshProUGUI>().text = "OK";
        btn.GetComponent<Button>().onClick.AddListener(() =>
        {
            fullScreenPanel.SetActive(false); // パネル閉じる
            onOk.Invoke(); // 次の処理へ
        });
    }

    // --- ★追加: カード取捨選択メニュー ---
    public void OpenCardDiscardMenu(List<int> currentCards, int newCard, UnityEngine.Events.UnityAction<int> onDecision)
    {
        fullScreenPanel.SetActive(true);

        foreach (Transform child in fullScreenButtonRoot) Destroy(child.gameObject);

        fullScreenTitle.text = "カード整理";
        fullScreenDesc.text = "手持ちがいっぱいです(5枚)。\n捨てるカードを1枚選んでください。\n(残すカードではありません)";

        // 1. 手持ちカード (0~4)
        for (int i = 0; i < currentCards.Count; i++)
        {
            int cardVal = currentCards[i];
            int index = i;

            GameObject btn = Instantiate(fullScreenButtonPrefab, fullScreenButtonRoot);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = $"手持ち[{cardVal}] を捨てる";
            btn.GetComponent<Button>().onClick.AddListener(() => {
                fullScreenPanel.SetActive(false);
                onDecision(index);
            });
        }

        // 2. 新規カード (5)
        int newIndex = currentCards.Count;
        GameObject newBtn = Instantiate(fullScreenButtonPrefab, fullScreenButtonRoot);
        newBtn.GetComponentInChildren<TextMeshProUGUI>().text = $"新規[{newCard}] を諦める";
        newBtn.GetComponentInChildren<TextMeshProUGUI>().color = Color.red;
        newBtn.GetComponent<Button>().onClick.AddListener(() => {
            fullScreenPanel.SetActive(false);
            onDecision(newIndex);
        });

        // レイアウト更新
        LayoutRebuilder.ForceRebuildLayoutImmediate(fullScreenPanel.GetComponent<RectTransform>());
    }
}

// 親友効果の説明文用（簡易）
string GetEffectDescription(FriendEffectType type)
    {
        switch (type)
        {
            case FriendEffectType.DiceReroll: return "1ターンに1度、ダイスを振り直せます。";
            case FriendEffectType.CardGeneration: return "定期的に移動カードをくれます。";
            case FriendEffectType.AutoFriend: return "毎ターン、友達を1人連れてきます。";
            default: return "特別な効果はありません。";
        }
    }
}