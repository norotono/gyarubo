using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class GameManager : MonoBehaviour
{
    [Header("--- ゲーム設定 ---")]
    [Range(1, 3)]
    public int currentGrade = 1;
    public float moveSpeed = 0.3f;

    [Header("--- 参照: プレイヤー ---")]
    public PlayerStats playerStats;
    // 他の変数宣言の並びに追加してください
    public PhoneUIManager phoneUI;
    public Transform playerPiece;

    [Header("--- 参照: ダイス ---")]
    public Button diceButton;
    public Image diceImage;
    public Sprite[] diceSprites;

    [Header("--- 参照: UI表示 ---")]
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI assetText;
    public TextMeshProUGUI statusText;

    [Header("--- 参照: イベント選択パネル ---")]
    public GameObject eventSelectionPanel;
    public TextMeshProUGUI eventTitleText;
    public Button btnOption1; // 上ボタン
    public Button btnOption2; // 中ボタン
    public Button btnOption3; // 下ボタン
    public TextMeshProUGUI txtOption1; // ボタン内のテキスト
    public TextMeshProUGUI txtOption2;
    public TextMeshProUGUI txtOption3;

    [Header("--- 参照: 購買部UI (New!) ---")]
    public GameObject shopPanel;        // 購買画面全体
    public Transform shopContent;       // 商品ボタンを並べる場所
    public GameObject shopItemPrefab;   // 商品ボタンのプレハブ
    public TextMeshProUGUI shopInfoText; // 「いらっしゃいませ」等の表示
    public GameObject shopCloseButton;  // 購買を閉じるボタン(Inspectorでアタッチ想定)

    [Header("--- 参照: ルーレットUI (New!) ---")]
    public GameObject roulettePanel;      // ルーレット画面
    public TextMeshProUGUI rouletteText;  // 結果を表示するテキスト

    [Header("--- ボード設定 ---")]
    public GameObject tilePrefab;
    public Transform boardParent;
    public int totalTiles = 48;

    [Header("--- データ ---")]
    public List<FriendData> allFriends = new List<FriendData>();
    public List<string> floor2Rooms = new List<string> { "2-A", "2-B", "2-C", "理科室", "美術室" };
    public List<string> floor3Rooms = new List<string> { "3-A", "3-B", "音楽室", "視聴覚室" };

    // 内部変数
    private string[] boardLayout;
    private int currentTileIndex = 0;
    private bool isMoving = false;
    private int middleTileIndex = 24;

    // 購買・イベント制御用
    private List<ShopItem> shopItems = new List<ShopItem>();
    private bool isShopOpen = false; // ショップが開いているか

    // イベント状態管理
    private enum EventState { None, EventTile, MaleTile, MiddleTile }
    private EventState currentEventState = EventState.None;

    private void Start()
    {
        if (playerStats == null) playerStats = PlayerStats.Instance;

        // 初期化
        InitializeFriends();
        CreateBoard();
        DefineShopItems(); // 商品リスト作成

        // プレイヤー配置
        currentTileIndex = 0;
        StartCoroutine(InitPlayerPos());

        // UI初期化
        if (eventSelectionPanel) eventSelectionPanel.SetActive(false);
        if (shopPanel) shopPanel.SetActive(false);
        if (roulettePanel) roulettePanel.SetActive(false);
        UpdateUI();

        // ボタン設定
        if (diceButton) diceButton.onClick.AddListener(OnDiceClicked);

        // イベント選択肢ボタン
        if (btnOption1) btnOption1.onClick.AddListener(OnOption1Clicked);
        if (btnOption2) btnOption2.onClick.AddListener(OnOption2Clicked);
        if (btnOption3) btnOption3.onClick.AddListener(OnOption3Clicked);

        // 購買を閉じるボタンの設定（もしボタンがあれば）
        if (shopCloseButton)
        {
            Button closeBtn = shopCloseButton.GetComponent<Button>();
            if (closeBtn) closeBtn.onClick.AddListener(CloseShop);
        }
    }

    IEnumerator InitPlayerPos()
    {
        yield return null;
        MovePlayerPieceToTile(0);
    }

    // =========================================================
    // 1. ダイス & 移動 (購買通過チェック付き)
    // =========================================================

    public void OnDiceClicked()
    {
        if (isMoving) return;
        StartCoroutine(RollDiceSequence());
    }

    IEnumerator RollDiceSequence()
    {
        isMoving = true;
        diceButton.interactable = false;

        // ダイス演出
        float timer = 0f;
        while (timer < 0.5f)
        {
            if (diceImage && diceSprites.Length > 0)
                diceImage.sprite = diceSprites[Random.Range(0, 6)];
            yield return new WaitForSeconds(0.05f);
            timer += 0.05f;
        }

        // 出目確定
        int result = Random.Range(1, 7);
        if (diceImage && diceSprites.Length >= 6)
            diceImage.sprite = diceSprites[result - 1];

        // 移動ボーナス
        if (result == 5) { playerStats.gp += 100; Debug.Log("ボーナス: GP+100"); }
        if (result == 6) { playerStats.friends += 1; Debug.Log("ボーナス: 友達+1"); }
        UpdateUI();

        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(MovePlayer(result));
    }

    IEnumerator MovePlayer(int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            currentTileIndex++;

            // --- 学年更新 (シームレス遷移) ---
            if (currentTileIndex >= totalTiles)
            {
                if (currentGrade < 3)
                {
                    currentGrade++; // 学年UP
                    currentTileIndex = 0; // 位置リセット

                    AddLog($"進級！ {currentGrade}年生になりました。");

                    CreateBoard(); // ★即座に新マップ生成
                    yield return null; // 1フレーム待ってUI反映させる

                    MovePlayerPieceToTile(0); // 駒を新マップの0へ
                    UpdateUI(); // 月などをリセット

                    yield return new WaitForSeconds(moveSpeed);
                    continue; // 残りの歩数を継続
                }
                else
                {
                    // 3年生で48マス超えたらゴール
                    currentTileIndex = totalTiles - 1;
                    MovePlayerPieceToTile(currentTileIndex);
                    break;
                }
            }

            // 通常移動
            MovePlayerPieceToTile(currentTileIndex);
            UpdateUI(); // 移動するたびに月を更新

            // 購買部通過チェック (ゴール直前以外)
            if (boardLayout[currentTileIndex] == "Shop" && i < steps - 1)
            {
                AddLog("購買部を通過します。");
                yield return StartCoroutine(ShopSequence(false));
            }

            // 中間地点強制ストップ
            if (currentTileIndex == middleTileIndex)
            {
                AddLog("中間イベント発生！");
                break;
            }

            yield return new WaitForSeconds(moveSpeed);
        }

        OnTileReached(currentTileIndex);
    }
    // 引数名を logicalIndex としていますが、元の関数定義 (int index) を書き換えてください
    void MovePlayerPieceToTile(int logicalIndex)
    {
        if (boardParent.childCount <= logicalIndex) return;

        // ★スネーク移動計算 (1行8マス想定)
        int columns = 8;
        int row = logicalIndex / columns;
        int col = logicalIndex % columns;

        int visualIndex;

        // 偶数行(0,2...)は左→右、奇数行(1,3...)は右→左
        if (row % 2 == 0)
        {
            visualIndex = logicalIndex; // そのまま
        }
        else
        {
            // 逆順計算: 行の開始インデックス + (7 - 現在の列)
            int rowStart = row * columns;
            visualIndex = rowStart + (7 - col);
        }

        // 範囲内なら移動
        if (visualIndex >= 0 && visualIndex < boardParent.childCount)
        {
            if (playerPiece != null)
                playerPiece.position = boardParent.GetChild(visualIndex).position;
        }
    }

    // =========================================================
    // 2. マス到達時の処理
    // =========================================================

    public void OnTileReached(int tileIndex)
    {
        string type = boardLayout[tileIndex];
        Debug.Log($"マス到達: {type}");

        // ユナの効果: マス効果2倍 (中間・購買除く)
        int mult = 1;
        if (type != "Middle" && type != "Shop" && HasFriendEffect(FriendEffectType.DoubleTileEffect)) mult = 2;

        switch (type)
        {
            case "Event":
                SetupChoicePanel(EventState.EventTile);
                return; // 選択待ち

            case "Male":
                SetupChoicePanel(EventState.MaleTile);
                return; // 選択待ち

            case "Middle":
                SetupChoicePanel(EventState.MiddleTile);
                return; // 選択待ち

            case "Shop":
                Debug.Log("【購買部】ピッタリ停止！ 20% OFF!");
                StartCoroutine(ShopSequence(true)); // 割引ありで開く
                return; // 閉じるまで待つ

            case "GPPlus":
                int gVal = (currentGrade * 150 + playerStats.galLv * 100) * mult;
                AddGP(gVal);
                playerStats.gpIncreaseTileCount++;
                break;

            case "GPMinus":
                if (HasFriendEffect(FriendEffectType.NullifyGPMinus)) break;
                if (HasFriendEffect(FriendEffectType.BadEventToGP)) { AddGP(100 * mult); break; }
                int gLoss = (currentGrade * 100) * mult;
                playerStats.gp = Mathf.Max(0, playerStats.gp - gLoss);
                playerStats.gpDecreaseTileCount++;
                break;

            case "FriendPlus":
                int fVal = (currentGrade * 1 + playerStats.commuLv) * mult;
                AddFriend(fVal);
                break;

            case "FriendMinus":
                if (HasFriendEffect(FriendEffectType.BadEventToGP)) { AddGP(100 * mult); break; }
                int fLoss = 1 * mult;
                if (playerStats.friends > 0) playerStats.friends -= fLoss;
                break;

            case "Classroom":
                Debug.Log("教室マス: 親友スカウトチャンス");
                break;
        }

        // 選択肢がないマスはここでターン終了
        EndTurn();
    }

    // =========================================================
    // 3. 購買システム (実装し直し)
    // =========================================================

    void DefineShopItems()
    {
        shopItems.Clear();
        shopItems.Add(new ShopItem("生徒手帳", 200, "教室用", () => playerStats.studentIdCount++));
        shopItems.Add(new ShopItem("移動カード", 150, "ランダム", () => playerStats.moveCards.Add(Random.Range(1, 7))));
        shopItems.Add(new ShopItem("プレゼント", 500, "親密度UP", () => playerStats.present++));
        shopItems.Add(new ShopItem("イベント強制", 800, "マス無視", () => playerStats.eventForce++));
        shopItems.Add(new ShopItem("ステータスUP", 1500, "Lv+1", () => playerStats.commuLv++));

        if (currentGrade == 3)
        {
            shopItems.Add(new ShopItem("卒業写真", 100, "友+1", () => playerStats.friends++));
            shopItems.Add(new ShopItem("卒業アルバム", 1000, "友+10", () => { playerStats.friends += 10; playerStats.albumPrice += 500; }));
        }
    }

    IEnumerator ShopSequence(bool isDiscount)
    {
        if (HasFriendEffect(FriendEffectType.ShopDiscount)) isDiscount = true; // エミ効果

        if (shopPanel) shopPanel.SetActive(true);
        if (shopInfoText) shopInfoText.text = isDiscount ? "<color=red>全品 20% OFF!</color>" : "いらっしゃいませ";

        // ボタン生成
        foreach (Transform child in shopContent) Destroy(child.gameObject);

        foreach (var item in shopItems)
        {
            GameObject btnObj = Instantiate(shopItemPrefab, shopContent);
            int price = isDiscount ? (int)(item.price * 0.8f) : item.price;

            // テキスト設定
            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (txt) txt.text = $"{item.itemName}\n{price}G";

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => OnBuyItem(item, price));

            if (playerStats.gp < price) btn.interactable = false;
        }

        // 閉じるボタンが押されるまで待機
        isShopOpen = true;
        while (isShopOpen)
        {
            yield return null;
        }

        if (shopPanel) shopPanel.SetActive(false);

        // マスに止まっていたならターン終了、通過中ならEndTurnせず移動再開へ戻る
        if (boardLayout[currentTileIndex] == "Shop") EndTurn();
    }

    public void CloseShop()
    {
        isShopOpen = false; // ループを抜ける
    }

    void OnBuyItem(ShopItem item, int price)
    {
        if (playerStats.gp >= price)
        {
            playerStats.gp -= price;
            item.onBuy.Invoke();
            UpdateUI();
            // ボタンの再生成などは省略（簡易実装）
        }
    }

    // =========================================================
    // 4. 一人で遊ぶ (ルーレット)
    // =========================================================

    public void OnSelectPlayAlone()
    {
        ClosePanel();
        playerStats.soloPlayConsecutive++;
        StartCoroutine(RouletteSequence());
    }

    IEnumerator RouletteSequence()
    {
        if (roulettePanel) roulettePanel.SetActive(true);

        string[] results = { "友達+", "GP+", "GP-", "友達-", "ステUP" };

        // 1. ルーレット演出 (パラパラ)
        float timer = 0f;
        while (timer < 2.0f)
        {
            if (rouletteText) rouletteText.text = results[Random.Range(0, results.Length)];
            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;
        }

        // 2. 結果確定
        int roll = Random.Range(0, 100);
        string msg = "";
        bool noah = HasFriendEffect(FriendEffectType.BadEventToGP);

        if (roll < 30) { AddFriend(1); msg = "友達ゲット！"; }
        else if (roll < 55) { AddGP(300); msg = "GPゲット！"; }
        else if (roll < 80)
        {
            if (noah) { AddGP(100); msg = "親友効果: GP+100"; }
            else { playerStats.gp = Mathf.Max(0, playerStats.gp - 200); msg = "GP減少..."; }
        }
        else if (roll < 95)
        {
            if (noah) { AddGP(100); msg = "親友効果: GP+100"; }
            else { if (playerStats.friends > 0) playerStats.friends--; msg = "友達減少..."; }
        }
        else { playerStats.commuLv++; msg = "ステータスUP!"; }

        if (rouletteText) rouletteText.text = msg;
        yield return new WaitForSeconds(1.5f);

        if (roulettePanel) roulettePanel.SetActive(false);
        EndTurn();
    }

    // =========================================================
    // 5. イベント選択肢 (3択)
    // =========================================================

    void SetupChoicePanel(EventState state)
    {
        currentEventState = state;
        if (eventSelectionPanel) eventSelectionPanel.SetActive(true);
        btnOption1.interactable = true;
        btnOption2.interactable = true;
        btnOption3.interactable = true;

        if (state == EventState.EventTile)
        {
            SetText("イベント", "同伴", "親友スカウト", "一人で遊ぶ");

            bool canDate = (playerStats.boyfriendCount > 0 || playerStats.maleFriendCount > 0);
            btnOption1.interactable = canDate;

            FriendData target = CheckSpecialConditionFriend();
            btnOption2.interactable = (target != null && !canDate);
        }
        else if (state == EventState.MaleTile)
        {
            SetText("男子生徒", "情報", "友達になる", "会話 (GP+300)");
        }
        else if (state == EventState.MiddleTile)
        {
            SetText("中間地点", "GP +800", "親密度 +40", "モブ昇格");
        }
    }

    void SetText(string title, string op1, string op2, string op3)
    {
        if (eventTitleText) eventTitleText.text = title;
        if (txtOption1) txtOption1.text = op1;
        if (txtOption2) txtOption2.text = op2;
        if (txtOption3) txtOption3.text = op3;
    }

    void ClosePanel() { if (eventSelectionPanel) eventSelectionPanel.SetActive(false); }

    // --- ボタンクリック時の処理 ---
    public void OnOption1Clicked()
    {
        ClosePanel();
        if (currentEventState == EventState.EventTile) // 同伴
        {
            Debug.Log("同伴イベント");
            playerStats.soloPlayConsecutive = 0;
        }
        else if (currentEventState == EventState.MaleTile) // 情報
        {
            Debug.Log("情報開示");
        }
        else if (currentEventState == EventState.MiddleTile) // GP+800
        {
            AddGP(800);
        }
        EndTurn();
    }

    public void OnOption2Clicked()
    {
        ClosePanel();
        if (currentEventState == EventState.EventTile) // スカウト
        {
            playerStats.soloPlayConsecutive = 0;
            FriendData f = CheckSpecialConditionFriend();
            if (f != null) RecruitFriend(f);
        }
        else if (currentEventState == EventState.MaleTile) // 友達
        {
            playerStats.maleFriendCount++;
        }
        EndTurn();
    }

    public void OnOption3Clicked()
    {
        if (currentEventState == EventState.EventTile) // 一人で遊ぶ(ルーレットへ)
        {
            OnSelectPlayAlone();
            return; // ここだけEndTurnしない
        }

        ClosePanel();
        if (currentEventState == EventState.MaleTile) // 会話
        {
            AddGP(300);
        }
        EndTurn();
    }

    // =========================================================
    // 6. ターン終了・補助
    // =========================================================

    public void EndTurn()
    {
        int shinyu = allFriends.Count(f => f.isRecruited);
        playerStats.gp += playerStats.CalculateSalary(shinyu);
        if (HasFriendEffect(FriendEffectType.AutoFriend)) AddFriend(1);

        playerStats.currentTurn++;
        playerStats.currentMonth++;
        if (playerStats.currentMonth > 12) playerStats.currentMonth -= 12;

        UpdateUI();
        isMoving = false;
        diceButton.interactable = true;
    }

    void UpdateUI()
    {
        // 季節計算: マス番号(0-47)を4で割って月を算出 (0-3:4月, 4-7:5月...)
        int monthOffset = currentTileIndex / 4;
        playerStats.currentMonth = 4 + monthOffset;
        if (playerStats.currentMonth > 12) playerStats.currentMonth -= 12;

        // 日付表示
        if (dateText != null)
            dateText.text = $"{currentGrade}年目\n{playerStats.currentMonth}月";

        // 資産表示 (縦並び)
        if (assetText != null)
            assetText.text = $"GP\n{playerStats.gp:N0}\n\n友\n{playerStats.friends}人";

        // ステータス表示 (縦並び)
        if (statusText != null)
            statusText.text = $"Com\n{playerStats.commuLv}\n\nGal\n{playerStats.galLv}\n\nLem\n{playerStats.lemonLv}";
    }

    // --- マップ生成・親友初期化 (省略なし) ---
    void InitializeFriends()
    {
        foreach (var f in allFriends) { f.isRecruited = false; f.assignedCondition = ConditionType.None; }
        var ai = allFriends.FirstOrDefault(f => f.isAi);
        var others = allFriends.Where(f => !f.isAi).OrderBy(x => Random.value).ToList();
        if (ai != null) ai.assignedCondition = ConditionType.Ai_Fixed;

        var clGroup = others.Take(5).ToList();
        var spGroup = others.Skip(5).Take(4).ToList();

        foreach (var c in clGroup)
        {
            c.assignedCondition = ConditionType.Classroom;
            c.assignedRoom = (floor2Rooms.Count > 0) ? floor2Rooms[Random.Range(0, floor2Rooms.Count)] : "教室";
        }
        List<ConditionType> pool = new List<ConditionType> { ConditionType.Conversation, ConditionType.Happiness, ConditionType.Unhappiness, ConditionType.DiceOne, ConditionType.Rich, ConditionType.Wasteful, ConditionType.Popularity, ConditionType.Steps, ConditionType.Solitude, ConditionType.StatusAll2 };
        pool = pool.OrderBy(x => Random.value).ToList();
        for (int i = 0; i < spGroup.Count; i++) spGroup[i].assignedCondition = pool[i];
    }

    void CreateBoard()
    {
        foreach (Transform child in boardParent) Destroy(child.gameObject);
        boardLayout = new string[totalTiles];

        // 固定マス設定
        boardLayout[0] = "Start";
        boardLayout[24] = "Middle";
        boardLayout[46] = "Shop";

        // ★修正: 3年生のみゴール、それ以外は通常移動用のマスにする
        boardLayout[47] = (currentGrade == 3) ? "Goal" : "Event";

        if (currentGrade >= 2)
        {
            int[] cIdx = { 5, 13, 21, 29, 37, 45 };
            foreach (int i in cIdx) if (i < totalTiles) boardLayout[i] = "Classroom";
        }

        List<string> p = new List<string>();
        if (currentGrade == 1)
        {
            AddTiles(p, "Male", 12); AddTiles(p, "Event", 12); AddTiles(p, "Shop", 1);
            AddTiles(p, "FriendPlus", 8); AddTiles(p, "GPPlus", 6); AddTiles(p, "GPMinus", 4); AddTiles(p, "FriendMinus", 2);
        }
        else
        {
            AddTiles(p, "GPPlus", 10); AddTiles(p, "FriendPlus", 8); AddTiles(p, "Event", 12);
            AddTiles(p, "Shop", 1); AddTiles(p, "GPMinus", 4); AddTiles(p, "Male", 3); AddTiles(p, "FriendMinus", 2);
        }
        p = p.OrderBy(x => Random.value).ToList();

        int pIdx = 0;
        for (int i = 0; i < totalTiles; i++)
        {
            if (string.IsNullOrEmpty(boardLayout[i]))
            {
                boardLayout[i] = (pIdx < p.Count) ? p[pIdx++] : "Normal";
            }
            GameObject t = Instantiate(tilePrefab, boardParent);
            t.name = $"Tile_{i}_{boardLayout[i]}";

            TileData td = t.GetComponent<TileData>();
            if (td != null)
            {
                // ここは論理インデックスのまま渡す
                td.index = i;
                td.type = ConvertStr(boardLayout[i]);
                td.UpdateVisuals();
            }
        }
    }
    void AddTiles(List<string> l, string t, int c) { for (int i = 0; i < c; i++) l.Add(t); }

    public void AddLog(string message)
    {
        Debug.Log(message);
        if (phoneUI != null)
        {
            phoneUI.AddLog(message);
        }
    }

    // --- 補助 ---
    void AddGP(int v) { if (HasFriendEffect(FriendEffectType.GPMultiplier)) v = (int)(v * 1.5f); playerStats.gp += v; }
    void AddFriend(int v) { playerStats.friends += v; }
    void RecruitFriend(FriendData f) { f.isRecruited = true; if (f.effectType == FriendEffectType.DoubleScoreOnJoin) playerStats.friends *= 2; }
    bool HasFriendEffect(FriendEffectType t) { return allFriends.Any(f => f.isRecruited && f.effectType == t); }
    FriendData CheckSpecialConditionFriend()
    {
        foreach (var f in allFriends)
        {
            if (f.isRecruited || f.assignedCondition == ConditionType.Classroom) continue;
            if (f.assignedCondition == ConditionType.Solitude && playerStats.soloPlayConsecutive >= 3) return f;
        }
        return null;
    }
    TileType ConvertStr(string s)
    {
        switch (s)
        {
            case "Start": return TileType.Start;
            case "Goal": return TileType.Goal;
            case "Male": return TileType.Boy;
            case "Event": return TileType.Event;
            case "Shop": return TileType.Shop;
            case "Classroom": return TileType.Classroom;
            case "FriendPlus": return TileType.Friend_Plus;
            case "FriendMinus": return TileType.Friend_Minus;
            case "GPPlus": return TileType.GP_Plus;
            case "GPMinus": return TileType.GP_Minus;
            default: return TileType.Normal;
        }
    }
}