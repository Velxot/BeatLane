using UnityEngine;

public class MusicManager : MonoBehaviour
{
    private AudioSource audio;
    private AudioClip Music;
    private string songName;
   

    // NotesManagerの参照
    [SerializeField] private NotesManager notesManager;

    [SerializeField] private Judge judge; // Judge の参照を追加

    // 楽曲が再生開始された時刻
    public float MusicStartTime { get; private set; }

    // 楽曲が再生中かどうか
    public bool IsPlaying { get; private set; }

    void Start()
    {
        audio = GetComponent<AudioSource>();

        // ハードコーディング: 曲名を直接指定
        songName = "狂喜蘭舞";

        // 音楽ファイルを読み込み
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
    }

    void Update()
    {
        // Enterキーでゲーム開始
        if (Input.GetKeyDown(KeyCode.Return) && !IsPlaying)
        {
            // NotesManagerにノーツ生成を指示
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

            // 楽曲を再生
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
}