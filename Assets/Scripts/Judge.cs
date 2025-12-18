using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic; // Dictionary���g�����߂ɒǉ�

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

        // ������ �����n�߁E�ʏ�m�[�c�̃^�b�v���� (GetButtonDown) ������
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

        // Miss����
        HandleNormalNoteMiss();
    }

    // ������ �C��: �ʏ�m�[�c��Miss��������\�b�h�ɐ؂�o�� ������
    void HandleNormalNoteMiss()
    {
        if (notesManager.NotesTime.Count > 0)
        {
            float noteIdealTime = notesManager.NotesTime[0] + musicManager.MusicStartTime;

            if (Time.time > noteIdealTime + 0.10f)
            {
                message(2); // Miss
                deleteData(0); // �ʏ�m�[�c���폜
                judgedNotesCount++;
                Debug.Log($"Miss (�����폜) - ����ςݒʏ�m�[�c: {judgedNotesCount}");
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

    // ������ �C��: CheckNoteHit (�����O�m�[�c�̉����n�߂𔻒�ɒǉ�) ������
    void CheckNoteHit(int lane)
    {

        // 1. �ʏ�m�[�c (type:1) �̔��� (NotesManager��type:1�݂̂��i�[����Ă���O��)
        for (int i = 0; i < notesManager.LaneNum.Count; i++)
        {
            if (notesManager.LaneNum[i] == lane)
            {
                float noteIdealTime = notesManager.NotesTime[i] + musicManager.MusicStartTime;
                float timeLag = GetABS(Time.time - noteIdealTime);

                if (timeLag <= 0.10f)
                {
                    Judgement(timeLag, i, lane); // �ʏ�m�[�c����
                    return;
                }
                else if (Time.time < noteIdealTime)
                {
                    Debug.Log($"���[��{lane}: �������i�ʏ�m�[�c�j");
                    TriggerLaneLight(lane, 2);
                    return;
                }
            }
        }

        Debug.Log($"���[��{lane}: ��ł�");
        TriggerLaneLight(lane, 2);
    }


    // �ʏ�m�[�c�̔��菈��
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
        if (laneLights != null && laneNum >= 0 && laneNum < laneLights.Length && laneLights[laneNum] != null)
        {
            laneLights[laneNum].LightUp(judgeType);
        }
        else
        {
            Debug.LogWarning($"���[��{laneNum}��lightsScript���ݒ肳��Ă��܂���");
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