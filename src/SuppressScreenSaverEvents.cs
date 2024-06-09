using System;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;


public class TrayApp : Form
{
    /**
    <summary>
    タスクトレイに常駐しスクリーンセーバーを抑制します。
    マウスやキーボードを動かしたようにエミュレートすることでスクリーンセーバーの
    抑制をしています。
    たとえばマウスを使った作業中にマウスをエミュレートした場合、作業に影響がでる
    ことが考えられます。
    システム的には推薦される方法ではないと考えますが作業に影響がでないようによう
    に考慮しています。
    ショートカットやバッチファイルを利用して引数に秒数を数値で指定するとイベント
    間隔を指定できます。
    スリープやモニターの電源断などは考慮していません。
    </summary>
    */

    // MessageBoxの2重表示を抑制するためのフラグです
    private static bool IsMessageBoxDisplayed = false;


    private static Timer timer;

    private static NotifyIcon trayIcon;

    private static ContextMenu trayMenu;


    // あとで参考にできるよう未使用の定数も残しておきます
    private const int INPUT_MOUSE = 0;
    private const int INPUT_KEYBOARD = 1;
    private const int INPUT_HARDWARE = 2;

    private const int MOUSEEVENTF_MOVE = 0x1;
    private const int MOUSEEVENTF_ABSOLUTE = 0x8000;

    private const int MOUSEEVENTF_LEFTDOWN = 0x2;
    private const int MOUSEEVENTF_LEFTUP = 0x4;
    private const int MOUSEEVENTF_RIGHTDOWN = 0x8;
    private const int MOUSEEVENTF_RIGHTUP = 0x10;
    private const int MOUSEEVENTF_MIDDLEDOWN = 0x20;
    private const int MOUSEEVENTF_MIDDLEUP = 0x40;
    private const int MOUSEEVENTF_WHEEL = 0x800;

    private const int WHEEL_DELTA = 120;

    private const int KEYEVENTF_KEYDOWN = 0x0;
    private const int KEYEVENTF_EXTENDEDKEY = 0x1;
    private const int KEYEVENTF_KEYUP = 0x2;
    private const int KEYEVENTF_UNICODE = 0x4;
    private const int KEYEVENTF_SCANCODE = 0x8;


    [StructLayout(LayoutKind.Sequential)]
    struct MousePoint
    {
        public int X;
        public int Y;
    }


    // https://learn.microsoft.com/ja-jp/windows/win32/inputdev/virtual-key-codes
    private enum VK : ushort
    {
        A = (ushort)'A',
        RETURN = 0xD,
        SHIFT = 0x10,
        CONTROL = 0x11,
        MENU = 0x12, // ALT
        ALT = 0x12,
        ESCAPE = 0x1B,
        SPACE = 0x20,
        LEFT = 0x25,
        UP = 0x26,
        RIGHT = 0x27,
        DOWN = 0x28,
        PAUSE = 0x13,
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MouseInput
    {
        public int dx; // C++のlongは4ビットのためC#ではintになります
        public int dy; // C++のlongは4ビットのためC#ではintになります
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct HardwareInput
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct InputUnion
    {
        [FieldOffset(0)]
        public MouseInput Mouse;
        [FieldOffset(0)]
        public KeyboardInput Keyboard;
        [FieldOffset(0)]
        public HardwareInput Hardware;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Input
    {
        public int type;
        public InputUnion ui;
    }


    class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = false)]
        public static extern bool GetCursorPos(out MousePoint lpPoint);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        public static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        public static extern UIntPtr GetMessageExtraInfo();
    }


    protected override void OnLoad(EventArgs e)
    {
        Visible = false;
        ShowInTaskbar = false;
        base.OnLoad(e);
    }


    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            timer.Stop();
            timer.Dispose();
            trayIcon.Dispose();
        }
        base.Dispose(isDisposing);
    }


    private static void OnExit(object sender, EventArgs e)
    {
        Application.Exit();
    }


    private static void TrayIcon_Click(object sender, MouseEventArgs e, int TISec)
    {
        if (e.Button == MouseButtons.Left)
        {
            // メッセージの2重表示を抑制します
            if (!IsMessageBoxDisplayed)
            {
                IsMessageBoxDisplayed = true;
                string titleMsg = "SuppressScreenSaver";
                string eventMsg =
                    string.Format(
                        "{0}分{1}秒毎にイベントを発生させて\n" +
                        "スクリーンセーバーを抑制しています\n\n" +
                        "終了するにはOKボタンを押してください",
                        (TISec - (TISec % 60)) / 60,
                        TISec % 60
                    );
                // ダミーのフォームを作成して最前面に表示します
                using (Form dummyForm = new Form())
                {
                    dummyForm.TopMost = true;
                    DialogResult result = MessageBox.Show(
                        dummyForm,
                        eventMsg,
                        titleMsg,
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Asterisk,
                        MessageBoxDefaultButton.Button2
                    );
                    dummyForm.TopMost = false;
                    if (result == DialogResult.OK)
                    {
                        Application.Exit();
                    }
                }
                IsMessageBoxDisplayed = false;
            }
        }
    }


    private static void SuppressScreenSaverOnMouseEvents()
    {
        /**
        <summary>
        マウスをエミュレートしてスクリーンセーバーを抑制します。
        SendInputで移動距離を0にして定期的に実行すると抑制できます。

        スクリーンセーバーの解除は移動距離0ではできません。
        また、マウスによる解除に必要な動作はスクリーンセーバーによって変わります。

        移動距離0で移動させるもっともシンプルな方法は以下の通りです。
        int posX = System.Windows.Forms.Cursor.Position.X;
        int posY = System.Windows.Forms.Cursor.Position.Y;
        System.Windows.Forms.Cursor.Position = new System.Drawing.Point(posX, posY);
        しかしCursor.Positionで座標を変更してもスクリーンセーバーを抑制できません。
        しかもプログラム開始直後に取得したマウスカーソルの現在値にはずれがあるため
        起動直後は位置取得をしないなど、対策をしないと微妙にカーソルが動くことが
        あります。
        挙動確認のために作ったスクリーンセーバーでは対策済みです。

        関数について
        https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput
        https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-mouseinput
        </summary>
        */

        // SendInputの引数を移動量 0 で初期化します
        uint actions = 1;
        Input[] inputs = new Input[actions];
        for (int i = 0; i < inputs.Length; i++)
        {
            inputs[i].type = INPUT_MOUSE;
            inputs[i].ui.Mouse.dwFlags = MOUSEEVENTF_MOVE; // | MOUSEEVENTF_ABSOLUTE;
            inputs[i].ui.Mouse.dx = 0;
            inputs[i].ui.Mouse.dy = 0;
            inputs[i].ui.Mouse.mouseData = 0;
            inputs[i].ui.Mouse.time = 0;
            inputs[i].ui.Mouse.dwExtraInfo = NativeMethods.GetMessageExtraInfo();
        }

        /*
        // マウス座標を左上、右下へ移動し元に戻す例です
        uint actions = 3; // アクション数が3回なので、ここも変更が必要です

        MousePoint mp;
        NativeMethods.GetCursorPos(out mp);
        int posX = mp.X;
        int posY = mp.Y;

        // 右下の座標 (MOUSEEVENTF_ABSOLUTE値のMickey最大値)
        int cornerMickeyX = 65535;
        int cornerMickeyY = 65535;

        // 現在位置をMickeyに変換します
        int posMickeyX = (posX * cornerMickeyX) / Screen.PrimaryScreen.Bounds.Width;
        int posMickeyY = (posY * cornerMickeyY) / Screen.PrimaryScreen.Bounds.Height;

        // アクションを設定します
        inputs[0].ui.Mouse.dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE;
        inputs[1].ui.Mouse.dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE;
        inputs[2].ui.Mouse.dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE;
        inputs[0].ui.Mouse.dx = 0;
        inputs[0].ui.Mouse.dy = 0;
        inputs[1].ui.Mouse.dx = cornerMickeyX;
        inputs[1].ui.Mouse.dy = cornerMickeyY;
        inputs[2].ui.Mouse.dx = posMickeyX;
        inputs[2].ui.Mouse.dy = posMickeyY;
        */

        NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(inputs[0]));
    }


    private static void SuppressScreenSaverOnKeyboardEvents()
    {
        /**
        <summary>
        キーボードをエミュレートしてスクリーンセーバーを抑制します。
        抑制だけなら KEYEVENTF_KEYUP だけでできます。
        スキャンコードを使う方法は細かなキー指定ができないため今回は使いません。

        関数について
        https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput
        https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-keybdinput
        </summary>
        */

        // 送信するキーコードを設定します
        ushort inVKKey = (ushort)VK.PAUSE; // いろいろと影響が少なそうな Pause キー

        // SendInputの引数を初期化します
        uint actions = 1;
        Input[] inputs = new Input[actions];
        for (int i = 0; i < inputs.Length; i++)
        {
            inputs[i].type = INPUT_KEYBOARD;
            inputs[i].ui.Keyboard.VirtualKey = inVKKey;
            inputs[i].ui.Keyboard.dwFlags = KEYEVENTF_KEYUP;
            inputs[i].ui.Keyboard.time = 0;
            inputs[i].ui.Keyboard.dwExtraInfo = NativeMethods.GetMessageExtraInfo();
        }

        /*
        // CTRL + A の場合の例
        // uint actions = 4; // 初期化ではアクション数の変更が必要です

        // アクションを設定します
        inputs[0].ui.Keyboard.VirtualKey = (ushort)VK.CONTROL; // CTRLを
        inputs[0].ui.Keyboard.dwFlags = KEYEVENTF_KEYDOWN;     // 押す
        inputs[1].ui.Keyboard.VirtualKey = (ushort)VK.A;       // Aを
        inputs[1].ui.Keyboard.dwFlags = KEYEVENTF_KEYDOWN;     // 押す
        inputs[2].ui.Keyboard.VirtualKey = (ushort)'A';        // Aを
        inputs[2].ui.Keyboard.dwFlags = KEYEVENTF_KEYUP;       // 離す
        inputs[3].ui.Keyboard.VirtualKey = (ushort)VK.CONTROL; // CTRLを
        inputs[3].ui.Keyboard.dwFlags = KEYEVENTF_KEYUP;       // 離す
        */

        NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(inputs[0]));
    }


    private static void OnTimedEvent(Object sender, ElapsedEventArgs e)
    {
        SuppressScreenSaverOnMouseEvents();
        SuppressScreenSaverOnKeyboardEvents();
    }


    private TrayApp(int TISec)
    {
        // タイマーを登録します
        timer = new Timer(TISec * 1000);
        timer.Elapsed += OnTimedEvent;
        timer.Start();

        // コンテキストメニューを作成します
        trayMenu = new ContextMenu();
        trayMenu.MenuItems.Add("アプリケーションを終了します", OnExit);

        // トレイアイコンを作成します
        trayIcon = new NotifyIcon();
        trayIcon.ContextMenu = trayMenu;
        trayIcon.Visible = true;
        trayIcon.Text = "スクリーンセーバーを抑制しています";
        try
        {
            // 自身の実行ファイルからアイコンを取得します
            System.Drawing.Icon myIcon = System.Drawing.Icon.ExtractAssociatedIcon(
                System.Reflection.Assembly.GetExecutingAssembly().Location
            );
            trayIcon.Icon = myIcon;
        }
#pragma warning disable 0168 // CS0168, the variable 'ex' is declared but never used
        catch (Exception ex)
        {
            // Nothing.
            // Console.WriteLine("アイコンの取得に失敗しました: {}", ex.Message);
        }
#pragma warning restore 0168

        // トレイアイコンをマウスでクリックしたときのイベントを登録します
        trayIcon.MouseClick += (sender, e) => TrayIcon_Click(sender, e, TISec);
    }


    [STAThread]
    public static void Main()
    {
        // 二重起動を抑止します
        string MutexObjectName = "Suppress Screen Saver on Events";
        bool createdNew;
        using (Mutex mutex = new Mutex(true, MutexObjectName, out createdNew))
        {
            if (createdNew)
            {
                // イベントを発生させる間隔(秒)
                int TISec = 10;
                // イベント間隔の最小と最大値(秒)
                int TIMin = 10;
                int TIMax = 3600;
                // コマンドライン引数をチェックします
                string[] args = Environment.GetCommandLineArgs();
                switch (args.Length)
                {
                    case 1:
                        Application.Run(new TrayApp(TISec));
                        break;
                    case 2:
                        // 第一引数
                        // int型に数値変換できるならTISecへ代入、結果をboolで返します
                        if (int.TryParse(args[1], out TISec))
                        {
                            // 数値が範囲外ならエラー
                            if (!Enumerable.Range(TIMin, TIMax - TIMin + 1).Contains(TISec))
                            {
                                goto default;
                            }
                        }
                        else
                        {
                            goto default;
                        }
                        Application.Run(new TrayApp(TISec));
                        break;
                    default:
                        // エラー処理
                        string error_msg = string.Format(
                            "指定できる引数は数値で {0} ～ {1}(秒)までです",
                            TIMin,
                            TIMax
                        );
                        MessageBox.Show(
                            error_msg,
                            "無効な引数が指定されています",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Asterisk
                        );
                        break;
                }
            }
        }
    }
}
