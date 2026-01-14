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
    public MenuManager menuManager;
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
    private bool hasRerolledThisTurn = false;
    
    // ★追加: アイテム管理と生徒手帳フラグ
    public ItemManager itemManager;
    public bool isHandbookActive = false;
    // 親友効果を持っているかチェック

    private int middleTileIndex = 24;

    // --- ★修正: GP獲得処理 (統合版) ---
    // 重複していた AddGP をここに統合しました
    public void AddGP(int amount)
    {
        // 1. ミレイの能力 (GP獲得1.5倍)
        if (HasFriendEffect(FriendEffectType.GPMultiplier))
        {
            amount = (int)(amount * 1.5f);
        }

        // 2. ステータス補正（ギャル力）
        int bonus = playerStats.GetGalGpBonus();
        int total = amount + bonus;
        
        playerStats.gp += total;

        // ログ出力
        Debug.Log($"GPを {total} (基本{amount} + ギャル{bonus}) 獲得しました。");
        if (phoneUI) phoneUI.UpdateStatusUI();
    }

    // --- ★修正: 友達獲得処理 (統合版) ---
    // AddFriend と AddFriends が混在していたのをここに統合
    public void AddFriends(int count)
    {
        // ステータス補正（コミュ力）
        int bonus = playerStats.GetCommuFriendBonus();
        int total = count + bonus;
        
        playerStats.friends += total;

        Debug.Log($"友達が {total}人 (基本{count} + コミュ{bonus}) 増えました。");
        if (phoneUI) phoneUI.UpdateStatusUI();
    }

    private void Start()
    {
        if (playerStats == null) playerStats = PlayerStats.Instance;
        isHandbookActive = false;
        // 1. 友達データの初期化
        InitializeFriends();
        hasRerolledThisTurn = false;
        // ★追加: 最初はダイス画像を消しておく
        if (diceImage != null) diceImage.gameObject.SetActive(false);

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

    // ★追加: アイテム「強制イベント」から呼ばれる
    public void TriggerEventTileFromItem()
    {
        Debug.Log("強制イベント発生！");
        HandleEventTile();
    }

    // ★追加: アイテム「生徒手帳」から呼ばれる
    public void ActivateHandbook()
    {
        isHandbookActive = true;
    }

    public void OnDiceClicked()
    {
        if (isMoving) return;
        StartCoroutine(RollDiceSequence());
    }
    IEnumerator RollDiceSequence()
    {
        int diceResult = Random.Range(1, 7);
        Debug.Log($"Dice: {diceResult}");

        // ... (カオルの振り直しロジックがあればここに維持) ...

        yield return new WaitForSeconds(0.5f);

        // ★修正点: 引数不足(CS7036)を解消
        if (menuManager != null)
        {
            bool isConfirmed = false;

            // ダイス画像を取得 (設定されていなければ null)
            Sprite resultSprite = (diceSprites != null && diceResult > 0 && diceResult <= diceSprites.Length)
                                ? diceSprites[diceResult - 1] : null;

            // 引数: (数字, 画像, OK時のアクション)
            if (diceSprite != null)
            {
                diceResultImage.gameObject.SetActive(true);
                diceResultImage.sprite = diceSprite;
                // テキストはシンプルに
                fullScreenDesc.text = "";
            }

            yield return new WaitUntil(() => isConfirmed);
        }

        yield return StartCoroutine(MovePlayer(diceResult));
    }
    public IEnumerator MovePlayer(int steps)
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
                int gVal = (currentGrade * 150) * mult;
                // AddGP内でギャル力ボーナスとミレイ効果(1.5倍)が適用される
                AddGP(gVal);
                playerStats.gpIncreaseTileCount++;
                break;

            case "GPMinus":
                // ★追加: ノアの能力 (減少→増加変換)
                if (HasFriendEffect(FriendEffectType.BadEventToGP))
                {
                    int bonus = 100 * mult;
                    AddGP(bonus);
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
                int fVal = (currentGrade * 1) * mult;
                AddFriends(fVal); // 修正: AddFriendsを使用
                break;

            case "FriendMinus":
                // ★追加: ノアの能力 (減少→増加変換)
                if (HasFriendEffect(FriendEffectType.BadEventToGP))
                {
                    int bonus = 100 * mult;
                    AddGP(bonus);
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

    // ---------------------------------------------------------
    // ★変更点2: 教室マスの処理 (確認フェーズの実装)
    // ---------------------------------------------------------
    void HandleClassroomTile(int tileIndex)
    {
        // 部屋とターゲット特定
        int roomIndex = tileIndex % floor2Rooms.Count;
        string roomName = floor2Rooms[roomIndex];
        var target = allFriends.FirstOrDefault(f => f.assignedRoom == roomName && !f.isRecruited);

        // 手帳の有無
        bool hasHandbook = (itemManager != null && itemManager.GetHandbookCount() > 0);

        if (menuManager != null)
        {
            // ★変更: 条件分岐なしでパネルを呼び出す
            menuManager.ShowClassroomPanel(hasHandbook,
                () => {
                    // [調査する] ボタンの処理
                    // ここで消費(-1)を実行
                    if (itemManager != null && itemManager.TryUseStudentHandbook())
                    {
                        if (phoneUI) phoneUI.AddLog("生徒手帳を消費して調査しました。");
                        HandleClassroomChallenge(target);
                    }
                    else
                    {
                        // 何らかのエラーで消費できなかった場合
                        EndTurn();
                    }
                },
                EndTurn // [閉じる/やめる] ボタンの処理 -> ターン終了
            );
        }
        else
        {
            Debug.LogError("MenuManager is not assigned!");
            EndTurn();
        }
    }


    // 調査実行
    void HandleClassroomChallenge(FriendData target)
    {
        // ターゲットが設定されている部屋なら成功
        if (target != null)
        {
            target.isRecruited = true;
            target.isHintRevealed = true;
            eventManager.ShowMessage($"【成功】\n教室で {target.friendName} を見つけました！\n親友になりました。", EndTurn);
        }
        else
        {
            if (phoneUI) phoneUI.AddLog("教室には誰もいなかったようだ……。");
            EndTurn();
        }
    }
    void HandleEventTile()
    {
        // ★修正: ドット抜け修正 (playerStats.MaleFriendCount)
        bool hasBoys = playerStats.MaleFriendCount > 0;

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
        float baseAffection = 20f + (playerStats.GetEffectiveLemonLv() * 5); // 修正: GetEffectiveLemonLv使用推奨だが、元の記述があればそれでも可
        string result = boyfriendManager.IncreaseAffection(baseAffection);
        AddLog($"お出かけしました。\n{result}");

        // マキの能力（モブ親友昇格）
        if (HasFriendEffect(FriendEffectType.MobFriendPromote))
        {
            AddLog("マキの能力: 確実に親友(モブ)へ昇格させます！");
            AddFriends(1); // 修正: AddFriends
        }
        else
        {
            // 通常確率(例:10%)
            if (Random.Range(0, 100) < 10)
            {
                AddLog("なんと！ 友達が親友(モブ)に昇格しました！");
                AddFriends(1); // 修正: AddFriends
            }
        }

        yield return new WaitForSeconds(1.0f);
        EndTurn();
    }

    // 一人で遊ぶ時の判定（親友 -> ランダム）
    void CheckSoloEvent()
    {
        // 1. 特殊条件親友の出現チェック
        FriendData newFriend = CheckSpecialConditionFriend();
        if (newFriend != null)
        {
            RecruitFriend(newFriend);
            EndTurn();
            return;
        }

        // 2. 該当者がいなければルーレット演出へ
        bool hasProtection = HasFriendEffect(FriendEffectType.NullifyGPMinus) || HasFriendEffect(FriendEffectType.BadEventToGP);

        // ルーレット実行
        StartCoroutine(eventManager.PlayRouletteSequence(playerStats, currentGrade, hasProtection, EndTurn));
    }

    // ※PlayRouletteSequence は EventManager に移動されている前提のため、ここでは削除するかコメントアウト
    // もしGameManager内に残す必要があるなら元のコードを復活させてください

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
                AddFriends(2); // 修正: AddFriends
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
        Debug.Log("中間地点に到達！ ボーナスを選択してください。");

        // UIパネルを表示
        eventManager.ShowOptions(
            "中間地点ボーナス",
            "ここまでのご褒美！ 好きなものを選んでね。",

            // 選択肢1: GPゲット
            "GP +1000",

            // 選択肢2: 親密度アップ（全員）
            "男子全員の親密度 +40",

            // 選択肢3: 友達ゲット
            "友達 +10人",

            // Action 1
            () =>
            {
                AddGP(1000);
                Debug.Log("GP +1000");
                EndTurn();
            },

            // Action 2
            () =>
            {
                if (boyfriendManager != null)
                {
                    // -1指定で全員に +40
                    string msg = boyfriendManager.IncreaseAffection(40f, -1);
                    Debug.Log(msg);
                }
                EndTurn();
            },

            // Action 3
            () =>
            {
                AddFriends(10); // 修正: AddFriends
                Debug.Log("友達 +10人");
                EndTurn();
            }
        );
    }

    // --- ターン終了・補助 ---

    // 【修正】ターン終了処理（完全版）
    public void EndTurn() // public にしておくと外部からも呼びやすい
    {
        // 既存の処理をいきなり行わず、まず親友出現チェックを開始
        StartCoroutine(EndTurnSequence());
    }

    // 2. 【追加】親友出現チェックの流れ（複数人対応）
    IEnumerator EndTurnSequence()
    {
        // まだ仲間になっていない親友を順番にチェック
        var potentialFriends = allFriends.Where(f => !f.isRecruited).ToList();

        foreach (var f in potentialFriends)
        {
            bool isMet = false;

            // --- 条件判定ロジック ---
            switch (f.assignedCondition)
            {
                case ConditionType.Happiness:
                    if (playerStats.happiness >= 50) isMet = true; 
                    break;
                case ConditionType.Rich:
                    if (playerStats.gp >= 5000) isMet = true;
                    break;
                case ConditionType.Popularity:
                    if (playerStats.friends >= 100) isMet = true;
                    break;
                case ConditionType.Steps:
                    // 例: 80歩以上
                    if (playerStats.totalSteps >= 80) isMet = true;
                    break;
                case ConditionType.Conversation:
                    if (playerStats.maleContactCount >= 4) isMet = true;
                    break;
                case ConditionType.Unhappiness:
                    if (playerStats.gpDecreaseTileCount >= 3) isMet = true;
                    break;
                case ConditionType.DiceOne:
                    if (playerStats.diceOneCount >= 3) isMet = true;
                    break;
                case ConditionType.Wasteful:
                    if (playerStats.shopSpentTotal >= 4000) isMet = true;
                    break;
                case ConditionType.Solitude:
                    if (playerStats.soloPlayConsecutive >= 3) isMet = true;
                    break;
                case ConditionType.StatusAll2:
                    if (playerStats.IsAllStatsOver(2)) isMet = true;
                    break;
                case ConditionType.Ai_Fixed:
                    if (playerStats.maleContactCount >= 8 || playerStats.gp >= 5000) isMet = true;
                    break;
            }

            // --- 条件達成時の処理 ---
            if (isMet)
            {
                bool processed = false;
                f.isRecruited = true;
                f.isHintRevealed = true; // 出会ったのでヒントも公開

                // 登場ダイアログを表示
                eventManager.ShowChoicePanel(
                    $"【親友登場！】\n{f.friendName} が現れた！\n(条件: {f.assignedCondition} 達成)",
                    new string[] { "仲間にする" },
                    new UnityEngine.Events.UnityAction[] { () => processed = true }
                );

                // プレイヤーがボタンを押すまで待機
                yield return new WaitUntil(() => processed);
            }
        }

        // 全員のチェックが終わったら、元の終了処理へ
        FinalizeTurn();
    }

    // 3. 【移動】元々のEndTurnの中身
    void FinalizeTurn()
    {
        Debug.Log("ターン終了処理実行");

        // 1. 給料計算
        int shinyu = 0;
        if (allFriends != null)
        {
            shinyu = allFriends.Count(f => f != null && f.isRecruited);
        }
        // 給料獲得時もステータス補正を乗せたい場合は AddGP を使う
        AddGP(playerStats.CalculateSalary(shinyu));

        // 2. レナの能力 (友達+1)
        if (HasFriendEffect(FriendEffectType.AutoFriend))
        {
            AddFriends(1);
        }

        // 3. 彼氏の能力発動
        if (boyfriendManager != null)
        {
            // 名前を修正: ActivateTurnEndAbilities
            string bfLog = boyfriendManager.ActivateTurnEndAbilities();
            if (!string.IsNullOrEmpty(bfLog)) Debug.Log(bfLog);
        }

        // 4. リカの能力 (カード生成)
        if (HasFriendEffect(FriendEffectType.CardGeneration) && playerStats.currentTurn % 12 == 0)
        {
            if (playerStats.moveCards.Count < 5)
            {
                playerStats.moveCards.Add(Random.Range(1, 7));
                Debug.Log("リカの能力: 定期便で移動カードが届きました！");
            }
        }

        // 5. 日付更新
        playerStats.currentTurn++;
        playerStats.currentMonth++;
        hasRerolledThisTurn = false; // ★修正: ここでフラグをリセット！
        if (playerStats.currentMonth > 12) playerStats.currentMonth -= 12;

        // 6. UI更新と操作許可
        if (diceButton != null) diceButton.interactable = true;
        if (phoneUI) phoneUI.UpdateStatusUI();
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
    
    // ★重複していた AddGP と AddFriend は上部に統合・削除しました。
    void AddLog(string msg) { Debug.Log(msg); if (phoneUI) phoneUI.AddLog(msg); }

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
        return allFriends != null && allFriends.Any(f => f != null && f.isRecruited && f.effectType == t);
    }

    void InitializeFriends()
    {
        Debug.Log("--- 友達データの読み込みと初期化を開始 ---");

        // 1. Resources/Friends フォルダから全データをロード
        FriendData[] loadedData = Resources.LoadAll<FriendData>("Friends");

        if (loadedData != null && loadedData.Length > 0)
        {
            allFriends = loadedData.ToList();
        }
        else
        {
            Debug.LogError("【Error】Resources/Friends フォルダにデータが見つかりません！");
            return;
        }

        // 2. リスト内の null（空データ）を削除
        allFriends.RemoveAll(f => f == null);

        // 3. パラメータのリセット
        foreach (var f in allFriends)
        {
            f.isRecruited = false;
            f.assignedCondition = ConditionType.None;
            f.assignedRoom = "";
            f.isHintRevealed = false;
        }

        // 4. 「アイ」役を決定
        var ai = allFriends.FirstOrDefault(f =>
            f.isAi || (f.friendName != null && f.friendName.Contains("アイ"))
        );

        if (ai != null)
        {
            ai.assignedCondition = ConditionType.Ai_Fixed;
        }
        else
        {
            if (allFriends.Count > 0)
            {
                allFriends[0].assignedCondition = ConditionType.Ai_Fixed;
                string tempName = allFriends[0].friendName ?? "名前なし";
                Debug.LogWarning($"アイ役が見つからないため、'{tempName}' をアイ条件に設定しました。");
            }
        }

        // 5. アイ以外のメンバーをシャッフル
        var others = allFriends
            .Where(f => f.assignedCondition == ConditionType.None)
            .OrderBy(x => Random.value)
            .ToList();

        // 6. 前半5名を「教室」に配置
        if (floor2Rooms == null || floor2Rooms.Count == 0)
            floor2Rooms = new List<string> { "2-A", "2-B", "2-C", "3-A", "3-B" };

        for (int i = 0; i < 5; i++)
        {
            if (i < others.Count)
            {
                others[i].assignedCondition = ConditionType.Classroom;
                others[i].assignedRoom = floor2Rooms[i % floor2Rooms.Count];
            }
        }

        // 7. 残りのメンバーを「ランダムな特殊条件」に割り当て
        List<ConditionType> randomConditions = new List<ConditionType>
        {
            ConditionType.Conversation, ConditionType.Happiness, ConditionType.Unhappiness,
            ConditionType.DiceOne, ConditionType.Rich, ConditionType.Wasteful,
            ConditionType.Popularity, ConditionType.Steps, ConditionType.Solitude,
            ConditionType.StatusAll2
        };
        randomConditions = randomConditions.OrderBy(x => Random.value).ToList();

        var remaining = others.Where(f => f.assignedCondition == ConditionType.None).ToList();

        for (int i = 0; i < remaining.Count; i++)
        {
            if (i < randomConditions.Count)
                remaining[i].assignedCondition = randomConditions[i];
            else
                remaining[i].assignedCondition = ConditionType.Conversation; 
        }

        // ログ出力
        Debug.Log("=========================================");
        Debug.Log($"【親友出現条件一覧】(全 {allFriends.Count} 名)");
        foreach (var f in allFriends)
        {
            string dName = string.IsNullOrEmpty(f.friendName) ? "名前未設定" : f.friendName;
            string hint = f.GetHintText();
            Debug.Log($"[{dName}] 条件タイプ: {f.assignedCondition}\n詳細: {hint}");
        }
        Debug.Log("=========================================");
    }

    // 親友出現条件の判定
    FriendData CheckSpecialConditionFriend()
    {
        if (allFriends == null) return null;

        foreach (var f in allFriends)
        {
            if (f == null) continue;
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