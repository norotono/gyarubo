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
    public Transform listContent; 
    public GameObject listButtonPrefab; 

    [Header("Smartphone Screen")]
    public TextMeshProUGUI headerText; 

    [Header("Detail Panel")]
    public GameObject detailPanel;      
    public TextMeshProUGUI detailTitle; 
    public TextMeshProUGUI detailDesc;  
    public Button actionButton;         
    public TextMeshProUGUI actionBtnText; 

    [Header("--- Full Screen Overlay UI ---")]
    public GameObject fullScreenPanel;        
    public TextMeshProUGUI fullScreenTitle;   
    public TextMeshProUGUI fullScreenDesc;    
    public Transform fullScreenButtonRoot;    
    public GameObject fullScreenButtonPrefab; 

    // --- 教室イベント用パネル表示（追加） ---
    public void ShowClassroomPanel(bool hasHandbook, UnityEngine.Events.UnityAction onInvestigate, UnityEngine.Events.UnityAction onCancel)
    {
        fullScreenPanel.SetActive(true);
        foreach (Transform child in fullScreenButtonRoot) Destroy(child.gameObject);

        fullScreenTitle.text = "教室イベント";
        
        if (!hasHandbook)
        {
            fullScreenDesc.text = "生徒手帳がないよ！\n中を調べることはできません。";
            
            // 戻るボタンのみ
            GameObject btn = Instantiate(fullScreenButtonPrefab, fullScreenButtonRoot);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = "戻る";
            btn.GetComponent<Button>().onClick.AddListener(() => {
                fullScreenPanel.SetActive(false);
                onCancel.Invoke();
            });
        }
        else
        {
            fullScreenDesc.text = "生徒手帳を持っています。\n教室の中を調べますか？";

            // 調べるボタン
            GameObject btn1 = Instantiate(fullScreenButtonPrefab, fullScreenButtonRoot);
            btn1.GetComponentInChildren<TextMeshProUGUI>().text = "調べる";
            btn1.GetComponent<Button>().onClick.AddListener(() => {
                fullScreenPanel.SetActive(false);
                onInvestigate.Invoke();
            });

            // やめるボタン
            GameObject btn2 = Instantiate(fullScreenButtonPrefab, fullScreenButtonRoot);
            btn2.GetComponentInChildren<TextMeshProUGUI>().text = "やめる";
            btn2.GetComponent<Button>().onClick.AddListener(() => {
                fullScreenPanel.SetActive(false);
                onCancel.Invoke();
            });
        }
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(fullScreenPanel.GetComponent<RectTransform>());
    }

    // --- ★追加: サイコロ結果のカットイン表示 ---
    public void ShowDiceResult(int result, UnityEngine.Events.UnityAction onConfirm)
    {
        fullScreenPanel.SetActive(true);
        foreach (Transform child in fullScreenButtonRoot) Destroy(child.gameObject);

        fullScreenTitle.text = "ダイス結果";
        // 大きく数字を表示
        fullScreenDesc.text = $"<size=200%>{result}</size>\nが出ました！";

        // OKボタン
        GameObject btn = Instantiate(fullScreenButtonPrefab, fullScreenButtonRoot);
        btn.GetComponentInChildren<TextMeshProUGUI>().text = "進む";
        btn.GetComponent<Button>().onClick.AddListener(() =>
        {
            fullScreenPanel.SetActive(false);
            onConfirm.Invoke();
        });

        LayoutRebuilder.ForceRebuildLayoutImmediate(fullScreenPanel.GetComponent<RectTransform>());
    }

    // --- ★追加: アイテムメニューの構築 (MenuManager管轄) ---
    public void ShowItemList()
    {
        ClearList(); // 既存リストをクリア
        headerText.text = "【アイテム一覧】";

        // 1. 移動カード
        var cardCounts = itemManager.GetCardCounts();
        foreach (var kvp in cardCounts)
        {
            int num = kvp.Key;
            int count = kvp.Value;
            if (count > 0)
            {
                CreateListButton($"移動カード [{num}]  x{count}", () =>
                {
                    ShowDetail(
                        $"移動カード [{num}]",
                        "使用するとサイコロを振らずに、この数字の分だけ進めます。",
                        () => itemManager.UseMovementCard(num),
                        "使う"
                    );
                });
            }
        }

        // 2. 生徒手帳 (ItemManager経由で取得)
        int hbCount = itemManager.GetHandbookCount();
        string hbLabel = (hbCount > 0) ? $"生徒手帳 (所持: {hbCount})" : "生徒手帳 (未所持)";
        CreateListButton(hbLabel, () =>
        {
            string desc = (hbCount > 0)
                ? "【効果】\n教室マスで使うと、中の様子を詳しく調べられます。\n(使うと1冊消費します)"
                : "【未所持】\nこれがないと教室の中を詳しく調べられません。\n購買部で購入しましょう。";
            // ボタンアクションは無し（詳細表示のみ）
            ShowDetail("生徒手帳", desc, null);
        });

        // 3. その他アイテム
        var invCounts = itemManager.GetItemCounts();
        foreach (var kvp in invCounts)
        {
            string iName = kvp.Key;
            int count = kvp.Value;
            CreateListButton($"{iName}  x{count}", () =>
            {
                ShowDetail(iName, "このアイテムを使用しますか？", () => itemManager.UseItemByName(iName), "使う");
            });
        }

        // 4. プレゼント
        if (gameManager.playerStats.present > 0)
        {
            CreateListButton($"プレゼント x{gameManager.playerStats.present}", () =>
            {
                ShowDetail("プレゼント", "特定のイベントで渡すと親密度が上がります。", null);
            });
        }
    }

    // --- 以下、既存のメソッド ---
    void ClearList()
    {
        foreach (Transform child in listContent) Destroy(child.gameObject);
        detailPanel.SetActive(false); 
    }

    public void OnInfoBtn()
    {
        ClearList();
        headerText.text = "【秘密の情報】";
        foreach (var f in gameManager.allFriends)
        {
            if (f.isHintRevealed)
            {
                CreateListButton(f.friendName, () =>
                {
                    ShowDetail(f.friendName, $"【出現条件ヒント】\n\n{f.GetHintText()}", null);
                });
            }
        }
    }

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

    public void OnBoyfriendBtn()
    {
        ClearList();
        headerText.text = "【彼氏】";
        foreach (var m in gameManager.playerStats.boyfriendList) 
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
        // ItemManager側の playerStats.moveCards を集計して表示する形に変更推奨ですが
        // ここではItemManagerのGetCardCountsが正しく実装されている前提とします
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

    void CreateListButton(string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = Instantiate(listButtonPrefab, listContent);
        btnObj.GetComponentInChildren<TextMeshProUGUI>().text = label;
        btnObj.GetComponent<Button>().onClick.AddListener(onClick);
    }

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
            actionButton.gameObject.SetActive(false);
        }
    }

    public void CloseDetail()
    {
        detailPanel.SetActive(false);
    }

    public void ShowCardConfirmation(int cardValue, UnityEngine.Events.UnityAction onOk)
    {
        fullScreenPanel.SetActive(true);
        foreach (Transform child in fullScreenButtonRoot) Destroy(child.gameObject);

        fullScreenTitle.text = "移動カード購入";
        fullScreenDesc.text = $"手に入れた数字は\n<size=200%><color=red>{cardValue}</color></size>\nです！";

        GameObject btn = Instantiate(fullScreenButtonPrefab, fullScreenButtonRoot);
        btn.GetComponentInChildren<TextMeshProUGUI>().text = "OK";
        btn.GetComponent<Button>().onClick.AddListener(() =>
        {
            fullScreenPanel.SetActive(false); 
            onOk.Invoke(); 
        });
    }

    public void OpenCardDiscardMenu(List<int> currentCards, int newCard, UnityEngine.Events.UnityAction<int> onDecision)
    {
        fullScreenPanel.SetActive(true);
        foreach (Transform child in fullScreenButtonRoot) Destroy(child.gameObject);

        fullScreenTitle.text = "カード整理";
        fullScreenDesc.text = "手持ちがいっぱいです(5枚)。\n捨てるカードを1枚選んでください。\n(残すカードではありません)";

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

        int newIndex = currentCards.Count;
        GameObject newBtn = Instantiate(fullScreenButtonPrefab, fullScreenButtonRoot);
        newBtn.GetComponentInChildren<TextMeshProUGUI>().text = $"新規[{newCard}] を諦める";
        newBtn.GetComponentInChildren<TextMeshProUGUI>().color = Color.red;
        newBtn.GetComponent<Button>().onClick.AddListener(() => {
            fullScreenPanel.SetActive(false);
            onDecision(newIndex);
        });

        LayoutRebuilder.ForceRebuildLayoutImmediate(fullScreenPanel.GetComponent<RectTransform>());
    }

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