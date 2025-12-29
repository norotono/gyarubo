using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Linq;

public class GameManager : MonoBehaviour
{
    [Header("--- Managers (Attach Scripts) ---")]
    public BoardManager boardManager;
    public ShopManager shopManager;
    public EventManager eventManager;

    [Header("--- Game Settings ---")]
    [Range(1, 3)]
    public int currentGrade = 1;
    public float moveSpeed = 0.3f;

    [Header("--- References ---")]
    public PlayerStats playerStats;
    public PhoneUIManager phoneUI;
    public Button diceButton;
    public Image diceImage;
    public Sprite[] diceSprites;

    [Header("--- Main UI ---")]
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI assetText;
    public TextMeshProUGUI statusText;

    [Header("--- Data ---")]
    public List<FriendData> allFriends = new List<FriendData>();
    public List<string> floor2Rooms = new List<string> { "2-A", "2-B", "2-C", "理科室", "美術室" };

    // 内部変数
    private int currentTileIndex = 0;
    private bool isMoving = false;
    private int middleTileIndex = 24;

    private void Start()
    {
        if (playerStats == null) playerStats = PlayerStats.Instance;

        // 1. 友達データの初期化
        InitializeFriends();

        // 2. マップ生成
        boardManager.InitializeBoard(currentGrade);

        // 3. ショップデータ初期化
        shopManager.InitializeShopItems(currentGrade, playerStats);

        // 4. プレイヤー配置
        currentTileIndex = 0;
        StartCoroutine(InitPlayerPos());

        // 5. UI更新
        UpdateMainUI();
        if (diceButton) diceButton.onClick.AddListener(OnDiceClicked);
    }

    IEnumerator InitPlayerPos()
    {
        yield return null;
        boardManager.MovePlayerPiece(0);
    }

    // --- ダイス & 移動処理 ---

    public void OnDiceClicked()
    {
        if (isMoving) return;
        StartCoroutine(RollDiceSequence());
    }

    IEnumerator RollDiceSequence()
    {
        isMoving = true;
        diceButton.interactable = false;

        float timer = 0f;
        while (timer < 0.5f)
        {
            if (diceImage && diceSprites.Length > 0)
                diceImage.sprite = diceSprites[Random.Range(0, 6)];
            yield return new WaitForSeconds(0.05f);
            timer += 0.05f;
        }

        int result = Random.Range(1, 7);
        if (diceImage && diceSprites.Length >= 6)
            diceImage.sprite = diceSprites[result - 1];

        // ダイスボーナス
        if (result == 5) { playerStats.gp += 100; AddLog("ボーナス: GP+100"); }
        if (result == 6) { playerStats.friends += 1; AddLog("ボーナス: 友達+1"); }
        UpdateMainUI();

        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(MovePlayer(result));
    }

    IEnumerator MovePlayer(int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            currentTileIndex++;

            // 進級・ゴール判定
            if (currentTileIndex >= boardManager.totalTiles)
            {
                if (currentGrade < 3)
                {
                    currentGrade++;
                    currentTileIndex = 0;
                    AddLog($"進級！ {currentGrade}年生になりました。");

                    boardManager.InitializeBoard(currentGrade);
                    shopManager.InitializeShopItems(currentGrade, playerStats); // 商品更新
                    yield return null;

                    boardManager.MovePlayerPiece(0);
                    UpdateMainUI();
                    yield return new WaitForSeconds(moveSpeed);
                    continue;
                }
                else
                {
                    currentTileIndex = boardManager.totalTiles - 1;
                    boardManager.MovePlayerPiece(currentTileIndex);
                    AddLog("ゲーム終了！");
                    // ここにリザルト処理を入れる予定
                    break;
                }
            }

            boardManager.MovePlayerPiece(currentTileIndex);
            UpdateMainUI();

            // 通過時の購買チェック
            if (boardManager.BoardLayout[currentTileIndex] == "Shop" && i < steps - 1)
            {
                AddLog("購買部を通過します。");
                yield return StartCoroutine(shopManager.OpenShopSequence(playerStats, false));
                UpdateMainUI(); // 買い物後の更新
            }

            // 中間地点チェック
            if (currentTileIndex == middleTileIndex)
            {
                AddLog("中間イベント発生！");
                break;
            }

            yield return new WaitForSeconds(moveSpeed);
        }

        OnTileReached(currentTileIndex);
    }

    // --- マス到達時の処理 ---

    void OnTileReached(int tileIndex)
    {
        string type = boardManager.BoardLayout[tileIndex];

        // 親友効果のチェック
        int mult = 1;
        if (type != "Middle" && type != "Shop" && HasFriendEffect(FriendEffectType.DoubleTileEffect)) mult = 2;

        switch (type)
        {
            case "Event":
                HandleEventTile();
                return; // EndTurnはイベント後に呼ぶ

            case "Male":
                HandleMaleTile();
                return;

            case "Middle":
                HandleMiddleTile();
                return;

            case "Shop":
                Debug.Log("【購買部】ピッタリ停止！ 20% OFF!");
                // 購買コルーチン開始
                StartCoroutine(RunShopTileSequence());
                return;

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
        }

        EndTurn();
    }

    // --- イベントハンドリング ---

    IEnumerator RunShopTileSequence()
    {
        // 親友効果があれば常に割引
        bool discount = true; // マス停止なら割引
        yield return StartCoroutine(shopManager.OpenShopSequence(playerStats, discount));
        EndTurn();
    }

    void HandleEventTile()
    {
        string[] labels = { "同伴", "親友スカウト", "一人で遊ぶ" };
        UnityAction[] actions = new UnityAction[3];

        // 1. 同伴
        bool canDate = (playerStats.boyfriendCount > 0 || playerStats.maleFriendCount > 0);
        actions[0] = () => { playerStats.soloPlayConsecutive = 0; EndTurn(); };

        // 2. 親友スカウト
        FriendData target = CheckSpecialConditionFriend();
        actions[1] = () => {
            playerStats.soloPlayConsecutive = 0;
            if (target != null) RecruitFriend(target);
            EndTurn();
        };

        // 3. 一人で遊ぶ（ルーレット）
        actions[2] = () => {
            playerStats.soloPlayConsecutive++;
            StartCoroutine(RunRoulette());
        };

        eventManager.ShowChoicePanel("イベント", labels, actions);
        eventManager.SetButtonInteractable(0, canDate);
        eventManager.SetButtonInteractable(1, target != null && !canDate);
    }

    void HandleMaleTile()
    {
        string[] labels = { "情報", "友達になる", "会話 (GP+300)" };
        UnityAction[] actions = new UnityAction[3];

        actions[0] = () => { /* 情報処理 */ EndTurn(); };
        actions[1] = () => { playerStats.maleFriendCount++; EndTurn(); };
        actions[2] = () => { AddGP(300); EndTurn(); };

        eventManager.ShowChoicePanel("男子生徒", labels, actions);
    }

    void HandleMiddleTile()
    {
        string[] labels = { "GP +800", "親密度 +40", "モブ昇格" };
        UnityAction[] actions = new UnityAction[3];

        actions[0] = () => { AddGP(800); EndTurn(); };
        actions[1] = () => { playerStats.present += 40; EndTurn(); }; // 仮
        actions[2] = () => { EndTurn(); };

        eventManager.ShowChoicePanel("中間地点", labels, actions);
    }

    IEnumerator RunRoulette()
    {
        bool protection = HasFriendEffect(FriendEffectType.BadEventToGP);
        yield return StartCoroutine(eventManager.PlayRoulette(playerStats, protection));
        EndTurn();
    }

    // --- ターン終了・補助 ---

    void EndTurn()
    {
        int shinyu = allFriends.Count(f => f.isRecruited);
        playerStats.gp += playerStats.CalculateSalary(shinyu);
        if (HasFriendEffect(FriendEffectType.AutoFriend)) AddFriend(1);

        playerStats.currentTurn++;
        playerStats.currentMonth++;
        if (playerStats.currentMonth > 12) playerStats.currentMonth -= 12;

        UpdateMainUI();
        isMoving = false;
        diceButton.interactable = true;
    }

    void UpdateMainUI()
    {
        if (shopManager.IsShopOpen) shopManager.RefreshButtons(playerStats.gp); // ショップが開いていれば更新

        // 季節の仮計算
        int monthOffset = currentTileIndex / 4;
        playerStats.currentMonth = 4 + monthOffset;
        if (playerStats.currentMonth > 12) playerStats.currentMonth -= 12;

        if (dateText) dateText.text = $"{currentGrade}年目 {playerStats.currentMonth}月";
        if (assetText) assetText.text = $"友: {playerStats.friends}人\nGP: {playerStats.gp:N0}";
        if (statusText) statusText.text = $"コミュ力: Lv{playerStats.commuLv}\nギャル力: Lv{playerStats.galLv}\nレモン力: Lv{playerStats.lemonLv}";
    }

    // --- 補助メソッド ---
    void AddLog(string msg) { Debug.Log(msg); if (phoneUI) phoneUI.AddLog(msg); }
    void AddGP(int v) { if (HasFriendEffect(FriendEffectType.GPMultiplier)) v = (int)(v * 1.5f); playerStats.gp += v; }
    void AddFriend(int v) { playerStats.friends += v; }
    void RecruitFriend(FriendData f) { f.isRecruited = true; if (f.effectType == FriendEffectType.DoubleScoreOnJoin) playerStats.friends *= 2; }
    bool HasFriendEffect(FriendEffectType t) { return allFriends.Any(f => f.isRecruited && f.effectType == t); }

    void InitializeFriends()
    {
        foreach (var f in allFriends) { f.isRecruited = false; f.assignedCondition = ConditionType.None; }
        // 簡易実装：ランダム割り当てロジックは必要に応じてここに復活させる
    }

    FriendData CheckSpecialConditionFriend()
    {
        // 簡易実装：孤立条件のみチェック
        foreach (var f in allFriends)
        {
            if (!f.isRecruited && f.assignedCondition == ConditionType.Solitude && playerStats.soloPlayConsecutive >= 3) return f;
        }
        return null;
    }
}