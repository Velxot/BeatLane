using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Judge : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI[] MessageObj;
    [SerializeField] private NotesManager notesManager;
    [SerializeField] private Slider slider;
    [SerializeField] private lightsScript[] laneLights;
    [SerializeField] private NEMSYSControllerInput nemsysController;
    [SerializeField] private MusicManager musicManager;

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

    // 判定されたノーツの総数
    private int judgedNotesCount = 0;

    void Start()
    {
        slider.maxValue = 100f;
        slider.value = 0f;
        laneposition = 2;
        judgedNotesCount = 0;

        activateLane();
    }

    void Update()
    {
        UpdateScoreDisplay();
        UpdateGaugeTextPosition();

        if (notesManager == null || notesManager.NotesTime.Count == 0)
        {
            // ノーツがすべて判定された場合の終了処理
            CheckGameEnd();
            return;
        }

        bool usesController = nemsysController != null && nemsysController.IsInitialized;

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
        if (notesManager.NotesTime.Count > 0)
        {
            float noteIdealTime = notesManager.NotesTime[0] + musicManager.MusicStartTime;

            if (Time.time > noteIdealTime + 0.10f)
            {
                message(2);
                deleteData(0);
                judgedNotesCount++; // Miss時もカウント
                Debug.Log($"Miss (自動削除) - 判定済み: {judgedNotesCount}/{notesManager.noteNum}");
                slider.value -= 1.0f;
            }
        }
    }

    // ゲーム終了チェック（すべてのノーツが判定されたか）
    void CheckGameEnd()
    {
        if (IsGameEnded) return;

        // すべてのノーツが判定され、かつ少し時間が経過した場合
        if (musicManager.IsPlaying && judgedNotesCount >= notesManager.noteNum && Time.time > endTime + musicManager.MusicStartTime + 1f)
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
                    if (judgecnt[0] == notesManager.noteNum)
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

    void CheckNoteHit(int lane)
    {
        for (int i = 0; i < notesManager.LaneNum.Count; i++)
        {
            if (notesManager.LaneNum[i] == lane)
            {
                float noteIdealTime = notesManager.NotesTime[i] + musicManager.MusicStartTime;
                float timeLag = GetABS(Time.time - noteIdealTime);

                if (timeLag <= 0.10f)
                {
                    Judgement(timeLag, i, lane);
                    return;
                }
                else if (Time.time < noteIdealTime)
                {
                    Debug.Log($"レーン{lane}: 早すぎ");
                    TriggerLaneLight(lane, 2);
                    return;
                }
            }
        }

        Debug.Log($"レーン{lane}: 空打ち");
        TriggerLaneLight(lane, 2);
    }

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
            judgedNotesCount++; // Perfect時もカウント
            Debug.Log($"Perfect - 判定済み: {judgedNotesCount}/{notesManager.noteNum}");
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
            judgedNotesCount++; // OK時もカウント
            Debug.Log($"OK - 判定済み: {judgedNotesCount}/{notesManager.noteNum}");
        }
    }

    void activateLane()
    {
        for(int i = 0; i < 8; i++)
        {
            if(laneposition<=i && i < laneposition + 4)
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

    public void InitGameData()
    {
        if (notesManager != null && notesManager.noteNum > 0)
        {
            scorestandard = 1000000 / notesManager.noteNum;
            remainderFlug = true;
            remainder = 1000000 % notesManager.noteNum;
            Debug.Log($"総ノーツ数: {notesManager.noteNum}, 1ノーツあたりのスコア: {scorestandard}");
        }
        else
        {
            Debug.LogError("NotesManagerが設定されていないか、ノーツ数が0です");
            scorestandard = 0;
            return;
        }

        if (notesManager.NotesTime.Count > 0)
        {
            if (musicManager != null)
            {
                endTime = notesManager.NotesTime[notesManager.NotesTime.Count - 1];
                Debug.Log($"楽曲終了時間: {endTime}秒");
            }
            else
            {
                Debug.LogError("MusicManagerが設定されていません");
            }
        }
        else
        {
            Debug.LogWarning("ノーツが一つもありません");
        }
    }
}