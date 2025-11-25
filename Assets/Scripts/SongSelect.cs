using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections; // ★ コルーチンを使うために必要

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
    [SerializeField] float animationDuration = 0.1f; // 拡大・縮小にかける時間
    [SerializeField] float targetScale = 1.3f;       // 拡大後のスケール倍率

    AudioSource audio;
    AudioClip Music;
    string songName;

    public static int select;

    private void Start()
    {
        select = 0;
        audio = GetComponent<AudioSource>();
        songName = dataBase.songData[select].songName;//new
        Music = (AudioClip)Resources.Load("Musics/" + songName);
        SongUpdateALL();
    }

    void Update()
    {
        // 楽曲リストの総数
        int songCount = dataBase.songData.Length;

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            // 循環ロジック: 次へ
            select = (select + 1) % songCount;

            SEaudio.PlayOneShot(songchangeClip);
            SongUpdateALL();

            // ★ DownArrowが押されたら、下の矢印をアニメーション
            if (downArrow != null)
            {
                StartCoroutine(AnimateArrow(downArrow));
            }
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            // 循環ロジック: 前へ (負の値を避けるための + songCount)
            select = (select - 1 + songCount) % songCount;

            SEaudio.PlayOneShot(songchangeClip);
            SongUpdateALL();

            // ★ UpArrowが押されたら、上の矢印をアニメーション
            if (upArrow != null)
            {
                StartCoroutine(AnimateArrow(upArrow));
            }
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            SongStart();
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
        // 循環を考慮した楽曲データのインデックス
        int displayIndex = (select + id + songCount) % songCount;
        // TextMeshProUGUI配列のインデックス (0～4)
        int textIndex = id + 2;

        // textIndex が songNameText の配列範囲内であることを確認
        if (textIndex >= 0 && textIndex < songNameText.Length)
        {
            // 参照オブジェクトが null でないか確認 (NullReferenceException対策)
            if (songNameText[textIndex] != null)
            {
                // 楽曲データを表示
                songNameText[textIndex].text = dataBase.songData[displayIndex].songName;
            }
            else
            {
                Debug.LogError($"songNameText[{textIndex}] (id={id}) に参照が設定されていません！");
            }
        }

        if (id == 0) // 選択中の曲
        {
            // 選択中の曲の画像を表示
            if (songImage != null)
            {
                songImage.sprite = dataBase.songData[displayIndex].songImage;
            }
        }
    }

    public void SongStart()
    {
        SceneManager.LoadScene("GameScene");
    }

    // ★ 矢印のアニメーションを実行するコルーチン
    private IEnumerator AnimateArrow(Image arrow)
    {
        if (arrow == null) yield break;

        // アニメーション開始前にスケールをリセット
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