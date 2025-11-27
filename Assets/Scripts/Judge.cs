using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic; // Dictionaryを使うために追加

public class Judge : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI[] MessageObj;
    [SerializeField] private NotesManager notesManager;
    [SerializeField] private Slider slider;
    [SerializeField] private lightsScript[] laneLights;
    [SerializeField] private NEMSYSControllerInput nemsysController;
    [SerializeField] private MusicManager musicManager;

    // ★★★ 追加: LongNotesManagerの参照 ★★★
    [SerializeField] private LongNotesManager longNotesManager;

    int[] judgecnt = { 0, 0, 0, 0 };
    int score = 0;
    float displayScore = 0f;
    int targetScore = 0;
    int scorestandard;
    bool remainderFlug;
    int remainder;

    [SerializeField] private float scoreAnimationDuration = 0.3f;
    [SerializeField] private AudioSource judgeSoundSource;
    [SerializeField] private AudioClip perfectClip;
    [SerializeField] private AudioClip okClip;
    [SerializeField] private RectTransform percentTextRect;
    [SerializeField] private float gaugeTextOffset = 10f;

    public GameObject[] allLanes = new GameObject[8];

    float endTime = 0f;
    int laneposition;
    bool IsGameEnded = false;

    // 判定されたノーツの総数 (通常ノーツのPerfect/OK/Miss + ロングノーツの開始/終了のPerfect/OK/Miss)
    private int judgedNotesCount = 0;

    // ★★★ 追加: ロングノーツの状態追跡 ★★★
    // Key: レーン番号, Value: 押されているLongNoteオブジェクト
    private Dictionary<int, GameObject> activeLongNotes = new Dictionary<int, GameObject>();

    // 内部用の判定タイプ定義
    enum JudgementType { Start, Release }

    void Start()
    {
        slider.maxValue = 100f;
        slider.value = 0f;
        laneposition = 2;
        judgedNotesCount = 0;
        activeLongNotes.Clear();

        activateLane();
    }

    void Update()
    {
        UpdateScoreDisplay();
        UpdateGaugeTextPosition();

        // ノーツがすべて判定された場合の終了処理
        CheckGameEnd();

        if (IsGameEnded) return;

        bool usesController = nemsysController != null && nemsysController.IsInitialized;

        // ★★★ 押し始め・通常ノーツのタップ判定 (GetButtonDown) ★★★
        if ((usesController && nemsysController.GetButtonDown(0)) || Input.GetKeyDown(KeyCode.S))
        {
            CheckNoteHit(laneposition);
        }
        if ((usesController && nemsysController.GetButtonDown(1)) || Input.GetKeyDown(KeyCode.F))
        {
            CheckNoteHit(laneposition + 1);
        }
        if ((usesController && nemsysController.GetButtonDown(2)) || Input.GetKeyDown(KeyCode.J))
        {
            CheckNoteHit(laneposition + 2);
        }
        if ((usesController && nemsysController.GetButtonDown(3)) || Input.GetKeyDown(KeyCode.L))
        {
            CheckNoteHit(laneposition + 3);
        }

        // ★★★ ロングノーツの押し終わり判定 (GetButtonUp) ★★★
        CheckLongNoteReleaseInput(0, KeyCode.S, usesController, nemsysController);
        CheckLongNoteReleaseInput(1, KeyCode.F, usesController, nemsysController);
        CheckLongNoteReleaseInput(2, KeyCode.J, usesController, nemsysController);
        CheckLongNoteReleaseInput(3, KeyCode.L, usesController, nemsysController);


        if (laneposition > 0 && ((usesController && nemsysController.GetButtonDown(4)) || Input.GetKeyDown(KeyCode.E)))
        {
            laneposition--;
            activateLane();
        }

        if (laneposition < 4 && ((usesController && nemsysController.GetButtonDown(5)) || Input.GetKeyDown(KeyCode.I)))
        {
            laneposition++;
            activateLane();
        }

        // Miss判定
        HandleNormalNoteMiss();
        HandleLongNoteHoldMiss();
    }

    // ★★★ 新規メソッド: ロングノーツの押し終わり入力チェック ★★★
    void CheckLongNoteReleaseInput(int buttonIndex, KeyCode keyCode, bool usesController, NEMSYSControllerInput controller)
    {
        int lane = laneposition + buttonIndex;

        if (activeLongNotes.ContainsKey(lane))
        {
            bool released = (usesController && controller.GetButtonUp(buttonIndex)) || Input.GetKeyUp(keyCode);

            if (released)
            {
                CheckLongNoteRelease(lane);
            }
        }
    }

    // ★★★ 修正: 通常ノーツのMiss判定をメソッドに切り出し ★★★
    void HandleNormalNoteMiss()
    {
        if (notesManager.NotesTime.Count > 0)
        {
            float noteIdealTime = notesManager.NotesTime[0] + musicManager.MusicStartTime;

            if (Time.time > noteIdealTime + 0.10f)
            {
                message(2); // Miss
                deleteData(0); // 通常ノーツを削除
                judgedNotesCount++;
                Debug.Log($"Miss (自動削除) - 判定済み通常ノーツ: {judgedNotesCount}");
                slider.value -= 1.0f;
            }
        }
    }

    // ★★★ 新規メソッド: ロングノーツのホールド中及び終了後のMiss判定/自動削除 ★★★
    void HandleLongNoteHoldMiss()
    {
        if (longNotesManager == null) return;

        // 1. 自動Miss（押し始めを逃した場合）
        if (longNotesManager.LongNotesObj.Count > 0)
        {
            // NotesManagerと同様に、常にリストの先頭を次のノーツとしてチェック
            GameObject firstLongNoteObj = longNotesManager.LongNotesObj[0];
            LongNote firstLongNote = firstLongNoteObj.GetComponent<LongNote>();

            // まだホールドされていないロングノーツの判定時間を過ぎた場合
            if (firstLongNote != null && !activeLongNotes.ContainsValue(firstLongNoteObj))
            {
                float noteIdealTime = firstLongNote.startTargetTime + musicManager.MusicStartTime;

                // 押し始めの受付期間を過ぎた場合 (通常ノーツと同じ0.10fを超過)
                if (Time.time > noteIdealTime + 0.10f)
                {
                    message(2); // Miss
                    DeleteLongNoteData(firstLongNoteObj, 0); // ロングノーツを削除
                    judgedNotesCount++; // 押し始めのMissとしてカウント
                    Debug.Log($"Long Note Start Miss (自動削除) - 判定済み: {judgedNotesCount}");
                    slider.value -= 1.0f;
                }
            }
        }

        // 2. 終点時間チェック（押しっぱなしが長すぎた場合 -> 自動Perfect/OKで終了）
        List<int> lanesToRelease = new List<int>();
        foreach (var pair in activeLongNotes)
        {
            int lane = pair.Key;
            GameObject longNoteObj = pair.Value;
            LongNote longNote = longNoteObj.GetComponent<LongNote>();

            if (longNote == null)
            {
                lanesToRelease.Add(lane);
                continue;
            }

            float musicCurrentTime = Time.time - musicManager.MusicStartTime;
            float endTargetTime = longNote.endTargetTime;

            // 終点時間を過ぎた場合（0.10fの猶予期間込み）
            if (musicCurrentTime > endTargetTime + 0.10f)
            {
                // Perfect/OK判定として処理し、ホールド終了
                // timeLag=0.0fとしてPerfect相当で処理
                LongNoteJudgement(longNoteObj, JudgementType.Release, 0.0f, lane);
                lanesToRelease.Add(lane);
            }
        }

        foreach (int lane in lanesToRelease)
        {
            activeLongNotes.Remove(lane);
        }
    }


    // ゲーム終了チェック（すべてのノーツが判定されたか）
    void CheckGameEnd()
    {
        if (IsGameEnded) return;

        // ノーツ総数を計算 (通常ノーツ + ロングノーツの開始/終了の2回)
        int totalNotesCount = (notesManager != null ? notesManager.noteNum : 0) +
                              (longNotesManager != null ? longNotesManager.longNoteCount * 2 : 0);

        // すべてのノーツが判定され、かつ少し時間が経過した場合
        if (musicManager.IsPlaying && judgedNotesCount >= totalNotesCount && Time.time > endTime + musicManager.MusicStartTime + 1f)
        {
            // クリア判定
            if (slider.value < 70.0f)
            {
                MessageObj[6].text = "FAILED...";
            }
            else
            {
                MessageObj[6].text = "CLEAR";

                // フルコンボ判定（Missが0の場合）
                if (judgecnt[2] == 0)
                {
                    MessageObj[6].text = "FULL COMBO";

                    // オールパーフェクト判定
                    if (judgecnt[0] == totalNotesCount)
                    {
                        MessageObj[6].text = "ALL PERFECT";
                    }
                }
            }

            OnGameEnd();
            IsGameEnded = true;
            Invoke("ResultScene", 3f);
        }
    }

    // ★★★ 修正: CheckNoteHit (ロングノーツの押し始めを判定に追加) ★★★
    void CheckNoteHit(int lane)
    {
        // 既にそのレーンでロングノーツをホールド中の場合は、空打ちとして扱う
        if (activeLongNotes.ContainsKey(lane))
        {
            Debug.Log($"レーン{lane}: ロングノーツホールド中の誤入力");
            TriggerLaneLight(lane, 2);
            return;
        }

        // 1. 通常ノーツ (type:1) の判定 (NotesManagerにtype:1のみが格納されている前提)
        for (int i = 0; i < notesManager.LaneNum.Count; i++)
        {
            if (notesManager.LaneNum[i] == lane)
            {
                float noteIdealTime = notesManager.NotesTime[i] + musicManager.MusicStartTime;
                float timeLag = GetABS(Time.time - noteIdealTime);

                if (timeLag <= 0.10f)
                {
                    Judgement(timeLag, i, lane); // 通常ノーツ判定
                    return;
                }
                else if (Time.time < noteIdealTime)
                {
                    Debug.Log($"レーン{lane}: 早すぎ（通常ノーツ）");
                    TriggerLaneLight(lane, 2);
                    return;
                }
            }
        }

        // 2. ロングノーツ (type:2) の押し始め判定
        if (longNotesManager != null && longNotesManager.LongNotesObj.Count > 0)
        {
            // NotesManagerと同様に、先頭ノーツが次に判定されるべきノーツ
            GameObject firstLongNoteObj = longNotesManager.LongNotesObj[0];
            LongNote firstLongNote = firstLongNoteObj.GetComponent<LongNote>();

            // 先頭のロングノーツが該当レーンのものであれば判定
            if (firstLongNote != null && firstLongNote.lane == lane)
            {
                CheckLongNoteHit(firstLongNoteObj, lane); // ロングノーツの押し始め判定
                return;
            }
        }

        Debug.Log($"レーン{lane}: 空打ち");
        TriggerLaneLight(lane, 2);
    }

    // ★★★ 新規メソッド: ロングノーツの押し始め判定 (Start) ★★★
    void CheckLongNoteHit(GameObject longNoteObj, int lane)
    {
        LongNote longNote = longNoteObj.GetComponent<LongNote>();
        if (longNote == null) return;

        float noteIdealTime = longNote.startTargetTime + musicManager.MusicStartTime;
        float timeLag = GetABS(Time.time - noteIdealTime);

        if (timeLag <= 0.10f)
        {
            // Perfect/OK判定
            LongNoteJudgement(longNoteObj, JudgementType.Start, timeLag, lane);

            // 判定成功: LongNotesManagerのリストからノーツを削除し、ホールド状態に移行
            // LongNotesManagerのリストから削除する代わりに、アクティブリストに追加する
            activeLongNotes.Add(lane, longNoteObj);

            // NotesManagerと異なり、LongNotesManagerのリストからは、ここでは**削除しない**。
            // 削除は終点判定またはMiss時まで保持
        }
        else if (Time.time < noteIdealTime)
        {
            Debug.Log($"レーン{lane}: 早すぎ（ロングノーツ）");
            TriggerLaneLight(lane, 2);
        }
    }

    // ★★★ 新規メソッド: ロングノーツの押し終わり判定 (Release) ★★★
    void CheckLongNoteRelease(int lane)
    {
        if (!activeLongNotes.TryGetValue(lane, out GameObject longNoteObj))
        {
            return;
        }

        LongNote longNote = longNoteObj.GetComponent<LongNote>();
        if (longNote == null)
        {
            activeLongNotes.Remove(lane);
            return;
        }

        float noteIdealTime = longNote.endTargetTime + musicManager.MusicStartTime;
        float timeLag = GetABS(Time.time - noteIdealTime);

        // 終点判定の許容範囲 (例: 0.10f)
        if (timeLag <= 0.10f)
        {
            // Perfect/OK判定
            LongNoteJudgement(longNoteObj, JudgementType.Release, timeLag, lane);
        }
        else
        {
            // Miss 判定 (早すぎる/遅すぎるリリース)
            Debug.Log($"Long Note Release Miss - TimeLag: {timeLag:F3}");
            message(2); // Miss
            slider.value -= 1.0f;
            TriggerLaneLight(lane, 2);
            judgedNotesCount++; // 押し終わりのMissとしてカウント
        }

        // 判定が成功/失敗にかかわらず、ホールド状態を解除し、ノーツを削除
        activeLongNotes.Remove(lane);
        DeleteLongNoteData(longNoteObj);
    }

    // ★★★ 新規メソッド: ロングノーツの判定処理 ★★★
    void LongNoteJudgement(GameObject longNoteObj, JudgementType type, float timeLag, int lane)
    {
        bool isPerfect = timeLag <= 0.045f;
        float scoreValue = 1.5f;

        if (isPerfect)
        {
            Debug.Log($"Long Note {(type == JudgementType.Start ? "Start" : "Release")} Perfect");
            message(0);
            addScore(0);
            slider.value += scoreValue;

            if (judgeSoundSource != null && perfectClip != null)
            {
                judgeSoundSource.PlayOneShot(perfectClip);
            }

            TriggerLaneLight(lane, 0);
        }
        else // OK判定
        {
            Debug.Log($"Long Note {(type == JudgementType.Start ? "Start" : "Release")} OK");
            message(1);
            addScore(1);
            slider.value += scoreValue;

            if (judgeSoundSource != null && okClip != null)
            {
                judgeSoundSource.PlayOneShot(okClip);
            }

            TriggerLaneLight(lane, 1);
        }

        judgedNotesCount++;
        Debug.Log($"Long Note {(type == JudgementType.Start ? "Start" : "Release")} - 判定済み: {judgedNotesCount}");
    }

    // 通常ノーツの判定処理
    void Judgement(float timeLag, int noteIndex, int lane)
    {
        if (timeLag <= 0.045f)
        {
            Debug.Log("Perfect");
            message(0);
            addScore(0);
            slider.value += 2.5f;

            if (judgeSoundSource != null && perfectClip != null)
            {
                judgeSoundSource.PlayOneShot(perfectClip);
            }

            TriggerLaneLight(lane, 0);
            deleteData(noteIndex);
            judgedNotesCount++;
            Debug.Log($"Perfect - 判定済み: {judgedNotesCount}");
        }
        else if (timeLag <= 0.10f)
        {
            Debug.Log("OK");
            message(1);
            addScore(1);
            slider.value += 2.5f;

            if (judgeSoundSource != null && okClip != null)
            {
                judgeSoundSource.PlayOneShot(okClip);
            }

            TriggerLaneLight(lane, 1);
            deleteData(noteIndex);
            judgedNotesCount++;
            Debug.Log($"OK - 判定済み: {judgedNotesCount}");
        }
    }

    void activateLane()
    {
        for (int i = 0; i < 8; i++)
        {
            if (laneposition <= i && i < laneposition + 4)
            {
                allLanes[i].SetActive(true);
            }
            else
            {
                allLanes[i].SetActive(false);
            }
        }
    }

    void TriggerLaneLight(int laneNum, int judgeType)
    {
        if (laneLights != null && laneNum >= 0 && laneNum < laneLights.Length && laneLights[laneNum] != null)
        {
            laneLights[laneNum].LightUp(judgeType);
        }
        else
        {
            Debug.LogWarning($"レーン{laneNum}のlightsScriptが設定されていません");
        }
    }

    float GetABS(float num)
    {
        return num >= 0 ? num : -num;
    }

    // 通常ノーツのデータ削除
    void deleteData(int index)
    {
        notesManager.NotesTime.RemoveAt(index);
        notesManager.LaneNum.RemoveAt(index);
        notesManager.NoteType.RemoveAt(index);

        if (index < notesManager.NotesObj.Count && notesManager.NotesObj[index] != null)
        {
            Destroy(notesManager.NotesObj[index]);
            notesManager.NotesObj.RemoveAt(index);
        }
    }

    // ★★★ 新規メソッド: ロングノーツのデータ削除 ★★★
    void DeleteLongNoteData(GameObject longNoteObj, int index = -1)
    {
        if (longNotesManager == null) return;

        // LongNotesManagerのリストからノーツを削除
        // 通常は先頭のノーツが判定されるため、index=0で削除を試みる
        int actualIndex = (index == -1) ? longNotesManager.LongNotesObj.IndexOf(longNoteObj) : index;

        if (actualIndex >= 0 && actualIndex < longNotesManager.LongNotesObj.Count && longNotesManager.LongNotesObj[actualIndex] == longNoteObj)
        {
            longNotesManager.LongNotesObj.RemoveAt(actualIndex);
        }

        Destroy(longNoteObj);
    }

    void message(int judge)
    {
        judgecnt[judge]++;

        if (judge == 2)
        {
            judgecnt[3] = 0;
        }
        else
        {
            judgecnt[3]++;
        }

        if (MessageObj != null && judge < MessageObj.Length && MessageObj[judge] != null)
        {
            MessageObj[judge].text = judgecnt[judge].ToString();
        }
        else
        {
            Debug.LogWarning($"MessageObj[{judge}]が設定されていません");
        }

        if (MessageObj.Length > 3 && MessageObj[3] != null)
        {
            MessageObj[3].text = judgecnt[3].ToString();
        }
    }

    void addScore(int judge)
    {
        if (judge == 0)
        {
            score += scorestandard;
            if (remainderFlug)
            {
                score += remainder;
                remainderFlug = false;
            }
        }
        else if (judge == 1)
        {
            // OK判定は Perfect の 3/4 のスコア
            score += scorestandard * 3 / 4;
        }

        targetScore = score;
    }

    void UpdateScoreDisplay()
    {
        if (displayScore < targetScore)
        {
            float difference = targetScore - displayScore;
            float increment = difference / scoreAnimationDuration * Time.deltaTime;

            if (increment < 1f)
            {
                increment = Mathf.Min(1f, difference);
            }

            displayScore += increment;

            if (displayScore > targetScore)
            {
                displayScore = targetScore;
            }

            if (MessageObj.Length > 4 && MessageObj[4] != null)
            {
                MessageObj[4].text = Mathf.FloorToInt(displayScore).ToString();
            }
        }
    }

    void UpdateGaugeTextPosition()
    {
        if (slider == null || percentTextRect == null)
            return;

        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        if (sliderRect == null)
            return;

        float normalizedValue = slider.value / slider.maxValue;
        float fillWidth = sliderRect.rect.width * normalizedValue;
        float sliderLeftX = sliderRect.localPosition.x - (sliderRect.rect.width / 2f);
        float newX = sliderLeftX + fillWidth - gaugeTextOffset;
        float currentY = percentTextRect.localPosition.y;

        percentTextRect.localPosition = new Vector3(newX, currentY, percentTextRect.localPosition.z);

        if (MessageObj.Length > 5 && MessageObj[5] != null)
        {
            MessageObj[5].text = slider.value.ToString("F1") + "%";
        }

        if (slider.value >= 70f)
        {
            MessageObj[5].color = Color.green;
        }
        else
        {
            MessageObj[5].color = Color.white;
        }
    }

    void OnGameEnd()
    {
        GameResultData.Score = score;
        GameResultData.PerfectCount = judgecnt[0];
        GameResultData.OkCount = judgecnt[1];
        GameResultData.MissCount = judgecnt[2];
        GameResultData.Combo = judgecnt[3];

        if (score < 700000)
        {
            GameResultData.ResultRank = "D";
        }
        else if (score < 800000)
        {
            GameResultData.ResultRank = "C";
        }
        else if (score < 900000)
        {
            GameResultData.ResultRank = "B";
        }
        else if (score < 950000)
        {
            GameResultData.ResultRank = "A";
        }
        else if (score < 980000)
        {
            GameResultData.ResultRank = "AA";
        }
        else if (score < 990000)
        {
            GameResultData.ResultRank = "AAA";
        }
        else
        {
            GameResultData.ResultRank = "S";
        }

        Debug.Log($"ゲーム終了 - スコア: {score}, ランク: {GameResultData.ResultRank}");
    }

    void ResultScene()
    {
        SceneManager.LoadScene("ResultScene");
    }

    // ★★★ 修正: InitGameData (ロングノーツの総数を含めてスコア計算) ★★★
    public void InitGameData()
    {
        // ロングノーツは「開始」と「終了」の2回判定されるため、総ノーツ数を (通常ノーツ数 + ロングノーツ数 * 2) で計算
        int totalNotes = (notesManager != null ? notesManager.noteNum : 0) +
                         (longNotesManager != null ? longNotesManager.longNoteCount * 2 : 0);

        if (totalNotes > 0)
        {
            scorestandard = 1000000 / totalNotes;
            remainderFlug = true;
            remainder = 1000000 % totalNotes;

            Debug.Log($"総判定回数: {totalNotes}, 1判定あたりのスコア: {scorestandard}");
        }
        else
        {
            Debug.LogError("ノーツマネージャが設定されていないか、ノーツ数が0です");
            scorestandard = 0;
            return;
        }

        // 終了時間の計算 (NotesManagerとLongNotesManagerの最終ノーツを比較)
        float normalNoteEndTime = (notesManager != null && notesManager.NotesTime.Count > 0) ? notesManager.NotesTime[notesManager.NotesTime.Count - 1] : 0f;

        float longNoteEndTime = 0f;
        if (longNotesManager != null && longNotesManager.LongNotesObj.Count > 0)
        {
            LongNote lastLongNote = longNotesManager.LongNotesObj[longNotesManager.LongNotesObj.Count - 1].GetComponent<LongNote>();
            if (lastLongNote != null)
            {
                // ロングノーツの終了時間を使用
                longNoteEndTime = lastLongNote.endTargetTime;
            }
        }

        endTime = Mathf.Max(normalNoteEndTime, longNoteEndTime);

        if (musicManager == null)
        {
            Debug.LogError("MusicManagerが設定されていません");
        }
        Debug.Log($"楽曲終了時間: {endTime}秒");
    }
}