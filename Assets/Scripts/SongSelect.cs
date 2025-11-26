using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class SongSelect : MonoBehaviour
{
    [SerializeField] SongDataBase dataBase;
    [SerializeField] TextMeshProUGUI[] songNameText;
    [SerializeField] Image songImage;

    // ユーザー様追加分
    [SerializeField] private AudioSource SEaudio;
    [SerializeField] private AudioClip songchangeClip;

    // ★ 矢印関連のフィールド
    [SerializeField] Image upArrow;
    [SerializeField] Image downArrow;
    [SerializeField] Image leftArrow;
    [SerializeField] Image rightArrow;

    // ★ アニメーション用のフィールド
    [Header("Arrow Animation Settings")]
    [SerializeField] float animationDuration = 0.1f;
    [SerializeField] float targetScale = 1.3f;

    // ★ NEMSYS コントローラー
    [Header("NEMSYS Controller Settings")]
    [SerializeField] private NEMSYSControllerInput nemsysController;
    [SerializeField] private float knobThreshold = 5000f; // ツマミの回転検知閾値
    [SerializeField] private bool useLeftKnob = true;     // 左ツマミを使用するか
    [SerializeField] private bool useRightKnob = true;    // 右ツマミを使用するか

    private int previousKnobLeftValue;
    private int previousKnobRightValue;

    AudioSource audio;
    AudioClip Music;
    string songName;

    public static int select;

    [Header("Chart Select Settings")]
    [SerializeField] private TextMeshProUGUI chartNameText; // 難易度名表示用 (例: HARD)
    [SerializeField] private TextMeshProUGUI levelText;     // 難易度レベル表示用 (例: 10)

    public static int selectedChartIndex = 0; // 選択中の難易度インデックス (0から始まる)

    private int totalCharts;

    bool isChartChanged;

    private void Start()
    {
        select = 0;
        selectedChartIndex = 0; // 起動時にも難易度をリセット
        audio = GetComponent<AudioSource>();

        // データベースに楽曲がない場合のガード
        if (dataBase.songData.Length > 0)
        {
            songName = dataBase.songData[select].songName;
            Music = (AudioClip)Resources.Load("Musics/" + songName);
            SongUpdateALL(); // 難易度情報もここで更新される
        }

        // ツマミの初期値を取得
        if (nemsysController != null && nemsysController.IsInitialized)
        {
            previousKnobLeftValue = nemsysController.KnobLeftValue;
            previousKnobRightValue = nemsysController.KnobRightValue;
        }
    }

    void Update()
    {
        // 楽曲リストの総数
        int songCount = dataBase.songData.Length;
        if (songCount == 0) return;

        // ★ 難易度変更フラグをリセット
        isChartChanged = false;

        // ★ NEMSYSコントローラーのツマミ入力を処理 (楽曲選択と難易度選択)
        if (nemsysController != null && nemsysController.IsInitialized)
        {
            HandleKnobInput(songCount);
        }

        // キーボード入力 (楽曲選択)
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            select = (select + 1) % songCount;
            selectedChartIndex = 0; // ★ 楽曲変更時に難易度をリセット
            SEaudio.PlayOneShot(songchangeClip);
            SongUpdateALL();

            if (downArrow != null)
            {
                StartCoroutine(AnimateArrow(downArrow));
            }
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            select = (select - 1 + songCount) % songCount;
            selectedChartIndex = 0; // ★ 楽曲変更時に難易度をリセット
            SEaudio.PlayOneShot(songchangeClip);
            SongUpdateALL();

            if (upArrow != null)
            {
                StartCoroutine(AnimateArrow(upArrow));
            }
        }

        // キーボード入力 (難易度選択)
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            selectedChartIndex--;
            isChartChanged = true;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            selectedChartIndex++;
            isChartChanged = true;
        }

        // ★ 難易度変更処理 (キーボードまたはツマミ入力の結果を処理)
        HandleChartSelectionLogic();


        // 決定ボタン
        if (Input.GetKeyDown(KeyCode.Return) || (nemsysController != null && nemsysController.GetButtonDown(8)))
        {
            SongStart();
        }

        // ★ NEMSYSコントローラーのボタン入力（STARTボタンで決定）
        if (nemsysController != null && nemsysController.GetButtonDown(6)) // ボタン6はSTARTボタン
        {
            SongStart();
        }
    }

    private void HandleChartSelectionLogic()
    {
        if (dataBase.songData.Length == 0) return;

        SongData currentSong = dataBase.songData[select];

        // availableChartsがnullでないことを確認 (Unityエディタでの設定漏れ対策)
        if (currentSong.availableCharts == null)
        {
            currentSong.availableCharts = new List<ChartData>();
        }

        totalCharts = currentSong.availableCharts.Count;

        // 譜面が1つ以下なら、難易度切り替え操作は無視し、インデックスを0に固定 (ただし、totalCharts=0の場合は除く)
        if (totalCharts <= 1)
        {
            if (selectedChartIndex != 0)
            {
                selectedChartIndex = 0;
                UpdateChartInfo(); // インデックスが0以外から0にリセットされたら更新
            }
            return;
        }

        // ツマミまたはキーボードで変更があった場合のみ実行
        if (isChartChanged)
        {
            bool SEflug = true;
            // インデックスをリストの範囲内で留まらせる
            if (selectedChartIndex < 0)
            {
                selectedChartIndex = 0;
                SEflug = false;
            }
            else if (selectedChartIndex >= totalCharts)
            {
                selectedChartIndex = totalCharts - 1;
                SEflug = false;
            }

            // UIを更新
            UpdateChartInfo();

            // 難易度変更SEを鳴らす
            if (SEaudio != null && songchangeClip != null && SEflug)
            {
                SEaudio.PlayOneShot(songchangeClip);
            }
        }
    }

    public void UpdateChartInfo()
    {
        if (dataBase.songData.Length == 0) return;

        SongData currentSong = dataBase.songData[select];

        // availableChartsがnullでないことを確認 (安全策)
        if (currentSong.availableCharts == null)
        {
            currentSong.availableCharts = new List<ChartData>();
        }

        // 選択中の難易度データ（ChartData）を取得
        // ここで selectedChartIndex が範囲外になることが問題でした。
        if (currentSong.availableCharts.Count > selectedChartIndex && selectedChartIndex >= 0)
        {
            // availableChartsは SongData.cs に定義された ChartData のリストであると仮定
            var currentChart = currentSong.availableCharts[selectedChartIndex]; // ← ★ エラーが発生した可能性のある行

            // UIの表示を更新
            if (chartNameText != null)
            {
                chartNameText.text = currentChart.chartName;
            }
            if (levelText != null)
            {
                levelText.text = currentChart.difficultyLevel.ToString();
            }
        }
        else
        {
            // 選択中の楽曲に設定された譜面がない、またはインデックスが不正な場合の処理
            if (chartNameText != null) chartNameText.text = "NO CHART";
            if (levelText != null) levelText.text = "--";
        }
    }

    // ★ ツマミ入力を処理するメソッド
    private void HandleKnobInput(int songCount)
    {
        int currentLeftValue = nemsysController.KnobLeftValue;
        int currentRightValue = nemsysController.KnobRightValue;

        // 左ツマミの処理 (楽曲選択)
        if (useLeftKnob)
        {
            int leftDelta = currentLeftValue - previousKnobLeftValue;

            // 65535付近での値のラップアラウンドを考慮
            if (leftDelta > 32767)
            {
                leftDelta -= 65535;
            }
            else if (leftDelta < -32767)
            {
                leftDelta += 65535;
            }

            if (leftDelta > knobThreshold)
            {
                // 右回転 → 次の曲へ
                select = (select + 1) % songCount;
                selectedChartIndex = 0; // ★ 楽曲変更時に難易度をリセット
                SEaudio.PlayOneShot(songchangeClip);
                SongUpdateALL();

                if (downArrow != null)
                {
                    StartCoroutine(AnimateArrow(downArrow));
                }

                previousKnobLeftValue = currentLeftValue;
            }
            else if (leftDelta < -knobThreshold)
            {
                // 左回転 → 前の曲へ
                select = (select - 1 + songCount) % songCount;
                selectedChartIndex = 0; // ★ 楽曲変更時に難易度をリセット
                SEaudio.PlayOneShot(songchangeClip);
                SongUpdateALL();

                if (upArrow != null)
                {
                    StartCoroutine(AnimateArrow(upArrow));
                }

                previousKnobLeftValue = currentLeftValue;
            }
        }

        // 右ツマミの処理 (難易度選択)
        if (useRightKnob)
        {
            int rightDelta = currentRightValue - previousKnobRightValue;

            // 65535付近での値のラップアラウンドを考慮
            if (rightDelta > 32767)
            {
                rightDelta -= 65535;
            }
            else if (rightDelta < -32767)
            {
                rightDelta += 65535;
            }


            if (rightDelta > knobThreshold)
            {
                // 右回転 → 難易度上昇
                selectedChartIndex++;
                isChartChanged = true;

                previousKnobRightValue = currentRightValue;
            }
            else if (rightDelta < -knobThreshold)
            {
                // 左回転 → 難易度減少
                selectedChartIndex--;
                isChartChanged = true;

                previousKnobRightValue = currentRightValue;
            }
        }
    }

    private void SongUpdateALL()
    {
        if (dataBase.songData.Length == 0) return;

        songName = dataBase.songData[select].songName;
        Music = (AudioClip)Resources.Load("Musics/" + songName);
        audio.Stop();

        if (Music != null)
        {
            audio.PlayOneShot(Music);
        }

        for (int i = 0; i < 5; i++)
        {
            SongUpdate(i - 2);
        }

        // 楽曲変更時に難易度情報を更新
        // (selectが変更された後、selectedChartIndexが0にリセットされている前提)
        UpdateChartInfo();
    }

    private void SongUpdate(int id)
    {
        int songCount = dataBase.songData.Length;
        int displayIndex = (select + id + songCount) % songCount;
        int textIndex = id + 2;

        if (textIndex >= 0 && textIndex < songNameText.Length)
        {
            if (songNameText[textIndex] != null)
            {
                songNameText[textIndex].text = dataBase.songData[displayIndex].songName;
            }
            else
            {
                Debug.LogError($"songNameText[{textIndex}] (id={id}) に参照が設定されていません！");
            }
        }

        if (id == 0)
        {
            if (songImage != null)
            {
                songImage.sprite = dataBase.songData[displayIndex].songImage;
            }
        }
    }

    public void SelectChart(int chartIndex)
    {
        // 外部からの難易度選択処理 (主にUIボタン用)
        selectedChartIndex = chartIndex;
        // isChartChangedをtrueにする必要はないが、範囲チェックと更新は必要
        HandleChartSelectionLogic();
        Debug.Log($"楽曲ID: {select}, 選択された譜面インデックス: {selectedChartIndex}");
    }

    public void SongStart()
    {
        SceneManager.LoadScene("GameScene");
    }

    private IEnumerator AnimateArrow(Image arrow)
    {
        if (arrow == null) yield break;

        arrow.rectTransform.localScale = Vector3.one;

        Vector3 startScale = Vector3.one;
        Vector3 endScale = Vector3.one * targetScale;
        float time = 0f;
        float duration = animationDuration;

        // Phase 1: 拡大アニメーション
        while (time < duration)
        {
            arrow.rectTransform.localScale = Vector3.Lerp(startScale, endScale, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        arrow.rectTransform.localScale = endScale;

        // Phase 2: 縮小アニメーション
        time = 0f;
        while (time < duration)
        {
            arrow.rectTransform.localScale = Vector3.Lerp(endScale, startScale, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        arrow.rectTransform.localScale = startScale;
    }
}