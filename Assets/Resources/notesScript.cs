// notesScript.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class notes : MonoBehaviour
{
    // ノーツの移動速度 (NotesManagerから設定される)
    public float notesSpeed = 15.0f;

    // ノーツが判定線(Z=0)に到達する時刻 (楽曲開始からの相対秒数)
    [HideInInspector] public float targetTime;

    // 楽曲開始時のTime.timeの値
    [HideInInspector] public float musicStartTime;

    // ゲームが開始されているか (MusicManagerから設定される)
    [HideInInspector] public bool isGameStarted = false;

    // 判定ラインのZ座標 (Z=0と仮定)
    private const float JUDGELINE_Z = 5.1f;

    void Update()
    {
        // ゲームが開始されるまでノーツは動かしません
        if (!isGameStarted) return;

        // 楽曲開始からの経過時間
        float elapsedTime = Time.time - musicStartTime;

        // ノーツが判定線に到達するまでの残り時間
        float timeRemaining = targetTime - elapsedTime;

        // ノーツのZ座標を計算: (残り時間) × (速度) + (判定線のZ座標)
        // ノーツは、timeRemaining秒後に判定線に到達します。
        float currentZ = timeRemaining * notesSpeed + JUDGELINE_Z;

        // ノーツの新しい位置を設定（X, Yは変更しない）
        transform.position = new Vector3(transform.position.x, transform.position.y, currentZ);

        // 【視覚的な修正】ノーツが判定線よりも手前に来たら消滅させる (Judge.csのMiss判定と同時にノーツをDestroyする処理があれば、この行は不要になる場合がありますが、一時的な視覚修正として残します)
        if (currentZ < JUDGELINE_Z - 0.5f) // 判定線Z=0を通り過ぎた場合
        {
            Destroy(gameObject);
        }
    }
}