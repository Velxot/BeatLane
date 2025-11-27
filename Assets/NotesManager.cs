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

    [SerializeField] private LongNotesManager longNotesManager;

    [SerializeField] SongDataBase database;

    [SerializeField] private MusicManager musicManager;

    private const float JUDGELINE_Z = 5.1f; // 定義を追加 (または直接 5.1f を使用)

    void OnEnable()
    {
        //総ノーツを0にする
        noteNum = 0;

        songName = database.songData[SongSelect.select].songName;

        Debug.Log($"譜面ファイル: {songName}");
    }

    // MusicManagerから呼び出される：ノーツを生成する
    public void GenerateNotes()
    {
        // 1. 選択された楽曲と譜面インデックスを取得
        int songIndex = SongSelect.select;
        int chartIndex = SongSelect.selectedChartIndex; // ★★★ 選択された譜面インデックスを取得

        // エラーチェック
        if (database == null || songIndex < 0 || songIndex >= database.songData.Length)
        {
            Debug.LogError($"SongDataBaseが不正、または楽曲ID({songIndex})が無効です。");
            return;
        }

        SongData selectedSong = database.songData[songIndex];

        if (chartIndex < 0 || chartIndex >= selectedSong.availableCharts.Count)
        {
            Debug.LogError($"譜面ID({chartIndex})が無効です。楽曲: {selectedSong.songName}");
            return;
        }

        // ★★★ 2. 選択された ChartData からファイル名を取得 ★★★
        ChartData selectedChart = selectedSong.availableCharts[chartIndex];
        string chartFileName = selectedChart.chartFileName; // 譜面ファイル名を取得

        // 既存の songName を譜面名に置き換える（もし必要なら）
        // songName = selectedSong.songName; // 楽曲名自体はそのまま

        // 3. 譜面ファイルを Resources から読み込み
        // 既存: TextAsset json = (TextAsset)Resources.Load("Notes/" + songName);
        // ★★★ 変更: chartFileNameを使用 ★★★
        TextAsset json = (TextAsset)Resources.Load(chartFileName);

        if (json == null)
        {
            Debug.LogError($"ノーツファイルが見つかりません: Resources/{chartFileName}");
            return;
        }
        Load(chartFileName);
        if (longNotesManager != null)
        {
            // 既存の NotesManager.cs の GenerateNotes メソッド内で Load が呼ばれた後に実行
            longNotesManager.GenerateLongNotes(chartFileName);
        }
        else
        {
            Debug.LogError("LongNotesManagerがNotesManagerに設定されていません。");
        }
    }

    private void Load(string SongName)
    {
        //jsonファイルを読み込む
        string inputString = Resources.Load<TextAsset>(SongName).ToString();
        Data inputJson = JsonUtility.FromJson<Data>(inputString);

        //総ノーツ数を設定
        //noteNum = inputJson.notes.Length;

        // ノーツ情報を一旦クリア
        NotesTime.Clear();
        LaneNum.Clear();
        NoteType.Clear();
        NotesObj.Clear();
        noteNum = 0; // ノーツ総数をリセット

        for (int i = 0; i < inputJson.notes.Length; i++)
        {
            // ★★★ 追加: typeが1（通常ノーツ）の場合のみ処理を行う ★★★
            if (inputJson.notes[i].type == 1 && inputJson.notes[i].block<8)
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
                // 通常ノーツのプレハブを使用
                GameObject newNote = Instantiate(noteObj, new Vector3(inputJson.notes[i].block * 2 - 7.0f, 0.55f, z_initial), Quaternion.identity);

                // NotesManagerのNotesSpeedをノーツの移動スクリプトに設定
                notes notesComponent = newNote.GetComponent<notes>();
                if (notesComponent != null)
                {
                    notesComponent.notesSpeed = NotesSpeed;
                    notesComponent.targetTime = time;
                    NotesObj.Add(newNote);
                }

                noteNum++; // 生成した通常ノーツをカウント
            }
            // typeが2以上のノーツは、新しいスクリプトで処理するためにここではスキップ
        }

        Debug.Log($"通常ノーツ生成完了: {noteNum}個");
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