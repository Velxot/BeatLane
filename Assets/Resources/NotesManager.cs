using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

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
    [SerializeField] private float NotesSpeed;
    //ノーツのprefabを入れる
    [SerializeField] GameObject noteObj;

    [SerializeField] private float VisualOffsetZ = 5.0f;

    [SerializeField] private AudioSource songAudioSource;

    [SerializeField] private float startDelay = 0.75f;

    void OnEnable()
    {
        //総ノーツを0にする
        noteNum = 0;
        //読み込む譜面のファイル名を入力
        songName = "狂喜乱舞_easy";

        Load(songName);

        if (songAudioSource != null)
        {
            // ★修正: PlayDelayedを使用
            songAudioSource.PlayDelayed(startDelay);
        }
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

            float z = NotesTime[i] * NotesSpeed;

            // ★修正: VisualOffsetZをZ座標に加算して、ノーツの初期位置を奥に移動させる
            float z_initial = z + VisualOffsetZ;

            //ノーツを生成
            // z_initial を使用
            GameObject newNote = Instantiate(noteObj, new Vector3(inputJson.notes[i].block * 2 - 7.0f, 0.55f, z_initial), Quaternion.identity);

            // ★追加: NotesManagerのNotesSpeedをノーツの移動スクリプトに設定
            notes notesComponent = newNote.GetComponent<notes>();
            if (notesComponent != null)
            {
                notesComponent.notesSpeed = NotesSpeed; // NotesManagerのNotesSpeedを使用
            }

            NotesObj.Add(newNote);
        }
    }
    private IEnumerator StartSongWithDelay(float delayTime)
    {
        // 指定された秒数だけ待機
        yield return new WaitForSeconds(delayTime);

        // 待機後、AudioSourceが設定されていれば再生する
        if (songAudioSource != null)
        {
            songAudioSource.Play();
        }
        else
        {
            Debug.LogError("AudioSourceが設定されていません。");
        }
    }

}
