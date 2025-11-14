using UnityEngine;
using TMPro;

public class Judge : MonoBehaviour
{
    // プレイヤーに判定を伝えるゲームオブジェクト
    [SerializeField] private TextMeshProUGUI[] MessageObj;

    // NotesManagerを入れる変数(クラス名を大文字に修正)
    [SerializeField] private NotesManager notesManager;

    int[] judgecnt = { 0, 0, 0 ,0};

    void Update()
    {
        // notesManagerとリストが空でないかチェック
        if (notesManager == null || notesManager.NotesTime.Count == 0)
            return;

        if (Input.GetKeyDown(KeyCode.S)) // Sキーが押されたとき
        {
            if (notesManager.LaneNum[0] == 2) // 押されたボタンはレーンの番号とあっているか?
            {
                Judgement(GetABS(Time.time - notesManager.NotesTime[0]));
            }
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (notesManager.LaneNum[0] == 3)
            {
                Judgement(GetABS(Time.time - notesManager.NotesTime[0]));
            }
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (notesManager.LaneNum[0] == 4)
            {
                Judgement(GetABS(Time.time - notesManager.NotesTime[0]));
            }
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (notesManager.LaneNum[0] == 5)
            {
                Judgement(GetABS(Time.time - notesManager.NotesTime[0]));
            }
        }

        // 本来ノーツをたたくべき時間から0.1秒たっても入力がなかった場合
        if (Time.time > notesManager.NotesTime[0] + 0.1f)
        {
            message(2);
            deleteData();
            Debug.Log("Miss");
        }
    }

    void Judgement(float timeLag)
    {
        // 本来ノーツをたたくべき時間と実際にノーツをたたいた時間の誤差が0.045秒以下だったら
        if (timeLag <= 0.045)
        {
            Debug.Log("Perfect");
            message(0);
            deleteData();
        }
        else if (timeLag <= 0.10) // 0.10秒以下だったら
        {
            Debug.Log("OK");
            message(1);
            deleteData();
        }
    }

    float GetABS(float num) // 引数の絶対値を返す関数
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

    // すでにたたいたノーツを削除する関数
    void deleteData()
    {
        notesManager.NotesTime.RemoveAt(0);
        notesManager.LaneNum.RemoveAt(0);
        notesManager.NoteType.RemoveAt(0);
    }

    // 判定を表示する(修正版)
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

        // MessageObj配列が正しく設定されているかチェック
        if (MessageObj != null && judge < MessageObj.Length && MessageObj[judge] != null)
        {
            MessageObj[judge].text = judgecnt[judge].ToString();
        }
        else
        {
            Debug.LogWarning($"MessageObj[{judge}]が設定されていません");
        }

        MessageObj[3].text = judgecnt[3].ToString();
    }
}