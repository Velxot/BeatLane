using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Judge : MonoBehaviour
{
    // プレイヤーに判定を伝えるゲームオブジェクト
    [SerializeField] private TextMeshProUGUI[] MessageObj;

    // NotesManagerを入れる変数
    [SerializeField] private NotesManager notesManager;

    //スライダーを入れる変数
    [SerializeField] private Slider slider;

    // 各レーンのlightsScriptを入れる変数（8レーン分）
    [SerializeField] private lightsScript[] laneLights; // インデックス0-7がレーン0-7に対応

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

    // ゲージテキストのRectTransformを直接取得 (MessageObj[5]がこれに相当)
    // Inspectorでこのテキストオブジェクトを直接アサインしてください。
    [SerializeField] private RectTransform percentTextRect;

    // ゲージ端からのテキストのオフセット（表示調整用）
    [SerializeField] private float gaugeTextOffset = 10f; // Inspectorで調整してください

    int laneposition;

    void Start()
    {
        slider.maxValue = 100f;
        slider.value = 0f;

        laneposition = 2;

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
        // ゲージテキストの位置と表示を毎フレーム更新
        UpdateGaugeTextPosition();

        // notesManagerとリストが空でないかチェック
        if (notesManager == null || notesManager.NotesTime.Count == 0)
            return;

        if (Input.GetKeyDown(KeyCode.S)) // Sキーが押されたとき
        {
            CheckNoteHit(laneposition); // レーン2をチェック
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            CheckNoteHit(laneposition + 1); // レーン3をチェック
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            CheckNoteHit(laneposition + 2); // レーン4をチェック
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            CheckNoteHit(laneposition + 3); // レーン5をチェック
        }
        if (laneposition>0 && Input.GetKeyDown(KeyCode.E))
        {
            laneposition--;
        }
        if (laneposition < 4 && Input.GetKeyDown(KeyCode.I))
        {
            laneposition++;
        }

        // 本来ノーツをたたくべき時間から0.1秒たっても入力がなかった場合
        // リストの先頭から順にチェック
        if (notesManager.NotesTime.Count > 0 && Time.time > notesManager.NotesTime[0] + 0.1f)
        {
            message(2);
            deleteData(0); // インデックス0を削除
            Debug.Log("Miss");
            slider.value -= 1.0f;
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
                    Judgement(timeLag, i, lane); // レーン番号も渡す
                    return; // 見つかったら終了
                }
            }
        }

        // 判定範囲内にノーツがない場合は空打ち（白く光る）
        Debug.Log($"レーン{lane}: 空打ち");
        TriggerLaneLight(lane, 2); // 2は空打ちを表す
    }

    void Judgement(float timeLag, int noteIndex, int lane)
    {
        // 本来ノーツをたたくべき時間と実際にノーツをたたいた時間の誤差が0.045秒以下だったら
        if (timeLag <= 0.045)
        {
            Debug.Log("Perfect");
            message(0);
            addScore(0);
            slider.value += 2.5f;

            // Perfect判定時に効果音を再生
            if (judgeSoundSource != null && perfectClip != null)
            {
                judgeSoundSource.PlayOneShot(perfectClip);
            }

            // レーンを光らせる（Perfect = 金色）
            TriggerLaneLight(lane, 0);

            deleteData(noteIndex);
        }
        else if (timeLag <= 0.10) // 0.10秒以下だったら
        {
            Debug.Log("OK");
            message(1);
            addScore(1);
            slider.value += 2.5f;

            // OK判定時に効果音を再生
            if (judgeSoundSource != null && okClip != null)
            {
                judgeSoundSource.PlayOneShot(okClip);
            }

            // レーンを光らせる（OK = 青色）
            TriggerLaneLight(lane, 1);

            deleteData(noteIndex);
        }
    }

    // レーンライトを発動
    void TriggerLaneLight(int laneNum, int judgeType)
    {
        // レーン番号をそのまま配列インデックスとして使用（レーン0-7 → インデックス0-7）
        if (laneLights != null && laneNum >= 0 && laneNum < laneLights.Length && laneLights[laneNum] != null)
        {
            laneLights[laneNum].LightUp(judgeType);
        }
        else
        {
            Debug.LogWarning($"レーン{laneNum}のlightsScriptが設定されていません");
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
        }
    }

    void UpdateGaugeTextPosition()
    {
        if (slider == null || percentTextRect == null)
            return;

        // スライダーのRectTransformを取得
        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        if (sliderRect == null)
            return;

        // スライダーの正規化された値 (0.0f to 1.0f)
        float normalizedValue = slider.value / slider.maxValue;

        // スライダーのFill領域の幅を計算
        float fillWidth = sliderRect.rect.width * normalizedValue;

        // スライダーのRectTransformの左端のローカルX座標を計算
        // (UI要素の中心が(0,0)にあるというUnityのデフォルト設定を仮定)
        float sliderLeftX = sliderRect.localPosition.x - (sliderRect.rect.width / 2f);

        // テキストの新しいX座標を計算
        // = スライダーの左端 + 塗りつぶされた幅 - オフセット
        float newX = sliderLeftX + fillWidth - gaugeTextOffset;

        // Y座標は現在の値を維持
        float currentY = percentTextRect.localPosition.y;

        // テキストの位置を更新
        percentTextRect.localPosition = new Vector3(newX, currentY, percentTextRect.localPosition.z);

        // パーセント表示の更新
        // MessageObj[5]がパーセントテキストであると仮定
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
}