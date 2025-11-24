using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Data
{
    public string name;  // 曲名
    public int maxBlock; // 最大ブロック数
    public int BPM;      // BPM（曲のテンポ）
    public int offset;   // 開始タイミングのオフセット
    public Note[] notes; // ノーツ情報のリスト
}

[Serializable]
public class Note
{
    public int type;  // ノーツの種類（通常ノーツ・ロングノーツなど）
    public int num;   // 何拍目に配置されるか
    public int block; // どのレーンに配置されるか
    public int LPB;   // 1拍あたりの分割数
}

public class NotesManager : MonoBehaviour
{
    //総ノーツ数
    public int noteNum;
    //曲名
    private string songName;
    //ノーツのレーン
    public List<int> LaneNum = new List<int>();
    //ノーツの種類
    public List<int> NoteType = new List<int>();
    //ノーツが判定線と重なる時間
    public List<float> NotesTime = new List<float>();
    //gameobject
    public List<GameObject> NotesObj = new List<GameObject>();
    //ノーツの速度
    [SerializeField] public float NotesSpeed;
    //ノーツのprefabを入れる
    [SerializeField] GameObject noteObj;

    //[SerializeField] public float VisualOffsetZ = -5.0f;

    [SerializeField] private MusicManager musicManager;

    private const float JUDGELINE_Z = 5.1f; // 定義を追加 (または直接 5.1f を使用)

    void OnEnable()
    {
        //総ノーツを0にする
        noteNum = 0;

        // ハードコーディング: 譜面ファイル名を直接指定
        songName = "狂喜蘭舞";

        Debug.Log($"譜面ファイル: {songName}");
    }

    // MusicManagerから呼び出される：ノーツを生成する
    public void GenerateNotes()
    {
        Load(songName);
    }

    private void Load(string SongName)
    {
        //jsonファイルを読み込む
        string inputString = Resources.Load<TextAsset>(SongName).ToString();
        Data inputJson = JsonUtility.FromJson<Data>(inputString);

        //総ノーツ数を設定
        noteNum = inputJson.notes.Length;

        for (int i = 0; i < inputJson.notes.Length; i++)
        {
            //時間を計算
            float kankaku = 60 / (inputJson.BPM * (float)inputJson.notes[i].LPB);
            float beatSec = kankaku * (float)inputJson.notes[i].LPB;
            float time = (beatSec * inputJson.notes[i].num / (float)inputJson.notes[i].LPB) + inputJson.offset * 0.01f;

            //リストに追加
            NotesTime.Add(time);
            LaneNum.Add(inputJson.notes[i].block);
            NoteType.Add(inputJson.notes[i].type);

            float z_initial = time * NotesSpeed + JUDGELINE_Z;

            //ノーツを生成
            GameObject newNote = Instantiate(noteObj, new Vector3(inputJson.notes[i].block * 2 - 7.0f, 0.55f, z_initial), Quaternion.identity);

            // NotesManagerのNotesSpeedをノーツの移動スクリプトに設定
            notes notesComponent = newNote.GetComponent<notes>();
            if (notesComponent != null)
            {
                notesComponent.notesSpeed = NotesSpeed;
                notesComponent.targetTime = time;
                NotesObj.Add(newNote);
            }

            NotesObj.Add(newNote);
        }

        Debug.Log($"ノーツ生成完了: {noteNum}個");
    }

    public float GetMusicEndTime(float musicStartTime)
    {
        if (NotesTime.Count > 0)
        {
            // 最後のノーツの時間 + 音楽の開始時間
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
                    // 楽曲開始時刻をノーツに渡し、移動開始フラグを立てる
                    notesComponent.musicStartTime = startMusicTime;
                    notesComponent.isGameStarted = true;
                }
            }
        }
    }
}