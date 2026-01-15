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
    // fullScreenPanelの中に新しくImageを作ってアタッチするか、diceResultImageを流用してもOKです
    public Image friendFaceImage;
    public Image detailImage;
    // ★追加: ダイス画像を表示するためのImageコンポーネント
    public Image diceResultImage;

    // --- PhoneUIManager取得用のヘルパープロパティ ---
    private PhoneUIManager _phoneUI;
    private PhoneUIManager phoneUI
    {
        get
        {
            if (_phoneUI == null)
                _phoneUI = FindFirstObjectByType<PhoneUIManager>();
            return _phoneUI;
        }
    }

    // --- ★修正: 教室イベント用パネル表示 ---
    public void ShowFriendRecruited(FriendData friend, UnityEngine.Events.UnityAction onConfirm)
    {
        // 1. パネルを表示
        detailPanel.SetActive(true);

        // 2. タイトルと説明文の設定
        detailTitle.text = "親友成立！";

        string effectDesc = GetEffectDescription(friend.effectType);
        detailDesc.text = $"<b>{friend.friendName}</b> が親友になった！\n\n【能力】\n{effectDesc}";

        // 3. 顔アイコンの設定
        if (detailImage != null)
        {
            if (friend.faceIcon != null)
            {
                detailImage.gameObject.SetActive(true);
                detailImage.sprite = friend.faceIcon;
            }
            else
            {
                detailImage.gameObject.SetActive(false);
            }
        }

        // 4. OKボタンの設定
        actionButton.gameObject.SetActive(true);
        actionBtnText.text = "OK"; // ボタンのテキスト

        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(() =>
        {
            // 閉じる処理
            detailPanel.SetActive(false);
            if (detailImage != null) detailImage.gameObject.SetActive(false);
            onConfirm.Invoke();
        });
    }

    // --- ★修正: 教室パネル (ボタンのクリアと生成を確実に) ---
    public void ShowClassroomPanel(bool hasHandbook, UnityEngine.Events.UnityAction onInvestigate, UnityEngine.Events.UnityAction onCancel)
    {
        fullScreenPanel.SetActive(true);
        foreach (Transform child in fullScreenButtonRoot) Destroy(child.gameObject);

        fullScreenTitle.text = "教室イベント";

        if (hasHandbook)
        {
            // --- ある場合 ---
            fullScreenDesc.text = "生徒手帳を持っています。\n1冊消費して、中の様子を調査しますか？";

            // 調査ボタン
            GameObject btn1 = Instantiate(fullScreenButtonPrefab, fullScreenButtonRoot);
            btn1.GetComponentInChildren<TextMeshProUGUI>().text = "調査する";
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
        else
        {
            // --- ない場合 ---
            fullScreenDesc.text = "生徒手帳がありません。\n（購買部で購入すると詳しく調べられます）";

            // 閉じるボタンのみ
            GameObject btnClose = Instantiate(fullScreenButtonPrefab, fullScreenButtonRoot);
            btnClose.GetComponentInChildren<TextMeshProUGUI>().text = "閉じる";
            btnClose.GetComponent<Button>().onClick.AddListener(() => {
                fullScreenPanel.SetActive(false);
                onCancel.Invoke();
            });
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(fullScreenPanel.GetComponent<RectTransform>());
    }

    // --- サイコロ結果表示 (修正版) ---
    public void ShowDiceResult(int result, Sprite diceSprite, UnityEngine.Events.UnityAction onConfirm)
    {
        // 1. パネル表示
        fullScreenPanel.SetActive(true);
        foreach (Transform child in fullScreenButtonRoot) Destroy(child.gameObject);

        fullScreenTitle.text = "ダイス結果";

        // 2. 画像の設定 (★ここを追加)
        if (diceResultImage != null)
        {
            if (diceSprite != null)
            {
                diceResultImage.gameObject.SetActive(true);
                diceResultImage.sprite = diceSprite;
                // テキストはシンプルに
                fullScreenDesc.text = "";
            }
            else
            {
                // 画像がない場合は非表示にしてテキストで大きく出す
                diceResultImage.gameObject.SetActive(false);
                fullScreenDesc.text = $"<size=300%>{result}</size>";
            }
        }
        else
        {
            // Image設定がない場合
            fullScreenDesc.text = $"<size=300%>{result}</size>";
        }

        // 3. 進むボタン
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
        RefreshItemList();
    }

    // 公開メソッドにしてItemManagerからも呼べるようにする
    // ★修正: アイテムリストの表示 (ItemManagerから情報を取得して構築)
    public void RefreshItemList()
    {
        ClearList(); // 既存のリストをクリア
        headerText.text = "【所持アイテム】";

        // ★修正: phoneUI変数が無いエラーを回避
        if (phoneUI) phoneUI.SetDiceVisibility(false);

        // 1. 移動カードの表示
        // (ItemManagerにGetCardCountsがある前提)
        var cardCounts = itemManager.GetCardCounts();
        foreach (var kvp in cardCounts)
        {
            int num = kvp.Key;
            int count = kvp.Value;
            if (count > 0)
            {
                string label = $"移動カード [{num}]  x{count}";
                CreateListButton(label, () =>
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
        // 2. 生徒手帳 (所持数0でも表示)
        int hbCount = itemManager.GetHandbookCount();
        string hbLabel = (hbCount > 0) ? $"生徒手帳 (所持: {hbCount})" : "生徒手帳 (未所持)";

        CreateListButton(hbLabel, () =>
        {
            string desc = (hbCount > 0)
                ? "【効果】\n教室マスで使用すると、中の様子を詳しく調べられます。\n(使うと1冊消費します)"
                : "【未所持】\nこれがないと教室の中を詳しく調べられません。\n購買部で購入しましょう。";
            ShowDetail("生徒手帳", desc, null);
        });

        // 3. その他アイテム (ItemManagerのInventoryリスト)
        var itemCounts = itemManager.GetItemCounts();
        foreach (var kvp in itemCounts)
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

        // 5. 閉じるボタン
        CreateListButton("閉じる", () =>
        {
            ClearList(); // ボタンを消す

            // ★修正: phoneUIプロパティを使用
            if (phoneUI)
            {
                phoneUI.ShowDiceMode(); // 元に戻す
            }
        });
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

        // ★修正
        if (phoneUI) phoneUI.SetDiceVisibility(false);

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
        CreateListButton("閉じる", () =>
        {
            ClearList(); // ボタンを消す

            // ★修正
            if (phoneUI)
            {
                phoneUI.ShowDiceMode(); // 元に戻す
            }
        });
    }

    public void OnShinyuBtn()
    {
        ClearList();
        headerText.text = "【親友リスト】";

        // ★修正
        if (phoneUI) phoneUI.SetDiceVisibility(false);

        foreach (var f in gameManager.allFriends)
        {
            if (f.isRecruited)
            {
                CreateListButton(f.friendName, () =>
                {
                    // 親友画像を渡す
                    ShowDetail(
                        f.friendName,
                        $"【親友効果】\n\nタイプ: {f.effectType}\n{GetEffectDescription(f.effectType)}",
                        null,
                        "閉じる",
                        f.faceIcon
                    );
                });
            }
        }
        CreateListButton("閉じる", () =>
        {
            ClearList(); // ボタンを消す

            // ★修正
            if (phoneUI)
            {
                phoneUI.ShowDiceMode(); // 元に戻す
            }
        });
    }

    public void OnMaleFriendBtn()
    {
        ClearList();
        headerText.text = "【男友達】";

        // ★修正
        if (phoneUI) phoneUI.SetDiceVisibility(false);

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
        CreateListButton("閉じる", () =>
        {
            ClearList(); // ボタンを消す

            // ★修正
            if (phoneUI)
            {
                phoneUI.ShowDiceMode(); // 元に戻す
            }
        });
    }

    public void OnBoyfriendBtn()
    {
        ClearList();
        headerText.text = "【彼氏】";

        // ★修正
        if (phoneUI) phoneUI.SetDiceVisibility(false);

        foreach (var m in gameManager.playerStats.boyfriendList)
        {
            if (m.isBoyfriend)
            {
                CreateListButton(m.name, () =>
                {
                    ShowDetail(m.name, $"【彼氏ボーナス】\nタイプ: {m.effectType}\n毎ターン終了時にType_AならGP,Type_Bなら友達のボーナスをくれます。", null);
                });
            }
        }
        CreateListButton("閉じる", () =>
        {
            ClearList(); // ボタンを消す

            // ★修正
            if (phoneUI)
            {
                phoneUI.ShowDiceMode(); // 元に戻す
            }
        });
    }

    public void OnItemBtn()
    {
        RefreshItemList();
    }

    void CreateListButton(string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = Instantiate(listButtonPrefab, listContent);
        btnObj.GetComponentInChildren<TextMeshProUGUI>().text = label;
        btnObj.GetComponent<Button>().onClick.AddListener(onClick);
    }

    // ★修正: 画像(icon)を受け取れるように引数を追加
    public void ShowDetail(string title, string content, UnityEngine.Events.UnityAction action, string btnLabel = "使う", Sprite icon = null)
    {
        // パネルを表示
        if (detailPanel) detailPanel.SetActive(true);
        if (detailTitle) detailTitle.text = title;
        if (detailDesc) detailDesc.text = content;

        // ★追加: 画像の表示設定 (親友確認などで使用)
        if (detailImage != null)
        {
            if (icon != null)
            {
                detailImage.gameObject.SetActive(true);
                detailImage.sprite = icon;
                // 必要なら縦横比維持の設定を入れる
                // detailImage.preserveAspect = true; 
            }
            else
            {
                // 画像がない場合は非表示
                detailImage.gameObject.SetActive(false);
            }
        }

        // ボタンの設定
        if (actionButton)
        {
            actionButton.gameObject.SetActive(true);

            // アクションがある場合はそのラベル、ない場合は「閉じる」
            if (actionBtnText) actionBtnText.text = (action != null) ? btnLabel : "閉じる";

            actionButton.onClick.RemoveAllListeners();
            if (action != null)
            {
                actionButton.onClick.AddListener(() => {
                    action.Invoke();
                    // アクション実行後に閉じるかどうかはケースバイケースですが、基本は閉じる
                    // CloseDetail(); 
                });
            }
            else
            {
                // アクションがない＝確認だけなので、閉じるボタンとして機能
                actionButton.onClick.AddListener(CloseDetail);
            }
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
            case FriendEffectType.ShopDiscount:
                return "購買部のアイテムが 20% OFF になります。";

            case FriendEffectType.DiceReroll:
                return "1ターンに1度、サイコロを振り直せます。";

            case FriendEffectType.NullifyGPMinus:
                return "GPが減るマスの効果を無効化します。";

            case FriendEffectType.MobFriendPromote:
                return "デート時に、確実に友達(モブ)が増えます。";

            case FriendEffectType.BadEventToGP:
                return "マイナスイベントをGP獲得に変換します。";

            case FriendEffectType.GPMultiplier:
                return "GP獲得マスの効果が 1.5倍 になります。";

            case FriendEffectType.DoubleTileEffect:
                return "マスの効果(増減)が 2倍 になります。";

            case FriendEffectType.CardGeneration:
                return "定期的に移動カードをプレゼントしてくれます。";

            case FriendEffectType.AutoFriend:
                return "毎ターン終了時、友達が1人増えます。";

            case FriendEffectType.DoubleScoreOnJoin:
                return "仲間になった時、友達の数が2倍になります。(発動済み)";

            default:
                return "特別な能力はありません。";
        }
    }
}