using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Judge : MonoBehaviour
{
    // プレイヤーに判定を伝えるゲームオブジェクト
    [SerializeField] private TextMeshProUGUI[] MessageObj;

    // NotesManagerを入れる変数
    [SerializeField] private NotesManager notesManager;

    //スライダーを入れる変数
    [SerializeField] private Slider slider;

    // 各レーンのlightsScriptを入れる変数（8レーン分）
    [SerializeField] private lightsScript[] laneLights;

    // NEMSYSコントローラー入力
    [SerializeField] private NEMSYSControllerInput nemsysController;

    int[] judgecnt = { 0, 0, 0, 0 };
    int score = 0;
    float displayScore = 0f;
    int targetScore = 0;
    int scorestandard;  //1パーフェクトのスコア
    bool remainderFlug;
    int remainder;  //オールパーフェクト時に1000000点で合わせるため、あまりを保持

    [SerializeField] private float scoreAnimationDuration = 0.3f;
    [SerializeField] private AudioSource judgeSoundSource;
    [SerializeField] private AudioClip perfectClip;
    [SerializeField] private AudioClip okClip;
    [SerializeField] private RectTransform percentTextRect;
    [SerializeField] private float gaugeTextOffset = 10f;

    float endTime = 0f;

    int laneposition;

    void Start()
    {
        slider.maxValue = 100f;
        slider.value = 0f;

        laneposition = 2;

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
        }

        endTime = notesManager.NotesTime[notesManager.NotesTime.Count - 1];
    }

    void Update()
    {
        UpdateScoreDisplay();
        UpdateGaugeTextPosition();

        if (notesManager == null || notesManager.NotesTime.Count == 0)
            return;

        // コントローラーが初期化されている場合はコントローラー入力、そうでなければキーボード入力
        bool usesController = nemsysController != null && nemsysController.IsInitialized;

        // BT-A（ボタン0） または Sキー
        if ((usesController && nemsysController.GetButtonDown(0)) || Input.GetKeyDown(KeyCode.S))
        {
            CheckNoteHit(laneposition);
        }

        // BT-B（ボタン1） または Fキー
        if ((usesController && nemsysController.GetButtonDown(1)) || Input.GetKeyDown(KeyCode.F))
        {
            CheckNoteHit(laneposition + 1);
        }

        // BT-C（ボタン2） または Jキー
        if ((usesController && nemsysController.GetButtonDown(2)) || Input.GetKeyDown(KeyCode.J))
        {
            CheckNoteHit(laneposition + 2);
        }

        // BT-D（ボタン3） または Lキー
        if ((usesController && nemsysController.GetButtonDown(3)) || Input.GetKeyDown(KeyCode.L))
        {
            CheckNoteHit(laneposition + 3);
        }

        // レーン移動（左）: FX-L（ボタン4） または Eキー
        if (laneposition > 0 && ((usesController && nemsysController.GetButtonDown(4)) || Input.GetKeyDown(KeyCode.E)))
        {
            laneposition--;
        }

        // レーン移動（右）: FX-R（ボタン5） または Iキー
        if (laneposition < 4 && ((usesController && nemsysController.GetButtonDown(5)) || Input.GetKeyDown(KeyCode.I)))
        {
            laneposition++;
        }

        // Miss判定
        if (notesManager.NotesTime.Count > 0 && Time.time > notesManager.NotesTime[0] + 0.1f)
        {
            message(2);
            deleteData(0);
            Debug.Log("Miss");
            slider.value -= 1.0f;
        }

        if(Time.time > endTime)
        {
            if (slider.value<70.0f)
            {
                MessageObj[6].text = "FAILED...";
            }
            else
            {
                MessageObj[6].text = "CLEAR";
                if (judgecnt[3] == notesManager.noteNum)
                {
                    MessageObj[6].text = "FULL COMBO";
                    if (score == 1000000)
                    {
                        MessageObj[6].text = "ALL PERFECT";
                    }
                }
            }
            OnGameEnd();
            Invoke("ResultScene", 3f);
            return;
        }
    }

    void CheckNoteHit(int lane)
    {
        for (int i = 0; i < notesManager.LaneNum.Count; i++)
        {
            if (notesManager.LaneNum[i] == lane)
            {
                float timeLag = GetABS(Time.time - notesManager.NotesTime[i]);

                if (timeLag <= 0.10f)
                {
                    Judgement(timeLag, i, lane);
                    return;
                }
            }
        }

        Debug.Log($"レーン{lane}: 空打ち");
        TriggerLaneLight(lane, 2);
    }

    void Judgement(float timeLag, int noteIndex, int lane)
    {
        if (timeLag <= 0.045)
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
        }
        else if (timeLag <= 0.10)
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
        if (num >= 0)
        {
            return num;
        }
        else
        {
            return -num;
        }
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
        Debug.Log($"目標スコア: {targetScore}");
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
        // ゲーム終了時にGameResultDataへコピー
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

    }

    void ResultScene()
    {
        SceneManager.LoadScene("ResultScene");
    }
}