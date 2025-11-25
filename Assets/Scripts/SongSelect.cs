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

    public static int selectedChartIndex = 0;

    private int totalCharts;

    private void Start()
    {
        select = 0;
        audio = GetComponent<AudioSource>();
        songName = dataBase.songData[select].songName;
        Music = (AudioClip)Resources.Load("Musics/" + songName);
        SongUpdateALL();

        UpdateChartInfo();

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

        // ★ NEMSYSコントローラーのツマミ入力を処理
        if (nemsysController != null && nemsysController.IsInitialized)
        {
            HandleKnobInput(songCount);
        }

        // キーボード入力
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            select = (select + 1) % songCount;
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
            SEaudio.PlayOneShot(songchangeClip);
            SongUpdateALL();

            if (upArrow != null)
            {
                StartCoroutine(AnimateArrow(upArrow));
            }
        }

        if (Input.GetKeyDown(KeyCode.Return) || (nemsysController.GetButtonDown(8)))
        {
            SongStart();
        }

        // ★ NEMSYSコントローラーのボタン入力（STARTボタンで決定）
        if (nemsysController != null && nemsysController.GetButtonDown(6)) // ボタン6はSTARTボタン
        {
            SongStart();
        }

        HandleChartSelectionInput();
    }

    private void HandleChartSelectionInput()
    {
        if (dataBase.songData.Length == 0) return;

        SongData currentSong = dataBase.songData[select];
        totalCharts = currentSong.availableCharts.Count;
        if (totalCharts <= 1) return; // 譜面が1つ以下なら切り替え不要

        int currentChartIndex = selectedChartIndex;
        bool isChartChanged = false;

        // --- キーボード入力 ---
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentChartIndex--;
            isChartChanged = true;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentChartIndex++;
            isChartChanged = true;
        }

        // --- コントローラーボタン入力 (例: BT-A/BT-BやFX-L/FX-R) ---
        // どのボタンを難易度切り替えに使うかによってインデックスを調整してください
        // 仮にボタン0とボタン1を使うとします (NEMSYSControllerInput.csを参照)
        if (nemsysController != null && nemsysController.IsInitialized)
        {
            // 仮にボタン0 (左) で難易度を下げる
            if (nemsysController.GetButtonDown(0))
            {
                currentChartIndex--;
                isChartChanged = true;
            }
            // 仮にボタン1 (右) で難易度を上げる
            if (nemsysController.GetButtonDown(1))
            {
                currentChartIndex++;
                isChartChanged = true;
            }
        }

        // --- 難易度インデックスのループ処理 ---
        if (isChartChanged)
        {
            // インデックスをリストの範囲内でループさせる
            if (currentChartIndex < 0)
            {
                currentChartIndex = totalCharts - 1; // 左端から右端へ
            }
            else if (currentChartIndex >= totalCharts)
            {
                currentChartIndex = 0; // 右端から左端へ
            }

            // 新しいインデックスを静的変数に保存し、UIを更新
            selectedChartIndex = currentChartIndex;
            UpdateChartInfo();

            // 難易度変更SEを鳴らす（オプション）
            if (SEaudio != null && songchangeClip != null)
            {
                SEaudio.PlayOneShot(songchangeClip);
            }
        }
    }

    public void UpdateChartInfo()
    {
        if (dataBase.songData.Length == 0) return;

        SongData currentSong = dataBase.songData[select];

        // 選択中の難易度データ（ChartData）を取得
        if (currentSong.availableCharts.Count > selectedChartIndex)
        {
            ChartData currentChart = currentSong.availableCharts[selectedChartIndex];

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
            // 選択中の楽曲に設定された譜面がない場合の処理 (例: デフォルト表示)
            if (chartNameText != null) chartNameText.text = "NO CHART";
            if (levelText != null) levelText.text = "--";
        }
    }

    // ★ ツマミ入力を処理する新しいメソッド
    private void HandleKnobInput(int songCount)
    {
        int currentLeftValue = nemsysController.KnobLeftValue;
        int currentRightValue = nemsysController.KnobRightValue;

        // 左ツマミの処理
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
                SEaudio.PlayOneShot(songchangeClip);
                SongUpdateALL();

                if (upArrow != null)
                {
                    StartCoroutine(AnimateArrow(upArrow));
                }

                previousKnobLeftValue = currentLeftValue;
            }
        }

        // 右ツマミの処理
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
                // 右回転 → 次の曲へ
                select = (select + 1) % songCount;
                SEaudio.PlayOneShot(songchangeClip);
                SongUpdateALL();

                if (downArrow != null)
                {
                    StartCoroutine(AnimateArrow(downArrow));
                }

                previousKnobRightValue = currentRightValue;
            }
            else if (rightDelta < -knobThreshold)
            {
                // 左回転 → 前の曲へ
                select = (select - 1 + songCount) % songCount;
                SEaudio.PlayOneShot(songchangeClip);
                SongUpdateALL();

                if (upArrow != null)
                {
                    StartCoroutine(AnimateArrow(upArrow));
                }

                previousKnobRightValue = currentRightValue;
            }
        }
    }

    private void SongUpdateALL()
    {
        songName = dataBase.songData[select].songName;
        Music = (AudioClip)Resources.Load("Musics/" + songName);
        audio.Stop();
        audio.PlayOneShot(Music);

        for (int i = 0; i < 5; i++)
        {
            SongUpdate(i - 2);
        }
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
        selectedChartIndex = chartIndex;
        // UIに選択中の譜面名などを表示するロジックをここに追加します
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