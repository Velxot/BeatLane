using UnityEngine;

public class lightsScript : MonoBehaviour
{
    [SerializeField] private float Speed = 10; // ���̏����鑬�x
    [SerializeField] public int num = 0; // �Ή����郌�[���ԍ�(2-5)

    private Renderer rend;
    private float alfa = 0;

    // ���育�Ƃ̐F�ݒ�
    private Color perfectColor = new Color(1f, 0.84f, 0f); // ���F
    private Color okColor = new Color(0.3f, 0.6f, 1f); // �F
    private Color defaultColor = new Color(1f, 1f, 1f); // ���F�i�f�t�H���g�j

    private Color currentColor;

    void Start()
    {
        rend = GetComponent<Renderer>();
        currentColor = defaultColor;

        // ������Ԃ𓧖��ɂ���
        rend.material.color = new Color(currentColor.r, currentColor.g, currentColor.b, 0f);
    }

    void Update()
    {
        // �����x��0���傫���ꍇ�A���X�Ɍ��炷
        if (alfa > 0)
        {
            alfa -= Speed * Time.deltaTime;

            if (alfa < 0)
            {
                alfa = 0;
            }

            // ���݂̐F�ɓ����x��K�p
            rend.material.color = new Color(currentColor.r, currentColor.g, currentColor.b, alfa);
        }
    }

    // �O������Ăяo���p�F����ɉ����Č��点��
    public void LightUp(int judgeType)
    {
        // --- 修正ポイント：Rendererのヌルチェックと再取得 ---
    if (rend == null) rend = GetComponent<Renderer>();
    if (rend == null) return; // それでも取れなければ何もしない
        // ����ɉ����ĐF��ݒ�
        if (judgeType == 0) // Perfect
        {
            currentColor = perfectColor;
            alfa = 0.8f; // Perfect�͖��邭
        }
        else if (judgeType == 1) // OK
        {
            currentColor = okColor;
            alfa = 0.6f; // OK�͏����Â�
        }
        else if (judgeType == 2) // ��ł�
        {
            currentColor = defaultColor;
            alfa = 0.3f; // ��ł��͍T���߂�
        }
        else // ���̑��iMiss���͌��点�Ȃ��z��j
        {
            currentColor = defaultColor;
            alfa = 0.3f;
        }

        // 色を即座に反映
        rend.material.color = new Color(currentColor.r, currentColor.g, currentColor.b, alfa);
    }
}