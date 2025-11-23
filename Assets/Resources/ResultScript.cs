using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ResultScript : MonoBehaviour
{
    // MessageObj[0]: スコア
    // MessageObj[1]: Perfect数
    // MessageObj[2]: OK数
    // MessageObj[3]: Miss数

    [SerializeField] private NEMSYSControllerInput nemsysController;

    [SerializeField] private TextMeshProUGUI[] MessageObj;

    // リザルトランク表示用（CLEAR, FULL COMBO, ALL PERFECT）
    [SerializeField] private TextMeshProUGUI resultRankText;

    // 最大コンボ表示用（オプション）
    [SerializeField] private TextMeshProUGUI ComboText;

    void Start()
    {
        DisplayResult();
    }

    void Update()
    {
        // コントローラーが初期化されている場合はコントローラー入力、そうでなければキーボード入力
        bool usesController = nemsysController != null && nemsysController.IsInitialized;

        // スタートボタンまたはEnterキー
        if ((usesController && nemsysController.GetButtonDown(8)) || Input.GetKeyDown(KeyCode.Return))
        {
            Retry();
        }
    }

    void DisplayResult()
    {
        // GameResultDataから各データを取得して表示
        if (MessageObj.Length > 0 && MessageObj[0] != null)
        {
            MessageObj[0].text = GameResultData.Score.ToString();
        }

        if (MessageObj.Length > 1 && MessageObj[1] != null)
        {
            MessageObj[1].text = GameResultData.PerfectCount.ToString();
        }

        if (MessageObj.Length > 2 && MessageObj[2] != null)
        {
            MessageObj[2].text = GameResultData.OkCount.ToString();
        }

        if (MessageObj.Length > 3 && MessageObj[3] != null)
        {
            MessageObj[3].text = GameResultData.MissCount.ToString();
        }

        // リザルトランク表示
        if (resultRankText != null)
        {
            resultRankText.text = GameResultData.ResultRank;
        }

        // 最大コンボ表示（オプション）
        if (ComboText != null)
        {
            ComboText.text =GameResultData.Combo.ToString();
        }

        // デバッグログ
        Debug.Log("=== リザルト画面表示 ===");
        GameResultData.DebugLog();
    }

    // タイトルに戻るボタン用（オプション）
    public void ReturnToTitle()
    {
        SceneManager.LoadScene("TitleScene"); // タイトルシーン名を適宜変更
    }

    // リトライボタン用（オプション）
    public void Retry()
    {
        SceneManager.LoadScene("GameScene"); // ゲームシーン名を適宜変更
    }
}