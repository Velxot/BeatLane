using UnityEngine;
using TMPro;

public class Judge : MonoBehaviour
{
    // プレイヤーに判定を伝えるゲームオブジェクト
    [SerializeField] private TextMeshProUGUI[] MessageObj;

    // NotesManagerを入れる変数
    [SerializeField] private NotesManager notesManager;

    int[] judgecnt = { 0, 0, 0, 0 };
    int score = 0; // 実際のスコア
    float displayScore = 0f; // 表示用のスコア（徐々に増える）
    int targetScore = 0; // 目標スコア
    int scorestandard; // 初期化をStart()で行う

    // スコア加算演出の時間（秒）
    [SerializeField] private float scoreAnimationDuration = 0.3f;

    [SerializeField] private AudioSource judgeSoundSource;

    [SerializeField] private AudioClip perfectClip;

    [SerializeField] private AudioClip okClip;

    void Start()
    {
        // NotesManagerがnoteNumを設定した後に計算
        if (notesManager != null && notesManager.noteNum > 0)
        {
            scorestandard = 1000000 / notesManager.noteNum;
            Debug.Log($"総ノーツ数: {notesManager.noteNum}, 1ノーツあたりのスコア: {scorestandard}");
        }
        else
        {
            Debug.LogError("NotesManagerが設定されていないか、ノーツ数が0です");
            scorestandard = 0;
        }
    }

    void Update()
    {
        // スコア表示の更新（徐々に増やす）
        UpdateScoreDisplay();

        // notesManagerとリストが空でないかチェック
        if (notesManager == null || notesManager.NotesTime.Count == 0)
            return;

        if (Input.GetKeyDown(KeyCode.S)) // Sキーが押されたとき
        {
            CheckNoteHit(2); // レーン2をチェック
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            CheckNoteHit(3); // レーン3をチェック
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            CheckNoteHit(4); // レーン4をチェック
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            CheckNoteHit(5); // レーン5をチェック
        }

        // 本来ノーツをたたくべき時間から0.1秒たっても入力がなかった場合
        // リストの先頭から順にチェック
        if (notesManager.NotesTime.Count > 0 && Time.time > notesManager.NotesTime[0] + 0.1f)
        {
            message(2);
            deleteData(0); // インデックス0を削除
            Debug.Log("Miss");
        }
    }

    // 指定されたレーンのノーツをチェックして判定
    void CheckNoteHit(int lane)
    {
        // 判定可能な範囲内のノーツを探す
        for (int i = 0; i < notesManager.LaneNum.Count; i++)
        {
            // レーンが一致するか確認
            if (notesManager.LaneNum[i] == lane)
            {
                float timeLag = GetABS(Time.time - notesManager.NotesTime[i]);

                // 判定範囲内（0.1秒以内）かチェック
                if (timeLag <= 0.10f)
                {
                    Judgement(timeLag, i); // インデックスも渡す
                    return; // 見つかったら終了
                }
            }
        }

        // 判定範囲内にノーツがない場合は何もしない（空打ち）
        Debug.Log($"レーン{lane}: 判定可能なノーツなし");
    }

    void Judgement(float timeLag, int noteIndex)
    {
        // 本来ノーツをたたくべき時間と実際にノーツをたたいた時間の誤差が0.045秒以下だったら
        if (timeLag <= 0.045)
        {
            Debug.Log("Perfect");
            message(0);
            addScore(0);
            // ★追加: Perfect判定時に効果音を再生
            if (judgeSoundSource != null && perfectClip != null)
            {
                judgeSoundSource.PlayOneShot(perfectClip); // 既存の再生中の音を止めずに効果音を鳴らす
            }
            deleteData(noteIndex);

        }
        else if (timeLag <= 0.10) // 0.10秒以下だったら
        {
            Debug.Log("OK");
            message(1);
            addScore(1);
            if (judgeSoundSource != null && okClip != null)
            {
                judgeSoundSource.PlayOneShot(okClip); // 既存の再生中の音を止めずに効果音を鳴らす
            }
            deleteData(noteIndex);
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
    void deleteData(int index)
    {
        notesManager.NotesTime.RemoveAt(index);
        notesManager.LaneNum.RemoveAt(index);
        notesManager.NoteType.RemoveAt(index);

        // ノーツオブジェクトも削除
        if (index < notesManager.NotesObj.Count && notesManager.NotesObj[index] != null)
        {
            Destroy(notesManager.NotesObj[index]);
            notesManager.NotesObj.RemoveAt(index);
        }
    }

    // 判定を表示する
    void message(int judge)
    {
        judgecnt[judge]++;

        if (judge == 2) // Missの場合
        {
            judgecnt[3] = 0; // コンボをリセット
        }
        else
        {
            judgecnt[3]++; // コンボを加算
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

        // コンボ表示
        if (MessageObj.Length > 3 && MessageObj[3] != null)
        {
            MessageObj[3].text = judgecnt[3].ToString();
        }
    }

    void addScore(int judge)
    {
        if (judge == 0) // Perfect
        {
            score += scorestandard;
        }
        else if (judge == 1) // OK
        {
            score += scorestandard * 3 / 4;
        }

        // 目標スコアを更新（表示スコアは徐々に追いつく）
        targetScore = score;

        Debug.Log($"目標スコア: {targetScore}");
    }

    // スコア表示を徐々に増やす
    void UpdateScoreDisplay()
    {
        // 表示スコアが目標スコアに達していない場合
        if (displayScore < targetScore)
        {
            // 差分を計算
            float difference = targetScore - displayScore;

            // 滑らかに追いつく（差分の一定割合ずつ増やす）
            float increment = difference / scoreAnimationDuration * Time.deltaTime;

            // 最小でも1は増やす（停滞を防ぐ）
            if (increment < 1f)
            {
                increment = Mathf.Min(1f, difference);
            }

            displayScore += increment;

            // 目標スコアを超えないように調整
            if (displayScore > targetScore)
            {
                displayScore = targetScore;
            }

            // スコア表示を更新
            if (MessageObj.Length > 4 && MessageObj[4] != null)
            {
                MessageObj[4].text = Mathf.FloorToInt(displayScore).ToString();
            }

            // デバッグ情報
            //Debug.Log($"表示スコア: {Mathf.FloorToInt(displayScore)} / 目標: {targetScore}");
        }
    }
}