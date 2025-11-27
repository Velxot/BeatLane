// LongNote.cs
using UnityEngine;

public class LongNote : MonoBehaviour
{
    // ノーツの移動速度 (LongNotesManagerから設定される)
    public float notesSpeed = 15.0f;

    // ロングノーツの開始と終了の時刻 (楽曲開始からの相対秒数)
    [HideInInspector] public float startTargetTime;
    [HideInInspector] public float endTargetTime;

    // どのレーンか
    [HideInInspector] public int lane;

    // 楽曲開始時のTime.timeの値
    [HideInInspector] public float musicStartTime;

    // ゲームが開始されているか
    [HideInInspector] public bool isGameStarted = false;

    // 判定ラインのZ座標
    private const float JUDGELINE_Z = 5.1f;

    private float totalZLength; // 予め計算したロングノーツのZ軸上の長さ

    void Start()
    {
        // ノーツの長さを計算 (LongNotesManagerで設定したスケール値に合わせる)
        // プレハブのZスケールが1.0の場合、totalZLength は LongNotesManager で設定された transform.localScale.z と等しい
        totalZLength = transform.localScale.z;
    }

    void Update()
    {
        if (!isGameStarted) return;

        // 楽曲開始からの経過時間
        float elapsedTime = Time.time - musicStartTime;

        // ロングノーツの中心が判定線に到達する時刻 (開始時刻 + 継続時間/2)
        float centerTargetTime = startTargetTime + (endTargetTime - startTargetTime) / 2f;

        // ロングノーツの中心が判定線に到達するまでの残り時間
        float timeRemaining = centerTargetTime - elapsedTime;

        // ロングノーツの中心のZ座標を計算: (残り時間) × (速度) + (判定線のZ座標)
        float currentCenterZ = timeRemaining * notesSpeed + JUDGELINE_Z;

        // ロングノーツの新しい位置を設定（X, Yは変更しない）
        transform.position = new Vector3(transform.position.x, transform.position.y, currentCenterZ);

        // 【視覚的な修正】終点が判定線Z=5.1を通り過ぎたら消滅
        // 終点のZ座標 = ノーツ中心のZ座標 - (ノーツの長さ/2)
        float currentEndZ = currentCenterZ - (totalZLength / 2f);

        if (currentEndZ < JUDGELINE_Z - 0.5f)
        {
            // 注意: Judge.csでMiss判定と同時にDestroyする処理を実装すれば、この行は不要になる場合があります。
            // Miss判定とDestroyのタイミングはJudge.csに任せるのが一般的です。
            // 今回はLongNotesManagerとJudgeの連携が未実装のため、一時的に残します。
            // Destroy(gameObject); 
        }
    }
}