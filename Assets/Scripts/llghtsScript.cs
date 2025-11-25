using UnityEngine;

public class lightsScript : MonoBehaviour
{
    [SerializeField] private float Speed = 10; // 光の消える速度
    [SerializeField] public int num = 0; // 対応するレーン番号(2-5)

    private Renderer rend;
    private float alfa = 0;

    // 判定ごとの色設定
    private Color perfectColor = new Color(1f, 0.84f, 0f); // 金色
    private Color okColor = new Color(0.3f, 0.6f, 1f); // 青色
    private Color defaultColor = new Color(1f, 1f, 1f); // 白色（デフォルト）

    private Color currentColor;

    void Start()
    {
        rend = GetComponent<Renderer>();
        currentColor = defaultColor;

        // 初期状態を透明にする
        rend.material.color = new Color(currentColor.r, currentColor.g, currentColor.b, 0f);
    }

    void Update()
    {
        // 透明度が0より大きい場合、徐々に減らす
        if (alfa > 0)
        {
            alfa -= Speed * Time.deltaTime;

            if (alfa < 0)
            {
                alfa = 0;
            }

            // 現在の色に透明度を適用
            rend.material.color = new Color(currentColor.r, currentColor.g, currentColor.b, alfa);
        }
    }

    // 外部から呼び出す用：判定に応じて光らせる
    public void LightUp(int judgeType)
    {
        // 判定に応じて色を設定
        if (judgeType == 0) // Perfect
        {
            currentColor = perfectColor;
            alfa = 0.8f; // Perfectは明るく
        }
        else if (judgeType == 1) // OK
        {
            currentColor = okColor;
            alfa = 0.6f; // OKは少し暗め
        }
        else if (judgeType == 2) // 空打ち
        {
            currentColor = defaultColor;
            alfa = 0.3f; // 空打ちは控えめに
        }
        else // その他（Miss時は光らせない想定）
        {
            currentColor = defaultColor;
            alfa = 0.3f;
        }

        // 色を即座に反映
        rend.material.color = new Color(currentColor.r, currentColor.g, currentColor.b, alfa);
    }
}