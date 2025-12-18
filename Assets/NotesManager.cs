using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Data
{
    public string name;  // �Ȗ�
    public int maxBlock; // �ő�u���b�N��
    public int BPM;      // BPM�i�Ȃ̃e���|�j
    public int offset;   // �J�n�^�C�~���O�̃I�t�Z�b�g
    public Note[] notes; // �m�[�c���̃��X�g
}

[Serializable]
public class Note
{
    public int type;  // �m�[�c�̎�ށi�ʏ�m�[�c�E�����O�m�[�c�Ȃǁj
    public int num;   // �����ڂɔz�u����邩
    public int block; // �ǂ̃��[���ɔz�u����邩
    public int LPB;   // 1��������̕�����
}

public class NotesManager : MonoBehaviour
{
    //���m�[�c��
    public int noteNum;
    //�Ȗ�
    private string songName;
    //�m�[�c�̃��[��
    public List<int> LaneNum = new List<int>();
    //�m�[�c�̎��
    public List<int> NoteType = new List<int>();
    //�m�[�c��������Əd�Ȃ鎞��
    public List<float> NotesTime = new List<float>();
    //gameobject
    public List<GameObject> NotesObj = new List<GameObject>();
    //�m�[�c�̑��x
    [SerializeField] public float NotesSpeed;
    //�m�[�c��prefab������
    [SerializeField] GameObject noteObj;

    
    [SerializeField] SongDataBase database;

    [SerializeField] private MusicManager musicManager;

    private const float JUDGELINE_Z = 5.1f; // ��`��ǉ� (�܂��͒��� 5.1f ���g�p)

    void OnEnable()
    {
        //���m�[�c��0�ɂ���
        noteNum = 0;

        songName = database.songData[SongSelect.select].songName;

        Debug.Log($"���ʃt�@�C��: {songName}");
    }

    // MusicManager����Ăяo�����F�m�[�c�𐶐�����
    public void GenerateNotes()
    {
        // 1. �I�����ꂽ�y�Ȃƕ��ʃC���f�b�N�X���擾
        int songIndex = SongSelect.select;
        int chartIndex = SongSelect.selectedChartIndex; // ������ �I�����ꂽ���ʃC���f�b�N�X���擾

        // �G���[�`�F�b�N
        if (database == null || songIndex < 0 || songIndex >= database.songData.Length)
        {
            Debug.LogError($"SongDataBase���s���A�܂��͊y��ID({songIndex})�������ł��B");
            return;
        }

        SongData selectedSong = database.songData[songIndex];

        if (chartIndex < 0 || chartIndex >= selectedSong.availableCharts.Count)
        {
            Debug.LogError($"����ID({chartIndex})�������ł��B�y��: {selectedSong.songName}");
            return;
        }

        // ������ 2. �I�����ꂽ ChartData ����t�@�C�������擾 ������
        ChartData selectedChart = selectedSong.availableCharts[chartIndex];
        string chartFileName = selectedChart.chartFileName; // ���ʃt�@�C�������擾

        // ������ songName �𕈖ʖ��ɒu��������i�����K�v�Ȃ�j
        // songName = selectedSong.songName; // �y�Ȗ����̂͂��̂܂�

        // 3. ���ʃt�@�C���� Resources ����ǂݍ���
        // ����: TextAsset json = (TextAsset)Resources.Load("Notes/" + songName);
        // ������ �ύX: chartFileName���g�p ������
        TextAsset json = (TextAsset)Resources.Load(chartFileName);

        if (json == null)
        {
            Debug.LogError($"�m�[�c�t�@�C����������܂���: Resources/{chartFileName}");
            return;
        }
        Load(chartFileName);
    }

    private void Load(string SongName)
    {
        //json�t�@�C����ǂݍ���
        string inputString = Resources.Load<TextAsset>(SongName).ToString();
        Data inputJson = JsonUtility.FromJson<Data>(inputString);

        //���m�[�c����ݒ�
        //noteNum = inputJson.notes.Length;

        // �m�[�c������U�N���A
        NotesTime.Clear();
        LaneNum.Clear();
        NoteType.Clear();
        NotesObj.Clear();
        noteNum = 0; // �m�[�c���������Z�b�g

        for (int i = 0; i < inputJson.notes.Length; i++)
        {
            // ������ �ǉ�: type��1�i�ʏ�m�[�c�j�̏ꍇ�̂ݏ������s�� ������
            if (inputJson.notes[i].type == 1 && inputJson.notes[i].block<8)
            {
                //���Ԃ��v�Z
                float kankaku = 60 / (inputJson.BPM * (float)inputJson.notes[i].LPB);
                float beatSec = kankaku * (float)inputJson.notes[i].LPB;
                float time = (beatSec * inputJson.notes[i].num / (float)inputJson.notes[i].LPB) + inputJson.offset * 0.01f;

                //���X�g�ɒǉ�
                NotesTime.Add(time);
                LaneNum.Add(inputJson.notes[i].block);
                NoteType.Add(inputJson.notes[i].type);

                float z_initial = time * NotesSpeed + JUDGELINE_Z;

                //�m�[�c�𐶐�
                // �ʏ�m�[�c�̃v���n�u���g�p
                GameObject newNote = Instantiate(noteObj, new Vector3(inputJson.notes[i].block * 2 - 7.0f, 0.55f, z_initial), Quaternion.identity);

                // NotesManager��NotesSpeed���m�[�c�̈ړ��X�N���v�g�ɐݒ�
                notes notesComponent = newNote.GetComponent<notes>();
                if (notesComponent != null)
                {
                    notesComponent.notesSpeed = NotesSpeed;
                    notesComponent.targetTime = time;
                    NotesObj.Add(newNote);
                }

                noteNum++; // ���������ʏ�m�[�c���J�E���g
            }
            // type��2�ȏ�̃m�[�c�́A�V�����X�N���v�g�ŏ������邽�߂ɂ����ł̓X�L�b�v
        }

        Debug.Log($"�ʏ�m�[�c��������: {noteNum}��");
    }

    public float GetMusicEndTime(float musicStartTime)
    {
        if (NotesTime.Count > 0)
        {
            // �Ō�̃m�[�c�̎��� + ���y�̊J�n����
            return NotesTime[NotesTime.Count - 1] + musicStartTime;
        }
        return 0f;
    }

    public void StartNotesMovement(float startMusicTime)
    {
        foreach (GameObject noteObj in NotesObj)
        {
            if (noteObj != null)
            {
                notes notesComponent = noteObj.GetComponent<notes>();
                if (notesComponent != null)
                {
                    // �y�ȊJ�n�������m�[�c�ɓn���A�ړ��J�n�t���O�𗧂Ă�
                    notesComponent.musicStartTime = startMusicTime;
                    notesComponent.isGameStarted = true;
                }
            }
        }
    }
}