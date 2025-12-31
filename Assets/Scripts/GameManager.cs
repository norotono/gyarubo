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
    // 親友効果を持っているかチェック

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
        // 男友達（彼氏含む）がいるか確認
        bool hasBoys = playerStats.maleFriendsList.Count > 0;

        if (hasBoys)
        {
            // 選択肢を出す
            string[] labels = { "デート/お出かけ", "一人で遊ぶ" };
            UnityEngine.Events.UnityAction[] actions = new UnityEngine.Events.UnityAction[2];

            // A. デート
            actions[0] = () =>
            {
                StartCoroutine(RunDateEvent());
            };

            // B. 一人で遊ぶ（親友チェック -> ランダムイベント）
            actions[1] = () =>
            {
                playerStats.soloPlayConsecutive++; // ぼっち回数加算
                CheckSoloEvent(); // 親友判定へ
            };

            eventManager.ShowChoicePanel("休日の過ごし方は？", labels, actions);
        }
        else
        {
            // 男友達がいないので強制的に一人
            AddLog("一緒に遊ぶ男友達がいません……。一人で遊びます。");
            playerStats.soloPlayConsecutive++;
            CheckSoloEvent();
        }
    }

    // デートイベント実行
    IEnumerator RunDateEvent()
    {
        playerStats.soloPlayConsecutive = 0; // ぼっち連鎖リセット

        // 親密度上昇 (+レモン力補正)
        float baseAffection = 20f + (playerStats.lemonLv * 5);
        string result = boyfriendManager.IncreaseAffection(baseAffection);
        AddLog($"お出かけしました。\n{result}");

        // マキの能力（モブ親友昇格）
        if (HasFriendEffect(FriendEffectType.MobFriendPromote))
        {
            AddLog("マキの能力: 確実に親友(モブ)へ昇格させます！");
            AddFriend(1);
        }
        else
        {
            // 通常確率(例:10%)
            if (Random.Range(0, 100) < 10)
            {
                AddLog("なんと！ 友達が親友(モブ)に昇格しました！");
                AddFriend(1);
            }
        }

        yield return new WaitForSeconds(1.0f);
        EndTurn();
    }

    // 一人で遊ぶ時の判定（親友 -> ランダム）
    // 【変更】一人で遊ぶ（ルーレット演出付き）
    void CheckSoloEvent()
    {
        // 親友確定条件のチェック（ここは確定なので演出なしで即出現でもOKだが、演出に組み込んでも良い）
        FriendData newFriend = CheckSpecialConditionFriend();

        if (newFriend != null)
        {
            // 条件を満たした親友がいる場合
            RecruitFriend(newFriend);
            EndTurn();
        }
        else
        {
            // ランダムイベント（ルーレット開始）
            StartCoroutine(PlayRouletteSequence());
        }
    }

    // 【追加】ルーレット演出のコルーチン
    IEnumerator PlayRouletteSequence()
    {
        AddLog("今日は何をしようかな……？");
        isMoving = true; // 操作ブロック

        // ルーレットの候補テキスト
        string[] candidates = {
            "新しい友達ができるかも？",
            "臨時収入の予感！",
            "無駄遣いしちゃうかも…",
            "友達と喧嘩しちゃうかも…",
            "勉強してステータスUP！"
        };

        // 演出：テキストをパラパラ切り替える（ログまたは専用パネル）
        // ※ここでは簡易的にLogを更新する演出とします
        float duration = 2.0f;
        float timer = 0f;
        float interval = 0.1f;

        while (timer < duration)
        {
            // ランダムに候補を表示（実際は専用のUI Textを更新するのが望ましいですが、ここではログウィンドウで表現）
            // もし専用の「結果表示テキスト」があるなら、statusText.text = candidates[...] 等を使用してください
            int randomIndex = Random.Range(0, candidates.Length);
            // statusText.text = $"ルーレット中... {candidates[randomIndex]}"; // もし使うなら

            yield return new WaitForSeconds(interval);
            timer += interval;
            // 徐々に遅くする演出
            interval *= 1.1f;
        }

        // 結果確定
        int roll = Random.Range(0, 100);
        string resultMsg = "";

        if (roll < 30) // 0-29: 友達+
        {
            int val = currentGrade * 1 + playerStats.commuLv;
            AddFriend(val);
            resultMsg = "新しい友達ができた！";
        }
        else if (roll < 55) // 30-54: GP+
        {
            int gain = currentGrade * 150 + playerStats.galLv * 100;
            AddGP(gain);
            resultMsg = $"臨時収入！ GP+{gain}";
        }
        else if (roll < 80) // 55-79: GP-
        {
            int loss = currentGrade * 100;
            if (HasFriendEffect(FriendEffectType.NullifyGPMinus)) loss = 0;
            playerStats.gp = Mathf.Max(0, playerStats.gp - loss);
            resultMsg = $"無駄遣いしてしまった…… GP-{loss}";
        }
        else if (roll < 95) // 80-94: 友達-
        {
            if (playerStats.friends > 0) playerStats.friends--;
            resultMsg = "友達と喧嘩してしまった…… 友達-1";
        }
        else // 95-99: ステータスUP
        {
            // ランダムにステータスアップ
            int type = Random.Range(0, 3);
            if (type == 0) { playerStats.commuLv++; resultMsg = "勉強してコミュ力が上がった！"; }
            else if (type == 1) { playerStats.galLv++; resultMsg = "メイクの研究をしてギャル力が上がった！"; }
            else { playerStats.lemonLv++; resultMsg = "恋バナをしてレモン力が上がった！"; }
        }

        AddLog($"結果: {resultMsg}");

        yield return new WaitForSeconds(1.0f);
        isMoving = false;
        EndTurn();
    }

    void HandleMaleTile()
    {
        string[] labels = { "情報 (ヒント)", "友達になる", "会話 (GP+300)" };
        UnityEngine.Events.UnityAction[] actions = new UnityEngine.Events.UnityAction[3];

        // 1. 情報
        actions[0] = () =>
        {
            // nullを除外して検索
            var unknownFriends = allFriends
                .Where(f => f != null && !f.isRecruited && !f.isHintRevealed)
                .OrderBy(x => Random.value)
                .ToList();

            if (unknownFriends.Count > 0)
            {
                var target = unknownFriends[0];
                target.isHintRevealed = true;

                string hint = target.GetHintText();
                AddLog($"【情報】\n{hint}");

                AddGP(500);
                AddFriend(2);
                AddLog("情報を入手した報酬: GP+500, 友達+2");
            }
            else
            {
                AddLog("めぼしい情報はもうないみたい……。");
            }
            EndTurn();
        };

        // 2. 友達になる (BoyfriendManagerのnullチェック追加)
        actions[1] = () => {
            if (boyfriendManager != null)
            {
                boyfriendManager.AddNewMaleFriend();
                AddLog("新しい男友達ができました！");
            }
            else
            {
                Debug.LogError("BoyfriendManagerがアタッチされていません！");
            }
            playerStats.maleContactCount++;
            EndTurn();
        };

        // 3. 会話
        actions[2] = () => {
            AddGP(300);
            playerStats.maleContactCount++;
            EndTurn();
        };

        eventManager.ShowChoicePanel("男子生徒に話しかけますか？", labels, actions);
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

    // 【修正】ターン終了処理（完全版）
    void EndTurn()
    {
        // 1. 給料計算 (null安全化)
        int shinyu = 0;
        if (allFriends != null)
        {
            shinyu = allFriends.Count(f => f != null && f.isRecruited);
        }
        playerStats.gp += playerStats.CalculateSalary(shinyu);

        // 2. レナの能力 (友達+1)
        if (HasFriendEffect(FriendEffectType.AutoFriend)) AddFriend(1);

        // 3. 彼氏の能力発動 (null安全化)
        // BoyfriendManagerがアタッチされていれば発動
        if (boyfriendManager != null)
        {
            string bfLog = boyfriendManager.ActivateBoyfriendEffects();
            if (!string.IsNullOrEmpty(bfLog)) AddLog(bfLog);
        }

        // 4. リカの能力 (カード生成)
        if (HasFriendEffect(FriendEffectType.CardGeneration) && playerStats.currentTurn % 12 == 0)
        {
            if (playerStats.moveCards.Count < 5)
            {
                playerStats.moveCards.Add(Random.Range(1, 7));
                AddLog("リカの能力: 定期便で移動カードが届きました！");
            }
        }

        // 5. 日付更新
        playerStats.currentTurn++;
        playerStats.currentMonth++;
        if (playerStats.currentMonth > 12) playerStats.currentMonth -= 12;

        // 6. UI更新と操作許可
        UpdateMainUI();
        isMoving = false;

        if (diceButton != null) diceButton.interactable = true;
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
    public bool HasFriendEffect(FriendEffectType t)
    {
        // allFriends自体がnullでないか確認し、かつ各要素(f)がnullでないか確認する
        return allFriends != null && allFriends.Any(f => f != null && f.isRecruited && f.effectType == t);
    }

    // 【変更】親友データの初期化と条件のランダム割り当て
    // 【修正】GameManager.cs の InitializeFriends 関数
    void InitializeFriends()
    {
        // 安全対策: リスト自体がない場合はエラーログを出して終了
        if (allFriends == null)
        {
            Debug.LogError("【Error】GameManagerの 'All Friends' リストが設定されていません！");
            return;
        }

        // 1. 全員の状態をリセット (nullはスキップ)
        foreach (var f in allFriends)
        {
            if (f == null) continue; // ★ここが重要
            f.isRecruited = false;
            f.assignedCondition = ConditionType.None;
            f.assignedRoom = "";
            f.isHintRevealed = false;
        }

        // 2. 「アイ」は特殊条件固定
        var ai = allFriends.FirstOrDefault(f => f != null && f.isAi);
        if (ai != null)
        {
            ai.assignedCondition = ConditionType.Ai_Fixed;
        }

        // 3. アイ以外のメンバーをシャッフル (nullを除外してリスト化)
        var otherFriends = allFriends
            .Where(f => f != null && !f.isAi)
            .OrderBy(x => Random.value)
            .ToList();

        // 4. 前半5名を「教室」に配置
        // 部屋リストの安全対策
        if (floor2Rooms == null || floor2Rooms.Count == 0)
            floor2Rooms = new List<string> { "2-A", "2-B", "2-C", "3-A", "3-B" };

        for (int i = 0; i < 5; i++)
        {
            if (i < otherFriends.Count)
            {
                otherFriends[i].assignedCondition = ConditionType.Classroom;
                otherFriends[i].assignedRoom = floor2Rooms[i % floor2Rooms.Count];
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
        randomConditions = randomConditions.OrderBy(x => Random.value).ToList();

        for (int i = 5; i < otherFriends.Count; i++)
        {
            int condIndex = i - 5;
            if (condIndex < randomConditions.Count)
            {
                otherFriends[i].assignedCondition = randomConditions[condIndex];
            }
        }

        // デバッグ用
        foreach (var f in allFriends)
            if (f != null) Debug.Log($"[Friend Init] {f.name}: {f.assignedCondition} / {f.assignedRoom}");
    }
    // 【変更】親友出現条件の判定
    FriendData CheckSpecialConditionFriend()
    {
        if (allFriends == null) return null;

        foreach (var f in allFriends)
        {
            // ★追加: nullチェック
            if (f == null) continue;

            // 既に仲間、または教室配置のキャラはスキップ
            if (f.isRecruited || f.assignedCondition == ConditionType.Classroom) continue;

            bool isMet = false;

            switch (f.assignedCondition)
            {
                case ConditionType.Ai_Fixed:
                    isMet = (playerStats.maleContactCount >= 8 || playerStats.gp >= 5000);
                    break;
                case ConditionType.Conversation:
                    isMet = (playerStats.maleContactCount >= 4);
                    break;
                case ConditionType.Happiness:
                    isMet = (playerStats.gpIncreaseTileCount >= 5);
                    break;
                case ConditionType.Unhappiness:
                    isMet = (playerStats.gpDecreaseTileCount >= 3);
                    break;
                case ConditionType.DiceOne:
                    isMet = (playerStats.diceOneCount >= 3);
                    break;
                case ConditionType.Rich:
                    isMet = (playerStats.gp >= 3000);
                    break;
                case ConditionType.Wasteful:
                    isMet = (playerStats.shopSpentTotal >= 4000);
                    break;
                case ConditionType.Popularity:
                    isMet = (playerStats.friends >= 20);
                    break;
                case ConditionType.Steps:
                    isMet = (playerStats.totalSteps >= 80);
                    break;
                case ConditionType.Solitude:
                    isMet = (playerStats.soloPlayConsecutive >= 3);
                    break;
                case ConditionType.StatusAll2:
                    isMet = playerStats.IsAllStatsOver(2);
                    break;
            }

            if (isMet) return f;
        }
        return null;
    }
}