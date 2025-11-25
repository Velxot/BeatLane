using UnityEngine;

// シーンをまたいでリザルトデータを保持する静的クラス
public static class GameResultData
{
    public static int Score { get; set; }
    public static int PerfectCount { get; set; }
    public static int OkCount { get; set; }
    public static int MissCount { get; set; }
    public static int Combo { get; set; }

    // リザルト判定（CLEAR, FULL COMBO, ALL PERFECT）
    public static string ResultRank { get; set; }

    // データをリセットする（ゲーム開始時に呼ぶ）
    public static void Reset()
    {
        Score = 0;
        PerfectCount = 0;
        OkCount = 0;
        MissCount = 0;
        Combo = 0;
        ResultRank = "";
    }

    // デバッグ用：データを表示
    public static void DebugLog()
    {
        Debug.Log($"=== Game Result ===");
        Debug.Log($"Score: {Score}");
        Debug.Log($"Perfect: {PerfectCount}");
        Debug.Log($"OK: {OkCount}");
        Debug.Log($"Miss: {MissCount}");
        Debug.Log($"Combo: {Combo}");
        Debug.Log($"Result: {ResultRank}");
    }
}