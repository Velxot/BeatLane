using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

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

    // ���肳�ꂽ�m�[�c�̑��� (�ʏ�m�[�c��Perfect/OK/Miss + �����O�m�[�c�̊J�n/�I����Perfect/OK/Miss)
    private int judgedNotesCount = 0;


    // �����p�̔���^�C�v��`
    enum JudgementType { Start, Release }

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

        // �m�[�c�����ׂĔ��肳�ꂽ�ꍇ�̏I������
        CheckGameEnd();

        if (IsGameEnded) return;

        bool usesController = nemsysController != null && nemsysController.IsInitialized;

        // --- 通常ノーツ / レーン切り替えの判定 (押した瞬間) ---
        if ((usesController && nemsysController.GetButtonDown(0)) || Input.GetKeyDown(KeyCode.S)) CheckNoteHit(laneposition);
        if ((usesController && nemsysController.GetButtonDown(1)) || Input.GetKeyDown(KeyCode.F)) CheckNoteHit(laneposition + 1);
        if ((usesController && nemsysController.GetButtonDown(2)) || Input.GetKeyDown(KeyCode.J)) CheckNoteHit(laneposition + 2);
        if ((usesController && nemsysController.GetButtonDown(3)) || Input.GetKeyDown(KeyCode.L)) CheckNoteHit(laneposition + 3);

        // レーン移動
        if (laneposition > 0 && ((usesController && nemsysController.GetButtonDown(4)) || Input.GetKeyDown(KeyCode.E)))
        {
            laneposition--;
            activateLane();
            CheckNoteHit(16);
        }
        if (laneposition < 4 && ((usesController && nemsysController.GetButtonDown(5)) || Input.GetKeyDown(KeyCode.I)))
        {
            laneposition++;
            activateLane();
            CheckNoteHit(17);
        }
        //トレースノーツの自動判定 (毎フレーム) ---
        HandleTraceNoteHit();

        // Miss判定
        HandleNormalNoteMiss();
    }

    // --- 追加: ボタンが「押しっぱなし」かどうかを判定するヘルパー ---
    bool IsLanePressed(int laneIndex)
    {
        bool usesController = nemsysController != null && nemsysController.IsInitialized;

        // laneIndexはlaneposition(0-4) + ボタン位置(0-3)
        int buttonIdx = laneIndex - laneposition;

        if (buttonIdx < 0 || buttonIdx > 3) return false;

        if (usesController)
        {
            return nemsysController.GetButton(buttonIdx); // コントローラーの現在の状態
        }
        else
        {
            KeyCode[] keys = { KeyCode.S, KeyCode.F, KeyCode.J, KeyCode.L };
            return Input.GetKey(keys[buttonIdx]);
        }
    }

    // --- 追加: トレースノーツの処理 ---
    void HandleTraceNoteHit()
    {
        if (notesManager == null || notesManager.NotesTime.Count == 0) return;

        // 全ノーツを走査 (逆順にループすることで削除時のインデックスずれを防ぐ)
        for (int i = 0; i < notesManager.NotesTime.Count; i++)
        {
            // トレースノーツ)の場合
            if (notesManager.LaneNum[i] % 2 == 1)
            {
                float noteIdealTime = notesManager.NotesTime[i] + musicManager.MusicStartTime;
                int lane = notesManager.LaneNum[i] / 2; // レーン番号の取得ロジックに合わせる

                // 判定ラインに到達した（あるいは少し過ぎた）瞬間
                if (Time.time >= noteIdealTime - 0.05f)
                {
                    // 該当レーンが押されているか確認
                    if (IsLanePressed(lane))
                    {
                        Debug.Log("Trace Perfect!");
                        Judgement(0, i, lane); // Perfect扱いで処理
                        return; // 1フレームに1つ処理すれば十分（リストが変わるため）
                    }
                    // 判定時間を大幅に過ぎた場合はMiss (HandleNormalNoteMissでも処理されるが念のため)
                }
            }
        }
    }

    void HandleNormalNoteMiss()
    {
        if (notesManager.NotesTime.Count > 0)
        {
            float noteIdealTime = notesManager.NotesTime[0] + musicManager.MusicStartTime;

            // 判定幅（0.10f）を完全に通り過ぎた場合のみMissとする
            // かつ、現在の時間が理想時間より明らかに未来であることを確認
            if (Time.time > noteIdealTime + 0.12f)
            {
                // デバッグログでどのノーツがMissになったか特定しやすくする
                Debug.Log($"Miss確定: Lane={notesManager.LaneNum[0]} Time={notesManager.NotesTime[0]}");

                message(2); // Miss
                deleteData(0);
                judgedNotesCount++;
                slider.value -= 1.0f;
            }
        }
    }


    // �Q�[���I���`�F�b�N�i���ׂẴm�[�c�����肳�ꂽ���j
    void CheckGameEnd()
    {
        if (IsGameEnded) return;

        // �m�[�c�������v�Z (�ʏ�m�[�c + �����O�m�[�c�̊J�n/�I����2��)
        int totalNotesCount = (notesManager != null ? notesManager.noteNum : 0);

        // ���ׂẴm�[�c�����肳��A���������Ԃ��o�߂����ꍇ
        if (musicManager.IsPlaying && judgedNotesCount >= totalNotesCount && Time.time > endTime + musicManager.MusicStartTime + 1f)
        {
            // �N���A����
            if (slider.value < 70.0f)
            {
                MessageObj[6].text = "FAILED...";
            }
            else
            {
                MessageObj[6].text = "CLEAR";

                // �t���R���{����iMiss��0�̏ꍇ�j
                if (judgecnt[2] == 0)
                {
                    MessageObj[6].text = "FULL COMBO";

                    // �I�[���p�[�t�F�N�g����
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

    // 既存のCheckNoteHitを修正 (トレースノーツが重複反応しないように)
    void CheckNoteHit(int lane)
    {
        for (int i = 0; i < notesManager.LaneNum.Count; i++)
        {
            if (notesManager.LaneNum[i] < 16 && notesManager.LaneNum[i] % 2 == 1) continue;

            // --- レーン切替ノーツ (16以上) の判定 ---
            if (notesManager.LaneNum[i] >= 16 && notesManager.LaneNum[i] == lane)
            {
                float noteIdealTime = notesManager.NotesTime[i] + musicManager.MusicStartTime;
                float timeLag = GetABS(Time.time - noteIdealTime);

                // 本来ならOKの範囲（0.10f以内）であれば実行
                if (timeLag <= 0.10f)
                {
                    // 第1引数に 0 を渡すことで、Judgementメソッド内で必ず Perfect 判定（<= 0.045f）になります
                    Judgement(0f, i, lane);
                    return;
                }
            }
            // 通常ノーツ (偶数)
            if (notesManager.LaneNum[i] < 16 && notesManager.LaneNum[i] / 2 == lane && notesManager.LaneNum[i] % 2 == 0)
            {
                float noteIdealTime = notesManager.NotesTime[i] + musicManager.MusicStartTime;
                float timeLag = GetABS(Time.time - noteIdealTime);

                if (timeLag <= 0.10f)
                {
                    Judgement(timeLag, i, lane);
                    return;
                }
            }
        }
        TriggerLaneLight(lane, 2);
    }


    // �ʏ�m�[�c�̔��菈��
    void Judgement(float timeLag, int noteIndex, int lane)
    {
        // インデックスが現在のリストの範囲内か再確認（二重判定防止）
        if (noteIndex >= notesManager.NotesTime.Count) return;

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
            Debug.Log($"Perfect - ����ς�: {judgedNotesCount}");
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
            Debug.Log($"OK - ����ς�: {judgedNotesCount}");
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
        if (laneLights == null || laneLights.Length == 0) return;

        // レーン切替ノーツ（16, 17など）の場合は、全レーンを光らせる
        if (laneNum >= 16)
        {
            for (int i = 0; i < laneLights.Length; i++)
            {
                if (laneLights[i] != null)
                {
                    laneLights[i].LightUp(judgeType);
                }
            }
        }
        // 通常のレーン番号（0〜7など）の場合、配列の範囲内かチェックして光らせる
        else if (laneNum >= 0 && laneNum < laneLights.Length)
        {
            if (laneLights[laneNum] != null)
            {
                laneLights[laneNum].LightUp(judgeType);
            }
        }
    }

    float GetABS(float num)
    {
        return num >= 0 ? num : -num;
    }

    // �ʏ�m�[�c�̃f�[�^�폜
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
            Debug.LogWarning($"MessageObj[{judge}]���ݒ肳��Ă��܂���");
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
            // OK����� Perfect �� 3/4 �̃X�R�A
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

        Debug.Log($"�Q�[���I�� - �X�R�A: {score}, �����N: {GameResultData.ResultRank}");
    }

    void ResultScene()
    {
        SceneManager.LoadScene("ResultScene");
    }

    // ������ �C��: InitGameData (�����O�m�[�c�̑������܂߂ăX�R�A�v�Z) ������
    public void InitGameData()
    {
        // �����O�m�[�c�́u�J�n�v�Ɓu�I���v��2�񔻒肳��邽�߁A���m�[�c���� (�ʏ�m�[�c�� + �����O�m�[�c�� * 2) �Ōv�Z
        int totalNotes = (notesManager != null ? notesManager.noteNum : 0);

        if (totalNotes > 0)
        {
            scorestandard = 1000000 / totalNotes;
            remainderFlug = true;
            remainder = 1000000 % totalNotes;

            Debug.Log($"�������: {totalNotes}, 1���肠����̃X�R�A: {scorestandard}");
        }
        else
        {
            Debug.LogError("�m�[�c�}�l�[�W�����ݒ肳��Ă��Ȃ����A�m�[�c����0�ł�");
            scorestandard = 0;
            return;
        }

        // �I�����Ԃ̌v�Z (NotesManager��LongNotesManager�̍ŏI�m�[�c���r)
        float normalNoteEndTime = (notesManager != null && notesManager.NotesTime.Count > 0) ? notesManager.NotesTime[notesManager.NotesTime.Count - 1] : 0f;

        endTime = normalNoteEndTime;

        if (musicManager == null)
        {
            Debug.LogError("MusicManager���ݒ肳��Ă��܂���");
        }
        Debug.Log($"�y�ȏI������: {endTime}�b");
    }
}