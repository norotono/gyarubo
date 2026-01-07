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
    private bool hasRerolledThisTurn = false;
    // ★追加: アイテム管理と生徒手帳フラグ
    public ItemManager itemManager;
    public bool isHandbookActive = false;
    // 親友効果を持っているかチェック

    private int middleTileIndex = 24;


    // ★追加: GP獲得処理 (ギャル力ボーナス適用)
    // 既存の HandleBlueTile などで gp += ... としている部分をこれに置き換えてください
    public void AddGP(int amount)
    {
        int bonus = playerStats.GetGalGpBonus();
        int total = amount + bonus;
        playerStats.gp += total;

        // ログ出力例 (AddLogなどがある場合)
        Debug.Log($"GPを {total} (基本{amount} + ギャル{bonus}) 獲得しました。");
        if (phoneUI) phoneUI.UpdateStatusUI(); // UI更新があれば呼ぶ
    }

    // ★追加: 友達獲得処理 (コミュ力ボーナス適用)
    public void AddFriends(int count)
    {
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
        hasRerolledThisTurn = false; // ★この行を追加
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
        // ... (サイコロアニメーション開始などはそのまま) ...

        // 1回目のロール
        int diceResult = Random.Range(1, 7);
        // サイコロの目画像の更新処理があればここに記述
        // UpdateDiceUI(diceResult); 

        Debug.Log($"ダイス結果: {diceResult}");

        yield return new WaitForSeconds(0.5f);

        // ★追加: カオル(DiceReroll)の効果チェック
        // カオルが仲間で、まだリロールしておらず、かつダイスUIが表示されている場合
        if (HasFriendEffect(FriendEffectType.DiceReroll) && !hasRerolledThisTurn)
        {
            bool deciding = true;
            bool doReroll = false;

            // 選択肢を表示
            eventManager.ShowOptions(
                "カオルの能力",
                $"今の出目は【{diceResult}】だよ。\n振り直す？",
                "振り直す！",
                "このまま進む",
                null, // 3つ目はなし
                () => { doReroll = true; deciding = false; },   // Yes
                () => { doReroll = false; deciding = false; },  // No
                null
            );

            // プレイヤーの選択を待つ
            yield return new WaitUntil(() => !deciding);

            if (doReroll)
            {
                hasRerolledThisTurn = true;
                Debug.Log("カオルの能力で振り直します！");

                // もう一度ロール演出などを入れても良いですが、ここでは即座に値を更新
                diceResult = Random.Range(1, 7);
                Debug.Log($"再ロール結果: {diceResult}");

                // 結果表示の更新（必要なら）
                // UpdateDiceUI(diceResult);

                yield return new WaitForSeconds(0.5f);
            }
        }

        // ここから移動処理（既存のコードへ）
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

                AddGP(gVal);
                playerStats.gpIncreaseTileCount++;
                break;

            case "GPMinus":
                // ★追加: ノアの能力 (減少→増加変換)
                if (HasFriendEffect(FriendEffectType.BadEventToGP))
                {
                    int bonus = 100 * mult;
                    if (HasFriendEffect(FriendEffectType.GPMultiplier)) bonus = (int)(bonus * 1.5f);
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
                int fVal = (currentGrade * 1 + playerStats.commuLv) * mult;
                AddFriend(fVal);
                break;

            case "FriendMinus":
                // ★追加: ノアの能力 (減少→増加変換)
                if (HasFriendEffect(FriendEffectType.BadEventToGP))
                {
                    int bonus = 100 * mult;
                    if (HasFriendEffect(FriendEffectType.GPMultiplier)) bonus = (int)(bonus * 1.5f);
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
    // GameManager.cs クラス内に以下のメソッドを追加してください



    // ★追加: 教室での親友チャレンジ処理
    void HandleClassroomChallenge(int tileIndex)
    {
        Debug.Log($"教室(Tile:{tileIndex})で親友を探します。");

        // 1. この教室に割り当てられている親友を探す
        // (assignedRoom の判定は文字列比較など実装に合わせて調整してください。ここでは簡易的に探します)

        // とりあえずランダムな確率で発見する処理（仮実装）
        // ※本来は tileIndex に対応する部屋名 ("2-A"など) と f.assignedRoom を比較します
        bool isSuccess = (Random.value < 0.5f); // 50%で発見

        if (isSuccess)
        {
            // まだ仲間にしていない、かつ教室条件の親友をランダムに1人選ぶ
            var target = allFriends.FirstOrDefault(f =>
                !f.isRecruited && f.assignedCondition == ConditionType.Classroom);

            if (target != null)
            {
                target.isRecruited = true;
                target.isHintRevealed = true;
                eventManager.ShowMessage($"【成功】\n教室で {target.friendName} を見つけました！\n仲間に誘いました。", EndTurn);
            }
            else
            {
                eventManager.ShowMessage("教室に誰かいる気配がしましたが、\n既に全員仲間のようです。", EndTurn);
            }
        }
        else
        {
            eventManager.ShowMessage("教室には誰もいないようです……。", EndTurn);
        }
    }

    // 【修正】教室マスの処理（選択肢を2つに限定）
    void HandleClassroomTile(int index)
    {
        // 生徒手帳が有効なら選択肢を出す
        if (isHandbookActive)
        {
            eventManager.ShowOptions(
                "生徒手帳の効果",
                "教室の様子が分かります。\nどうしますか？",

                // ボタン1
                "親友を探す",
                // ボタン2
                "何もしない",
                // ボタン3 (なし)
                null,

                // ボタン1の処理: チャレンジ実行
                () => HandleClassroomChallenge(index),

                // ボタン2の処理: 何もしないでターン終了
                EndTurn,

                // ボタン3の処理: なし
                null
            );
        }
        else
        {
            // 生徒手帳がない通常時は、強制的にチャレンジ（またはランダムイベント）へ
            HandleClassroomChallenge(index);
        }
    }

    void HandleEventTile()
    {
        // 男友達（彼氏含む）がいるか確認
        bool hasBoys = playerStatsMaleFriendCount > 0;

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
    // 【修正】GameManager.cs の CheckSoloEvent メソッド
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

        // ★エラー修正: PlayRouletteSequence に名前を変更し、引数を合わせる
        StartCoroutine(eventManager.PlayRouletteSequence(playerStats, currentGrade, hasProtection, EndTurn));
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
        Debug.Log("中間地点に到達！ ボーナスを選択してください。");

        // UIパネルを表示
        eventManager.ShowOptions(
            "中間地点ボーナス",
            "ここまでのご褒美！ 好きなものを選んでね。",

            // 選択肢1: GPゲット
            "GP +1000",

            // 選択肢2: 親密度アップ（全員）
            "男子全員の親密度 +40",

            // 選択肢3: 友達ゲット（モブ昇格廃止 -> 友達+10）
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
                AddFriends(10);
                Debug.Log("友達 +10人");
                EndTurn();
            }
        );
    }

    // 【修正】古い呼び出し方になっていた部分を修正
    IEnumerator RunRoulette()
    {
        bool protection = HasFriendEffect(FriendEffectType.BadEventToGP);

        // エラー原因: 引数が足りていませんでした。
        // 修正: (stats, grade, protection, callback) の4つを渡すように変更
        yield return StartCoroutine(eventManager.PlayRouletteSequence(playerStats, currentGrade, protection, null));

        EndTurn();
    }

    // --- ターン終了・補助 ---

    // 【修正】ターン終了処理（完全版）
    void EndTurn()
    {
        // 既存の処理をいきなり行わず、まず親友出現チェックを開始
        StartCoroutine(EndTurnSequence());
    }

    // 2. 【追加】親友出現チェックの流れ（複数人対応）
    IEnumerator EndTurnSequence()
    {
        // まだ仲間になっていない親友を順番にチェック
        // ※コレクション変更エラーを防ぐため ToList() でコピーして回す
        var potentialFriends = allFriends.Where(f => !f.isRecruited).ToList();

        foreach (var f in potentialFriends)
        {
            bool isMet = false;

            // --- 条件判定ロジック (簡易実装) ---
            switch (f.assignedCondition)
            {
                case ConditionType.Happiness:
                    if (playerStats.happiness >= 50) isMet = true; // 数値は調整してください
                    break;
                case ConditionType.Rich:
                    if (playerStats.gp >= 5000) isMet = true;
                    break;
                case ConditionType.Popularity:
                    if (playerStats.friends >= 100) isMet = true;
                    break;
                case ConditionType.Steps:
                    // 例: 10ターン経過など
                    if (playerStats.currentTurn >= 10) isMet = true;
                    break;
                case ConditionType.Conversation:
                    // 会話条件はイベントマスなどでフラグ管理が必要ですが、一旦仮で
                    break;
                    // 必要に応じて他のcaseを追加
            }

            // --- 条件達成時の処理 ---
            if (isMet)
            {
                bool processed = false;
                f.isRecruited = true;
                f.isHintRevealed = true; // 出会ったのでヒントも公開

                // 登場ダイアログを表示
                // (EventManagerのメソッド名はご自身の環境に合わせてください)
                eventManager.ShowChoicePanel(
                    $"【親友登場！】\n{f.friendName} が現れた！\n(条件: {f.assignedCondition} 達成)",
                    new string[] { "仲間にする" },
                    new UnityEngine.Events.UnityAction[] { () => processed = true }
                );

                // プレイヤーがボタンを押すまで待機 (ここが重要)
                yield return new WaitUntil(() => processed);
            }
        }

        // 全員のチェックが終わったら、元の終了処理へ
        FinalizeTurn();
    }

    // 3. 【移動】元々のEndTurnの中身 (名前をFinalizeTurnに変更)
    void FinalizeTurn()
    {
        Debug.Log("ターン終了処理実行");

        // --- 以下、頂いたコードそのまま ---

        // 1. 給料計算 (null安全化)
        int shinyu = 0;
        if (allFriends != null)
        {
            shinyu = allFriends.Count(f => f != null && f.isRecruited);
        }
        AddGP(playerStats.CalculateSalary(shinyu));

        // 2. レナの能力 (友達+1)
        if (HasFriendEffect(FriendEffectType.AutoFriend))
        {
            AddFriends(1); // AddFriendメソッドがなければ直接加算
        }

        // 3. 彼氏の能力発動 (null安全化)
        if (boyfriendManager != null)
        {
            string bfLog = boyfriendManager.ActivateBoyfriendEffects();
            // ログ出力メソッドが AddLog ならそのまま、なければ Debug.Log や ShowMessage に
            if (!string.IsNullOrEmpty(bfLog)) Debug.Log(bfLog);
        }

        // 4. リカの能力 (カード生成)
        if (HasFriendEffect(FriendEffectType.CardGeneration) && playerStats.currentTurn % 12 == 0)
        {
            // ※もしItemManagerを導入済みなら itemManager.movementCards を参照するように書き換えてください
            if (playerStats.moveCards.Count < 5)
            {
                playerStats.moveCards.Add(Random.Range(1, 7));
                Debug.Log("リカの能力: 定期便で移動カードが届きました！");
            }
        }

        // 5. 日付更新
        playerStats.currentTurn++;
        playerStats.currentMonth++;
        if (playerStats.currentMonth > 12) playerStats.currentMonth -= 12;

        // 6. UI更新と操作許可
        // UpdateMainUI(); // メソッドがあれば
        // isMoving = false; // フラグがあれば

        if (diceButton != null) diceButton.interactable = true;

        // 最後に次のターンの準備（必要なら）
        // StartTurn(); 
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
    // GameManager.cs の InitializeFriends メソッドを以下と差し替えてください

    // 【変更】親友データの初期化と条件のランダム割り当て（自動補正版）
    // 【変更】親友データの初期化（自動ロード機能付き）
    // 【変更】フォルダから一括読み込みして初期化（シンプル版）
    // 【修正】フォルダから読み込み + 名前未設定対策済み
    // 【修正完了版】データ自動ロード + 名前未設定対策 + 条件一覧ログ出力
    void InitializeFriends()
    {
        Debug.Log("--- 友達データの読み込みと初期化を開始 ---");

        // 1. Resources/Friends フォルダから全データをロード
        FriendData[] loadedData = Resources.LoadAll<FriendData>("Friends");

        if (loadedData != null && loadedData.Length > 0)
        {
            // リストを上書き
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

        // 4. 「アイ」役を決定（名前未設定でもエラーにならないよう対策済み）
        // ★修正ポイント: f.friendName != null のチェックを追加
        var ai = allFriends.FirstOrDefault(f =>
            f.isAi || (f.friendName != null && f.friendName.Contains("アイ"))
        );

        if (ai != null)
        {
            ai.assignedCondition = ConditionType.Ai_Fixed;
        }
        else
        {
            // アイが見つからない場合、リストの先頭をアイ役にする
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
                remaining[i].assignedCondition = ConditionType.Conversation; // 条件不足時は「会話」
        }


        // ★★★ 追加: 全員の名前と出現条件をログに出力 ★★★
        Debug.Log("=========================================");
        Debug.Log($"【親友出現条件一覧】(全 {allFriends.Count} 名)");
        Debug.Log("=========================================");

        foreach (var f in allFriends)
        {
            // 名前がnullの場合は "名前未設定" と表示
            string dName = string.IsNullOrEmpty(f.friendName) ? "名前未設定" : f.friendName;

            // 条件の詳細テキストを取得（ヒントテキストを活用）
            // ※FriendData.GetHintText() は内部で friendName を使うため、名前未設定だと少し変な表示になる可能性がありますがエラーにはなりません
            string hint = f.GetHintText();

            // ログを見やすく整形
            // 例: [ミレイ] Happiness : ミレイは【GP増幅マスに5回止まる】と...
            Debug.Log($"[{dName}] 条件タイプ: {f.assignedCondition}\n詳細: {hint}");
        }
        Debug.Log("=========================================");
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