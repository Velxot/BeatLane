using UnityEngine;
using SharpDX.DirectInput;
using DInput = SharpDX.DirectInput;

public class NEMSYSControllerInput : MonoBehaviour
{
    private DInput.DirectInput directInput;
    private DInput.Joystick joystick;

    // ボタンの状態を保持（前フレームとの比較用）
    private bool[] previousButtonStates = new bool[10];
    private bool[] currentButtonStates = new bool[10];

    // つまみの位置
    public int KnobLeftValue { get; private set; }
    public int KnobRightValue { get; private set; }

    // 初期化が成功したかどうか
    public bool IsInitialized { get; private set; }

    void Start()
    {
        InitializeController();
    }

    void InitializeController()
    {
        try
        {
            directInput = new DInput.DirectInput();

            // コントローラーを検索
            System.Guid joystickGuid = System.Guid.Empty;

            foreach (var deviceInstance in directInput.GetDevices(DInput.DeviceType.Joystick, DInput.DeviceEnumerationFlags.AttachedOnly))
            {
                Debug.Log($"見つかったデバイス: {deviceInstance.InstanceName}");

                // NEMSYS コントローラーを探す
                if (deviceInstance.InstanceName.Contains("SOUND VOLTEX") ||
                    deviceInstance.InstanceName.Contains("Konami"))
                {
                    joystickGuid = deviceInstance.InstanceGuid;
                    Debug.Log($"NEMSYSコントローラーを検出: {deviceInstance.InstanceName}");
                    break;
                }
            }

            if (joystickGuid == System.Guid.Empty)
            {
                Debug.LogWarning("NEMSYSコントローラーが見つかりませんでした。キーボード入力にフォールバックします。");
                IsInitialized = false;
                return;
            }

            // ジョイスティックを初期化
            joystick = new DInput.Joystick(directInput, joystickGuid);
            joystick.Acquire();

            // バッファサイズの設定はAcquire前に行う必要があるため削除

            IsInitialized = true;
            Debug.Log("NEMSYSコントローラー初期化完了！");
        }
        catch (SharpDX.SharpDXException e)
        {
            Debug.LogError($"コントローラー初期化エラー: {e.Message}");
            IsInitialized = false;
        }
    }

    void Update()
    {
        if (!IsInitialized || joystick == null)
            return;

        try
        {
            joystick.Poll();
            var state = joystick.GetCurrentState();

            // ボタンの状態を更新
            for (int i = 0; i < state.Buttons.Length && i < currentButtonStates.Length; i++)
            {
                previousButtonStates[i] = currentButtonStates[i];
                currentButtonStates[i] = state.Buttons[i];
            }

            // つまみの値を更新（X軸とY軸）
            KnobLeftValue = state.X;
            KnobRightValue = state.Y;
        }
        catch (SharpDX.SharpDXException e)
        {
            Debug.LogWarning($"コントローラー読み取りエラー: {e.Message}");
            joystick = null;
            IsInitialized = false;
        }
    }

    // ボタンが押された瞬間を検出
    public bool GetButtonDown(int buttonIndex)
    {
        if (!IsInitialized || buttonIndex < 0 || buttonIndex >= currentButtonStates.Length)
            return false;

        return currentButtonStates[buttonIndex] && !previousButtonStates[buttonIndex];
    }

    // ボタンが押されているかを検出
    public bool GetButton(int buttonIndex)
    {
        if (!IsInitialized || buttonIndex < 0 || buttonIndex >= currentButtonStates.Length)
            return false;

        return currentButtonStates[buttonIndex];
    }

    // ボタンが離された瞬間を検出
    public bool GetButtonUp(int buttonIndex)
    {
        if (!IsInitialized || buttonIndex < 0 || buttonIndex >= currentButtonStates.Length)
            return false;

        return !currentButtonStates[buttonIndex] && previousButtonStates[buttonIndex];
    }

    void OnDestroy()
    {
        if (joystick != null)
        {
            joystick.Unacquire();
            joystick.Dispose();
        }

        if (directInput != null)
        {
            directInput.Dispose();
        }
    }
}