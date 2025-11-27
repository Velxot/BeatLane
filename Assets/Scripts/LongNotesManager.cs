// LongNotesManager.cs
using System;
using UnityEngine;
using System.Collections.Generic;

public class LongNotesManager : MonoBehaviour
{
    // NotesManagerと共有するデータ構造
    [Serializable]
    public class Data // NotesManager.cs と同じ Data クラス定義を避けるため、別ファイルにするか、共通ファイルに移動してください。今回は一旦、LongNotesManager内でのみ参照するものとします。
    {
        public string name;
        public int maxBlock;
        public int BPM;
        public int offset;
        public Note[] notes;
    }

    [Serializable]
    public class Note
    {
        public int type;
        public int num;
        public int block;
        public int LPB;
        public Note[] notes; // ネストされたノーツ (ロングノーツの終点)
    }
    [SerializeField] private GameObject longNotePrefab; // ロングノーツ用のPrefab

    public float NotesSpeed; // NotesManagerから受け継ぐ速度

    private const float JUDGELINE_Z = 5.1f;

    // 生成したロングノーツオブジェクトのリスト
    public List<GameObject> LongNotesObj = new List<GameObject>();
    public int longNoteCount = 0; // 総ロングノーツ数

    // NotesManagerのGenerateNotesから呼び出されるか、NotesManagerと連携して呼び出される想定
    public void GenerateLongNotes(string chartFileName)
    {
        LoadLongNotes(chartFileName);
    }

    private void LoadLongNotes(string chartFileName)
    {
        LongNotesObj.Clear();
        longNoteCount = 0;

        // jsonファイルをResourcesから読み込む
        TextAsset chartAsset = Resources.Load<TextAsset>(chartFileName);
        if (chartAsset == null)
        {
            Debug.LogError($"譜面ファイルが見つかりません: Resources/{chartFileName}");
            return;
        }

        string inputString = Resources.Load<TextAsset>(chartFileName).ToString();
        Data inputJson = JsonUtility.FromJson<Data>(inputString);

        for (int i = 0; i < inputJson.notes.Length; i++)
        {
            Note startNote = inputJson.notes[i];

            // typeが2（ロングノーツの開始ノーツ）の場合のみ処理を行う
            if (startNote.type == 2 && startNote.notes != null && startNote.notes.Length > 0)
            {
                Note endNote = startNote.notes[0]; // ネストされた最初のノーツを終点とする

                // 開始時間を計算
                float kankaku = 60 / (inputJson.BPM * (float)startNote.LPB);
                float beatSec = kankaku * (float)startNote.LPB;
                float startTime = (beatSec * startNote.num / (float)startNote.LPB) + inputJson.offset * 0.01f;

                // 終了時間を計算 (終点ノーツのLPBとnumを使用)
                // 終点ノーツのLPBが親と異なる場合、親のLPBを使うか終点のLPBを使うか設計によるが、今回は終点のLPBを使う
                float endKankaku = 60 / (inputJson.BPM * (float)endNote.LPB);
                float endBeatSec = endKankaku * (float)endNote.LPB;
                float endTime = (endBeatSec * endNote.num / (float)endNote.LPB) + inputJson.offset * 0.01f;

                // 終了時間 < 開始時間 のチェック（譜面エラー対応）
                if (endTime <= startTime)
                {
                    Debug.LogWarning($"ロングノーツの終了時間が開始時間以下です: num={startNote.num} to num={endNote.num}");
                    continue;
                }

                float duration = endTime - startTime; // ノーツの持続時間
                float startZ_initial = startTime * NotesSpeed + JUDGELINE_Z;
                float endZ_initial = endTime * NotesSpeed + JUDGELINE_Z;
                float totalZLength = Mathf.Abs(endZ_initial - startZ_initial); // Z軸上の長さ

                // ロングノーツを生成
                // ロングノーツの中心位置を計算 (Z軸の半分の位置)
                float centerZ = startZ_initial - (totalZLength / 2f);

                // Prefabを生成。位置は中心、Xはレーンに依存
                GameObject newLongNote = Instantiate(
                    longNotePrefab,
                    new Vector3(startNote.block * 2 - 7.0f, 0.55f, centerZ),
                    Quaternion.identity
                );

                // スケールを調整して長さを表現
                // Z軸のスケールを totalZLength / スケール前のノーツの長さ に設定。
                // プレハブが単位長さ(例: 1.0)で設計されていると仮定
                newLongNote.transform.localScale = new Vector3(
                    newLongNote.transform.localScale.x,
                    newLongNote.transform.localScale.y,
                    totalZLength
                );

                // LongNoteコンポーネントに情報を設定
                LongNote longNoteComponent = newLongNote.GetComponent<LongNote>();
                if (longNoteComponent != null)
                {
                    longNoteComponent.notesSpeed = NotesSpeed;
                    longNoteComponent.musicStartTime = 0f; // StartNotesMovementで設定
                    longNoteComponent.startTargetTime = startTime;
                    longNoteComponent.endTargetTime = endTime;
                    longNoteComponent.lane = startNote.block;
                    LongNotesObj.Add(newLongNote);
                }

                longNoteCount++;
            }
        }

        Debug.Log($"ロングノーツ生成完了: {longNoteCount}個");
    }

    public void StartLongNotesMovement(float startMusicTime)
    {
        foreach (GameObject longNoteObj in LongNotesObj)
        {
            if (longNoteObj != null)
            {
                LongNote longNoteComponent = longNoteObj.GetComponent<LongNote>();
                if (longNoteComponent != null)
                {
                    // 楽曲開始時刻をノーツに渡し、移動開始フラグを立てる
                    longNoteComponent.musicStartTime = startMusicTime;
                    longNoteComponent.isGameStarted = true;
                }
            }
        }
    }
}