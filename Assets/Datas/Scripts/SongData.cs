using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [1] 譜面ごとの情報を定義するクラスを SongData クラス内に記述
[System.Serializable]
public class ChartData
{
    // 難易度名 (例: "Easy", "Hard")
    [SerializeField] public string chartName; 
    
    // 難易度レベル (例: 1, 5, 9)
    [SerializeField] public int difficultyLevel;
    
    // ノーツデータのファイル名 (NotesManagerが読み込むファイル名)
    // 例: "MySongA_Hard"
    [SerializeField] public string chartFileName; 
}


[CreateAssetMenu(fileName = "SongData", menuName = "楽曲データを作成")]
public class SongData : ScriptableObject
{
    [SerializeField] public string songID;
    [SerializeField] public string songName;
    
    // [2] 既存の songLevel を削除し、ChartData のリストに変更する
    // [SerializeField] public int songLevel; // ← 削除
    
    // ★ 複数の譜面（難易度）を格納するためのリスト
    [SerializeField] public List<ChartData> availableCharts = new List<ChartData>();
    
    [SerializeField] public Sprite songImage;
}