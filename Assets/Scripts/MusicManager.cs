using UnityEngine;
using System.Collections;
using TMPro; // ★ TextMeshProUGUIを使うために必要

public class MusicManager : MonoBehaviour
{
    // ... 既存の private fields ...
    private AudioSource audio;
    private AudioClip Music;
    private string songName;

    // NotesManagerの参照
    [SerializeField] private NotesManager notesManager;
    [SerializeField] private Judge judge;
    [SerializeField] SongDataBase database;

    // ★ カウントダウン表示用のフィールドを追加
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Game Start Settings")]
    [SerializeField] private float StartDelaySeconds = 3.0f;

    // 楽曲が再生開始された時刻
    public float MusicStartTime { get; private set; }
    public bool IsPlaying { get; private set; }

    void Start()
    {
        // ... 既存の Start 処理（AudioSource, Music読み込み） ...
        audio = GetComponent<AudioSource>();
        songName = database.songData[SongSelect.select].songName;
        Music = (AudioClip)Resources.Load("Musics/" + songName);

        if (Music != null)
        {
            audio.clip = Music;
            Debug.Log($"音楽ファイル読み込み成功: {songName}");
        }
        else
        {
            Debug.LogError($"音楽ファイルが見つかりません: Musics/{songName}");
        }
        IsPlaying = false;

        // ★ カウントダウン表示を初期化
        if (countdownText != null)
        {
            // まずは非表示にするか、初期テキストを設定
            countdownText.text = "";
        }

        // ゲーム開始処理をコルーチンで呼び出す
        StartCoroutine(StartGameWithDelay(StartDelaySeconds));
    }

    void Update()
    {
        // (Enterキーによる開始処理は削除済み)
    }

    // 遅延後にゲーム開始処理を行うコルーチン
    private IEnumerator StartGameWithDelay(float delay)
    {
        // UIが設定されている場合、カウントダウンを開始
        if (countdownText != null)
        {
            // カウントダウン開始
            for (int i = Mathf.FloorToInt(delay); i > 0; i--)
            {
                // 現在の秒数を表示
                countdownText.text = i.ToString();

                // 1秒待機
                yield return new WaitForSeconds(1.0f);
            }

            // カウントダウン終了後のテキスト (例: GO!)
            countdownText.text = "GO!";
            // 最後の「GO!」表示を短時間だけ維持
            yield return new WaitForSeconds(0.5f);

            // 最後にテキストを非表示にする
            countdownText.text = "";
        }
        else // UIが設定されていない場合、単に全体の遅延時間だけ待つ
        {
            yield return new WaitForSeconds(delay);
        }

        // --- ゲーム開始処理 (ノーツ生成、楽曲再生) ---

        // ノーツ生成と初期化
        if (notesManager != null)
        {
            notesManager.GenerateNotes();
            if (judge != null)
            {
                judge.InitGameData();
            }
        }
        else
        {
            Debug.LogError("NotesManagerが設定されていません");
        }

        // 楽曲を再生し、ノーツの移動を開始
        if (audio != null && Music != null)
        {
            MusicStartTime = Time.time;
            audio.Play();
            IsPlaying = true;
            Debug.Log("楽曲再生開始");

            if (notesManager != null)
            {
                notesManager.StartNotesMovement(MusicStartTime);
            }
        }
        else
        {
            Debug.LogError("AudioSourceまたはAudioClipが設定されていません");
        }
    }
}