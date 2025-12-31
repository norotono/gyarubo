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
    public BoyfriendManager boyfriendManager; // ★Inspectorでアタッチする！

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

        // ★追加: 1の目カウント (親友条件: DiceOne用)
        if (result == 1) playerStats.diceOneCount++;

        // ダイスボーナス
        if (result == 5) { playerStats.gp += 100; AddLog("ボーナス: GP+100"); }
        if (result == 6) { playerStats.friends += 1; AddLog("ボーナス: 友達+1"); }
        UpdateMainUI();

        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(MovePlayer(result));
    }

    IEnumerator MovePlayer(int steps)
    {
        // ★追加: 歩数カウントを加算 (親友条件: Steps用)
        playerStats.totalSteps += steps;

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
                    shopManager.InitializeShopItems(currentGrade, playerStats);
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
                    break;
                }
            }

            boardManager.MovePlayerPiece(currentTileIndex);
            UpdateMainUI();

            // 通過時の購買チェック
            // 通過時の購買チェック
            if (boardManager.BoardLayout[currentTileIndex] == "Shop" && i < steps - 1)
            {
                AddLog("購買部を通過します。");

                // ★修正: エミ（ShopDiscount）がいれば通過時でも割引ONにする
                bool hasDiscount = HasFriendEffect(FriendEffectType.ShopDiscount);

                yield return StartCoroutine(shopManager.OpenShopSequence(playerStats, hasDiscount));
                UpdateMainUI();
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

        // 親友効果のチェック: ユナ（マス効果2倍）
        int mult = 1;
        if (type != "Middle" && type != "Shop" && HasFriendEffect(FriendEffectType.DoubleTileEffect)) mult = 2;

        switch (type)
        {
            case "Event":
                HandleEventTile();
                return;

            case "Male":
                HandleMaleTile();
                return;

            case "Middle":
                HandleMiddleTile();
                return;

            case "Shop":
                Debug.Log("【購買部】ピッタリ停止！ 20% OFF!");
                StartCoroutine(RunShopTileSequence());
                return;

            case "GPPlus":
                int gVal = (currentGrade * 150 + playerStats.galLv * 100) * mult;
                // ★追加: ミレイの能力 (GP獲得1.5倍)
                if (HasFriendEffect(FriendEffectType.GPMultiplier)) gVal = (int)(gVal * 1.5f);

                playerStats.gp += gVal;
                playerStats.gpIncreaseTileCount++;
                break;

            case "GPMinus":
                // ★追加: ノアの能力 (減少→増加変換)
                if (HasFriendEffect(FriendEffectType.BadEventToGP))
                {
                    int bonus = 100 * mult;
                    if (HasFriendEffect(FriendEffectType.GPMultiplier)) bonus = (int)(bonus * 1.5f);
                    playerStats.gp += bonus;
                    AddLog("ノアの能力: GP減少を回避して逆に獲得！");
                    break;
                }
                // ★追加: サオリの能力 (減少無効化)
                if (HasFriendEffect(FriendEffectType.NullifyGPMinus))
                {
                    AddLog("サオリの能力: GP減少を無効化した。");
                    break;
                }

                int gLoss = (currentGrade * 100) * mult;
                playerStats.gp = Mathf.Max(0, playerStats.gp - gLoss);
                playerStats.gpDecreaseTileCount++;
                break;

            case "FriendPlus":
                int fVal = (currentGrade * 1 + playerStats.commuLv) * mult;
                AddFriend(fVal);
                break;

            case "FriendMinus":
                // ★追加: ノアの能力 (減少→増加変換)
                if (HasFriendEffect(FriendEffectType.BadEventToGP))
                {
                    int bonus = 100 * mult;
                    if (HasFriendEffect(FriendEffectType.GPMultiplier)) bonus = (int)(bonus * 1.5f);
                    playerStats.gp += bonus;
                    AddLog("ノアの能力: 友達減少を回避してGPを獲得！");
                    break;
                }

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
        UnityEngine.Events.UnityAction[] actions = new UnityEngine.Events.UnityAction[3];

        // 1. 情報 (実装済みと仮定)
        actions[0] = () => { /* 情報処理 */ EndTurn(); };

        // 2. 友達になる (修正箇所)
        actions[1] = () => {
            boyfriendManager.AddNewMaleFriend(); // ★呼び出し
            AddLog("新しい男友達ができました！");
            playerStats.maleContactCount++; // 接触カウントは共通
            EndTurn();
        };

        // 3. 会話
        actions[2] = () => {
            AddGP(300);
            playerStats.maleContactCount++;
            EndTurn();
        };

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

        // レナの能力: 毎ターン友達+1
        if (HasFriendEffect(FriendEffectType.AutoFriend)) AddFriend(1);

        // ★追加: リカの能力 (12ターンごとにカード生成)
        if (HasFriendEffect(FriendEffectType.CardGeneration) && playerStats.currentTurn % 12 == 0)
        {
            if (playerStats.moveCards.Count < 5)
            {
                playerStats.moveCards.Add(Random.Range(1, 7));
                AddLog("リカの能力: 定期便で移動カードが届きました！");
            }
        }

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
    // 【追加】親友加入処理
    void RecruitFriend(FriendData f)
    {
        if (f.isRecruited) return;

        f.isRecruited = true;
        AddLog($"【祝】{f.name} が親友になった！");

        // アイの能力: 加入時に一度だけ友達数2倍
        if (f.effectType == FriendEffectType.DoubleScoreOnJoin)
        {
            playerStats.friends *= 2;
            AddLog("アイの能力発動！ 友達の数が 2倍 になった！");
            UpdateMainUI();
        }
    }
    bool HasFriendEffect(FriendEffectType t) { return allFriends.Any(f => f.isRecruited && f.effectType == t); }

    // 【変更】親友データの初期化と条件のランダム割り当て
    void InitializeFriends()
    {
        // 1. 全員の状態をリセット
        foreach (var f in allFriends)
        {
            f.isRecruited = false;
            f.assignedCondition = ConditionType.None;
            f.assignedRoom = "";
        }

        // 2. 「アイ」は特殊条件固定
        var ai = allFriends.FirstOrDefault(f => f.isAi);
        if (ai != null)
        {
            ai.assignedCondition = ConditionType.Ai_Fixed;
        }

        // 3. アイ以外のメンバーをシャッフル
        var otherFriends = allFriends.Where(f => !f.isAi).OrderBy(x => Random.value).ToList();

        // 4. 前半5名を「教室」に配置
        // floor2Rooms: 2-A, 2-B... などの部屋リストを使用
        for (int i = 0; i < 5; i++)
        {
            if (i < otherFriends.Count)
            {
                otherFriends[i].assignedCondition = ConditionType.Classroom;
                // 部屋リストがあれば割り当て（なければ仮の部屋名）
                if (i < floor2Rooms.Count) otherFriends[i].assignedRoom = floor2Rooms[i];
                else otherFriends[i].assignedRoom = "空き教室";
            }
        }

        // 5. 後半4名を「ランダムな特殊条件」に割り当て
        List<ConditionType> randomConditions = new List<ConditionType>
        {
            ConditionType.Conversation, ConditionType.Happiness, ConditionType.Unhappiness,
            ConditionType.DiceOne, ConditionType.Rich, ConditionType.Wasteful,
            ConditionType.Popularity, ConditionType.Steps, ConditionType.Solitude,
            ConditionType.StatusAll2
        };
        // 条件リストをシャッフル
        randomConditions = randomConditions.OrderBy(x => Random.value).ToList();

        for (int i = 5; i < otherFriends.Count; i++)
        {
            // 残りのメンバーに条件を割り振る
            int condIndex = i - 5;
            if (condIndex < randomConditions.Count)
            {
                otherFriends[i].assignedCondition = randomConditions[condIndex];
            }
        }

        // デバッグ用: 割り当て結果を表示
        foreach (var f in allFriends)
            Debug.Log($"[Friend Init] {f.name}: {f.assignedCondition} / {f.assignedRoom}");
    }

    // 【変更】親友出現条件の判定
    FriendData CheckSpecialConditionFriend()
    {
        foreach (var f in allFriends)
        {
            // 既に仲間、または教室配置のキャラはスキップ
            if (f.isRecruited || f.assignedCondition == ConditionType.Classroom) continue;

            bool isMet = false;

            switch (f.assignedCondition)
            {
                case ConditionType.Ai_Fixed:
                    // 男子接触8回以上 or 所持金5000以上
                    isMet = (playerStats.maleContactCount >= 8 || playerStats.gp >= 5000);
                    break;

                case ConditionType.Conversation: // 会話: 男子接触4回以上
                    isMet = (playerStats.maleContactCount >= 4);
                    break;

                case ConditionType.Happiness: // 幸福: GP増幅マス5回以上
                    isMet = (playerStats.gpIncreaseTileCount >= 5);
                    break;

                case ConditionType.Unhappiness: // 不幸: GP減少マス3回以上
                    isMet = (playerStats.gpDecreaseTileCount >= 3);
                    break;

                case ConditionType.DiceOne: // ダイス: 1の目3回以上
                    isMet = (playerStats.diceOneCount >= 3);
                    break;

                case ConditionType.Rich: // 金満: 3000 GP 以上
                    isMet = (playerStats.gp >= 3000);
                    break;

                case ConditionType.Wasteful: // 浪費: 購買消費 4000 GP 以上
                    isMet = (playerStats.shopSpentTotal >= 4000);
                    break;

                case ConditionType.Popularity: // 友達: 20人以上
                    isMet = (playerStats.friends >= 20);
                    break;

                case ConditionType.Steps: // 歩数: 合計80歩以上
                    isMet = (playerStats.totalSteps >= 80);
                    break;

                case ConditionType.Solitude: // 孤独: ぼっち3連続
                    isMet = (playerStats.soloPlayConsecutive >= 3);
                    break;

                case ConditionType.StatusAll2: // ステータス: 全て2以上
                    isMet = playerStats.IsAllStatsOver(2);
                    break;
            }

            if (isMet) return f; // 条件を満たした最初の親友を返す
        }
        return null;
    }
}