using UnityEngine;

public class LaneEffect : MonoBehaviour
{
    // レーンごとのエフェクトオブジェクト（MeshRendererまたはSpriteRendererを持つオブジェクト）
    [SerializeField] private GameObject[] laneEffects; // 4つのレーン分

    // エフェクトの色
    private Color perfectColor = new Color(1f, 0.84f, 0f, 1f); // 金色
    private Color okColor = new Color(0f, 0.5f, 1f, 1f); // 青色
    private Color transparentColor = new Color(1f, 1f, 1f, 0f); // 透明

    // エフェクトの持続時間（秒）
    [SerializeField] private float effectDuration = 0.2f;

    // 各レーンのエフェクトタイマー
    private float[] effectTimers = new float[4];
    private Color[] currentColors = new Color[4];

    void Start()
    {
        // 初期化：全レーンを透明に
        for (int i = 0; i < 4; i++)
        {
            effectTimers[i] = 0f;
            currentColors[i] = transparentColor;

            if (i < laneEffects.Length && laneEffects[i] != null)
            {
                SetEffectColor(i, transparentColor);
            }
        }
    }

    void Update()
    {
        // 各レーンのエフェクトタイマーを更新
        for (int i = 0; i < 4; i++)
        {
            if (effectTimers[i] > 0)
            {
                effectTimers[i] -= Time.deltaTime;

                // フェードアウト効果
                float alpha = effectTimers[i] / effectDuration;
                Color fadeColor = currentColors[i];
                fadeColor.a = alpha;

                if (i < laneEffects.Length && laneEffects[i] != null)
                {
                    SetEffectColor(i, fadeColor);
                }

                // タイマー終了時に完全に透明に
                if (effectTimers[i] <= 0)
                {
                    SetEffectColor(i, transparentColor);
                }
            }
        }
    }

    // レーンエフェクトを発動（外部から呼び出し）
    public void TriggerEffect(int laneNum, int judgeType)
    {
        // レーン番号を配列インデックスに変換（レーン2-5 → インデックス0-3）
        int index = laneNum - 2;

        if (index < 0 || index >= 4 || index >= laneEffects.Length)
        {
            Debug.LogWarning($"無効なレーン番号: {laneNum}");
            return;
        }

        if (laneEffects[index] == null)
        {
            Debug.LogWarning($"レーン{laneNum}のエフェクトオブジェクトが設定されていません");
            return;
        }

        // 判定に応じて色を設定
        Color targetColor;
        if (judgeType == 0) // Perfect
        {
            targetColor = perfectColor;
        }
        else if (judgeType == 1) // OK
        {
            targetColor = okColor;
        }
        else
        {
            return; // Missは光らせない
        }

        // エフェクト開始
        currentColors[index] = targetColor;
        effectTimers[index] = effectDuration;
        SetEffectColor(index, targetColor);
    }

    // エフェクトオブジェクトの色を設定
    private void SetEffectColor(int index, Color color)
    {
        if (laneEffects[index] == null) return;

        // SpriteRendererを試す
        SpriteRenderer spriteRenderer = laneEffects[index].GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
            return;
        }

        // MeshRendererを試す（3Dオブジェクトの場合）
        MeshRenderer meshRenderer = laneEffects[index].GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.material.color = color;
            return;
        }

        // Imageコンポーネントを試す（UI要素の場合）
        UnityEngine.UI.Image image = laneEffects[index].GetComponent<UnityEngine.UI.Image>();
        if (image != null)
        {
            image.color = color;
            return;
        }
    }
}