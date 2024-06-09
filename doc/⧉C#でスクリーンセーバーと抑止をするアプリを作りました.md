# ⧉C#でスクリーンセーバーと抑止アプリを作りました

## ◇はじめに

C#でWindows10標準のコンパイラで動作するスクリーンセーバーと抑止アプリを作りました。

スクリーンセーバーは必要と思われる機能のみ実装しました。
抑止アプリはシステムで用意された方法とマウスをエミュレートする方法で作りました。

:::note info
スクリーンセーバーを抑止するアプリは他に多機能なものがあります。
しかし会社の環境ではセキュリティーに引っ掛かり実行できませんでした。
そこで試しに作成したものが実行できたため同じような方がいるかもと
考えて公開することにしました。
:::

## ⧉リポジトリを公開しています

https://github.com/Aromatibus/SuppressScreenSaver

:::note info
GitHUBに開発時の環境をそのまま公開しています。
普段はコメントをほとんど書かないのですが記事にするため頑張って書きました。
コンパイル用バッチファイルもいっしょにあります。
こちらも合わせてご利用ください。
:::

## ⧉C#でスクリーンセーバーを作りました

Windows用スクリーンセーバーの記事はなん番煎じになるかわかりませんが
必要な機能は揃えていると思いますので参考になればと思います。

### ◇スクリーンセーバーのコード

<details><summary>C#のコード</summary>

```c#:SimpleScreenSaver.cs
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;


public class SimpleScreenSaver : Form
{
    /**
    <summary>
    スクリーンセーバーの挙動を理解するために作成しました。
    拡張子をEXEからSCRに変更すればスクリーンセーバーとして登録できるようになります。
    Windowsに登録するにはアプリを右クリックして表示されるコンテキストメニューから
    インストールを選びます。
    また、インストールされたスクリーンセーバーは設定メニューからプレビューなどの
    項目を選択できるようになります。
    この時、スクリーンセーバーはWindowsから引数付きで起動されます。
    引数は実際の動作を見て実装しています。

    デバッグ用のConsole.WriteLineを表示させるにはコンソールアプリとしてコンパイル
    し実行はコマンドプロンプトから行います。
    </summary>
    */


    private Random random = new Random();
    private Timer timer;

    private static readonly DateTime StartDT = DateTime.Now;
    private static Point MousePoint = new Point();


    private void timer_Tick(object sender, EventArgs e)
    {
        // OnPaint()の呼び出しもここで行われています。
        this.Invalidate();
    }


    protected override void OnPaint(PaintEventArgs e)
    {
        /**
        <summary>
        一度に複数の図形をランダムな位置に絵画します。
        C#は様々な図形を書くメソッドが用意されています。
        https://learn.microsoft.com/ja-jp/dotnet/api/system.drawing.graphics?view=dotnet-plat-ext-8.0

        背景の透過設定について
        Windowsにインストールして実行されるデスクトップ画面は消されていました。
        プレビューでは消されないようです。
        個人的には透過できたほうが好みなんですが残念です。
        探せば透過する方法もありそうですが、そこまで調べていません。
        </summary>
        */

        // 背景を透過設定します
        this.Opacity = 0.8; // 0.0 - 1.0

        // 背景設定します
        Color backgroundColor = Color.FromArgb(0, 0, 0);
        this.BackColor = backgroundColor;

        // 図形を絵画します
        int numberShapes = 20;
        int sizeMin = this.Width / 30;
        int sizeMax = this.Width / 5;
        int colorMin = 0; // 0 - 256
        int colorMax = 128; // 0 - 256
        for (int i = 0; i < numberShapes; i++)
        {
            int x = random.Next(this.Width);
            int y = random.Next(this.Height);
            int size = random.Next(sizeMin, sizeMax);
            Color color = Color.FromArgb(
                random.Next(colorMin, colorMax),
                random.Next(colorMin, colorMax),
                random.Next(colorMin, colorMax)
            );
            e.Graphics.FillEllipse(new SolidBrush(color), x, y, size, size);
        }
    }


    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        Console.WriteLine("Key Down     : {0}", e.KeyCode);
        Application.Exit();
    }


    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        /**
        <summary>
        通常、OnKeyDownイベントがあれば十分ですがOnKeyUpイベントでも
        解除できるようにします。
        なお、コマンドプロンプトからアプリを実行した時はエンターキー
        を押した瞬間にプログラムは起動します。
        そのままエンターキーを放した時にOnKeyUpイベントが発生します。
        起動直後は判定を回避する処理を入れて対応しています。
        回避に必要な秒数は1秒としていますがもっと短くても良いかもしれません。
        多くのスクリーンセーバーはここに対応していないと思われます。
        個人的にはOnKeyUpもいれるべきだと思います。
        </summary>
        */

        // 起動直後対策 : 起動後、指定ミリ秒判定しません
        int skipMSec = 1000;
        TimeSpan ts = DateTime.Now - StartDT;
        if (ts.TotalMilliseconds < skipMSec) { return; }

        Console.WriteLine("Key Up       : {0}", e.KeyCode);
        Application.Exit();
    }


    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        /**
        <summary>
        TODO : マウスカーソルの位置情報がおかしい。。。
        本イベントをテスト中、マウスカーソルの位置情報を取得したときに
        プログラム開始から最大4回、位置情報に±1の誤差を確認しました。
        結果、プログラム開始直後はマウスを動かしていなくても
        動かした(位置を変更した)と誤検知してしまいます。
        起動直後は位置判定しないことで対策をしました。
        誤検知の回避だけなら100ミリ秒で十分でしたがダブルクリックで
        起動したときに反応しないように大きめに数値と取っています。

        C#でマウスカーソルの位置を取得する方法はいくつかありますが、
        テストした方法はいずれも同様の問題が発生しました。
        Win32APIのGetCursorPos関数でも同様の問題が発生したため根本的な
        解決策は見つかっていません。
        </summary>
        */

        // 起動直後対策 : 起動後、指定ミリ秒判定しません
        int skipMSec = 1000;
        TimeSpan ts = DateTime.Now - StartDT;
        if (ts.TotalMilliseconds < skipMSec) { return; }

        // 開始時のカーソル位置を保存します
        if (MousePoint.IsEmpty) { MousePoint = e.Location; }

        // マウスが動いたか座標をチェックします
        if (MousePoint.X != e.X || MousePoint.Y != e.Y)
        {
            Console.WriteLine(
                "Mouse Move   : X {0} -> {1} / Y {2} -> {3}",
                MousePoint.X, e.X,
                MousePoint.Y, e.Y
            );
            Application.Exit();
        }
    }


    private void OnMouseDown(object sender, MouseEventArgs e)
    {
        Console.WriteLine("Mouse Button : {0}", e.Button);
        Application.Exit();
    }


    private void OnMouseWheel(object sender, MouseEventArgs e)
    {
        Console.WriteLine("Mouse Wheel  : {0}", e.Delta);
        Application.Exit();
    }


    public SimpleScreenSaver()
    {
        // フォームを設定します
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Maximized;
        this.TopMost = true;
        this.DoubleBuffered = true;

        // マウスカーソルを非表示にします
        Cursor.Hide();

        // キーボード、マウスイベントを登録します
        this.KeyDown += new KeyEventHandler(OnKeyDown);
        this.KeyUp += new KeyEventHandler(OnKeyUp);
        this.MouseMove += new MouseEventHandler(OnMouseMove);
        this.MouseDown += new MouseEventHandler(OnMouseDown);
        this.MouseWheel += new MouseEventHandler(OnMouseWheel);

        // タイマーイベントを登録します
        int TIMSec = 300;
        timer = new Timer();
        timer.Interval = TIMSec;
        timer.Tick += new EventHandler(timer_Tick);
        timer.Start();
    }


    [STAThread]
    static void Main()
    {
        // 二重起動を抑止します
        string MutexObjectName = "Simple Screen Saver";
        bool createdNew;
        using (Mutex mutex = new Mutex(true, MutexObjectName, out createdNew))
        {
            if (createdNew)
            {
                // コマンドライン引数をチェックします
                string[] args = Environment.GetCommandLineArgs();
                switch (args.Length)
                {
                    case 1: // 引数がない場合:スクリーンセーバーを表示します
                        Application.Run(new SimpleScreenSaver());
                        break;
                    case 2: // 引数がある場合:引数をチェックします
                        // オプション"/C"が指定されるとき後ろに"/C"以外の文字列が
                        // 付与されているため先頭2文字でトリムします
                        // 文字列の意味は調べていません
                        switch (args[1].ToLower().Trim().Substring(0, 2))
                        {
                            case "/s": // show:スクリーンセーバーを表示します
                                Application.Run(new SimpleScreenSaver());
                                break;
                            case "/c": // configure:オプションの表示します
                                MessageBox.Show(
                                    "\n\n\nCopyright (c) 2024 Aromatibus\n",
                                    "シンプル・スクリーンセーバー",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Asterisk
                                );
                                break;
                            case "/p": // preview:プレビューを表示します
                                // プレビューボタンを押すと実際には/Sが指定されてます
                                break;
                            default: // 指定できない引数がある場合はエラーになります
                                Console.WriteLine("指定できない引数が指定されました : {}", args[1]);
                                MessageBox.Show(
                                    "指定できない引数が指定されました",
                                    "SimpleScreenSaver",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Asterisk
                                );
                                break;
                        }
                        break;
                    default: // 複数の引数がある場合はエラーになります
                        Console.WriteLine("複数の引数が指定されました");
                        for (int i = 1; i < args.Length; i++)
                            Console.WriteLine("args[{0}] : {1}", i, args[i]);
                        MessageBox.Show(
                            "指定できない複数の引数が指定されました",
                            "SimpleScreenSaver",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Asterisk
                        );
                        break;
                }
            }
        }
    }
}

```

</details>

### ◇実装した機能について

シンプルに画面を暗くしランダムに円を絵画するプログラムにしました。

実装した機能は以下の通りです。

1. **アプリの2重起動を抑止します**
2重起動を抑止するためにMutexを使っています。
Mutexはプロセス間通信の一種でプロセス間で排他制御を行うために使われます。
良いサンプルが見つからずGitCopilotに助けられました。
1. **スクリーンセーバーの設定で表示、設定、プレビューを行います**
スクリーンセーバーはWindowsから起動される時に引数付きで起動されます。
実際に動かした結果から引数を判断しています。
1. **キーボード、マウスのカーソル、ボタン、ホイールを監視します**
キーボードはキーを押した時、離した時を区別して監視しています。
マウスカーソルはアプリ起動時の位置を取得して現在位置と比較しています。
マウスボタンはボタンの押下を取得、監視しています。
マウスホイールはホイールの回転量を取得、監視しています。

:::note alert
起動直後のマウスカーソルの位置取得に問題が見つかりました。
詳細はソースコードのコメントを見ていただければと思います。
:::

### ◇スクリーンセーバーのインストールについて

WindowsのスクリーンセーバーはEXE型のただの実行ファイルのようです。
拡張子をSCRに変更すればスクリーンセーバーとしてインストール可能になります。
登録するには右クリックして表示されるメニューからインストールを選びます。

## ⧉C#でスクリーンセーバーを抑止するアプリを作りました

### ◇スクリーンセーバーの抑止アプリのコード

アプリは3種類作成しましたコードと合わせて紹介します。

#### ※SetThreadExecutionState関数を使ったもの

- **通常のアプリ形式のシンプルなもの**

<details><summary>C#のコード</summary>

```c#:SimpleSuppressScreenSaver.cs
using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;


public class Program
{
    /**
    <summary>
    スクリーンセーバーを抑止します。
    </summary>
    */


    [FlagsAttribute]
    public enum EXECUTION_STATE : uint
    {
        // システムのアイドルタイマーをリセットします
        // スリープを抑止します
        ES_SYSTEM_REQUIRED = 0x00000001,
        // ディスプレイのアイドルタイマーをリセットします
        // スクリーンセーバーを抑止します
        ES_DISPLAY_REQUIRED = 0x00000002,
        // 上記のフラグと組み合わせると設定を継続して有効にします
        // 単独で使用すると設定をリセットします
        ES_CONTINUOUS = 0x80000000,
    }


    class NativeMethods
    {
        // https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-setthreadexecutionstate
        [DllImport("kernel32.dll", SetLastError = false)]
        public static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);
    }


    public static EXECUTION_STATE SuppressScreenSaver()
    {
        /**
        <summary>
        スクリーンセーバーを抑止するだけであればES_DISPLAY_REQUIREDだけでできます。
        一般的にスクリーンセーバーを抑止する必要がある時はスリープも抑止する必要
        があると考えて同時に抑止しています。
        </summary>
        */

        // モニターオフとスリープを抑止します
        return NativeMethods.SetThreadExecutionState(
            EXECUTION_STATE.ES_CONTINUOUS |
            EXECUTION_STATE.ES_DISPLAY_REQUIRED |
            EXECUTION_STATE.ES_SYSTEM_REQUIRED
        );
    }


    public static EXECUTION_STATE SuppressReset()
    {
        // 抑止状態を初期化します
        return NativeMethods.SetThreadExecutionState(
            EXECUTION_STATE.ES_CONTINUOUS
        );
    }


    [STAThread]
    public static void Main()
    {
        // 二重起動を抑止します
        string MutexObjectName = "Simple Suppress Screen Saver";
        bool createdNew;
        using (Mutex mutex = new Mutex(true, MutexObjectName, out createdNew))
        {
            if (createdNew)
            {
                // スクリーンセーバーを抑止します
                SuppressScreenSaver();

                string titleMsg = "Simple Suppress Screen Saver";
                string eventMsg =
                    string.Format(
                        "スクリーンセーバーを抑止しています\n\n" +
                        "終了するにはOKボタンを押してください"
                    );
                MessageBox.Show(
                    eventMsg,
                    titleMsg,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Asterisk
                );

                // 抑止状態を初期化します
                SuppressReset();
            }
        }
    }
}

```

</details>

- **タスクトレイに常駐するもの**

<details><summary>C#のコード</summary>

```c#:SuppressScreenSaver.cs
using System;
using System.Drawing;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;


public class TrayApp : Form
{
    /**
    <summary>
    スクリーンセーバーを抑止します。
    タスクバーのアイコンを非表示にしてタスクトレイに常駐させています。
    </summary>
    */

    // MessageBoxの2重表示を抑止するためのフラグです
    private static bool IsMessageBoxDisplayed = false;

    private static NotifyIcon trayIcon;

    private static ContextMenu trayMenu;


    [FlagsAttribute]
    public enum EXECUTION_STATE : uint
    {
        // システムのアイドルタイマーをリセットします
        // スリープを抑止します
        ES_SYSTEM_REQUIRED = 0x00000001,
        // ディスプレイのアイドルタイマーをリセットします
        // スクリーンセーバーを抑止します
        ES_DISPLAY_REQUIRED = 0x00000002,
        // 上記のフラグと組み合わせると設定を継続して有効にします
        // 単独で使用すると設定をリセットします
        ES_CONTINUOUS = 0x80000000,
    }


    class NativeMethods
    {
        // https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-setthreadexecutionstate
        [DllImport("kernel32.dll", SetLastError = false)]
        public static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);
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
            trayIcon.Dispose();
        }
        base.Dispose(isDisposing);
    }


    private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
    {
        // 抑止状態を初期化します
        SuppressReset();
    }


    private static void OnExit(object sender, EventArgs e)
    {
        Application.Exit();
    }


    public static EXECUTION_STATE SuppressScreenSaver()
    {
        /**
        <summary>
        スクリーンセーバーを抑止するだけであればES_DISPLAY_REQUIREDだけでできます。
        一般的にスクリーンセーバーを抑止する必要がある時はスリープも抑止する必要
        があると考えて同時に抑止しています。
        </summary>
        */

        // モニターオフとスリープを抑止します
        return NativeMethods.SetThreadExecutionState(
            EXECUTION_STATE.ES_CONTINUOUS |
            EXECUTION_STATE.ES_DISPLAY_REQUIRED |
            EXECUTION_STATE.ES_SYSTEM_REQUIRED
        );
    }


    public static EXECUTION_STATE SuppressReset()
    {
        // 抑止状態を初期化します
        return NativeMethods.SetThreadExecutionState(
            EXECUTION_STATE.ES_CONTINUOUS
        );
    }


    private static void TrayIcon_Click(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            // メッセージの2重表示を抑止します
            if (!IsMessageBoxDisplayed)
            {
                IsMessageBoxDisplayed = true;
                string titleMsg = "Suppress Screen Saver";
                string eventMsg =
                    string.Format(
                        "スクリーンセーバーを抑止しています\n\n" +
                        "終了するにはOKボタンを押してください"
                    );
                // ダミーフォームを作成してメッセージを最前面に表示させます
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


    private TrayApp()
    {
        // アプリ終了時に呼び出されるイベントを登録します
        AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

        // スクリーンセーバーを抑止します
        SuppressScreenSaver();

        // コンテキストメニューを作成します
        trayMenu = new ContextMenu();
        trayMenu.MenuItems.Add("アプリケーションを終了します", OnExit);

        // トレイアイコンを作成します
        trayIcon = new NotifyIcon();
        trayIcon.ContextMenu = trayMenu;
        trayIcon.Visible = true;
        trayIcon.Text = "スクリーンセーバーを抑止しています";
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
        trayIcon.MouseClick += (sender, e) => TrayIcon_Click(sender, e);
    }


    [STAThread]
    public static void Main()
    {
        // 二重起動を抑止します
        string MutexObjectName = "Suppress Screen Saver";
        bool createdNew;
        using (Mutex mutex = new Mutex(true, MutexObjectName, out createdNew))
        {
            if (createdNew)
            {
                Application.Run(new TrayApp());
            }
        }
    }
}

```

</details>

#### ※キーボードまたはマウスをエミュレートして抑制するもの

作ってみてわかったことですがこの方法は考慮するべきことが多くお勧めできません。
たとえばマウスを使った作業中にマウスをエミュレートした場合、作業に影響がでること
が考えられます。
また、コードも冗長になりやすく抑止以外に目的がない場合はお勧めしません。
キーボードやマウスをエミュレートする方法は他でも使えるので残すことにしました。

- **タスクトレイに常駐するもの**

<details><summary>C#のコード</summary>

```c#:SuppressScreenSaverEvents.cs
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

```

</details>

### ◇スクリーンセーバーの抑止方法について

スクリーンセーバーを抑止するにはWin32APIの[SetThreadExecutionState関数][]を使います。
同関数の備考にはスリープおよびスクリーンセーバーが起動する条件が記載されています。

`
The system automatically detects activities such as local keyboard or mouse input, server activity, and changing window focus.
Activities that are not automatically detected include disk or CPU activity and video display.
`

意訳すると
`
「システムは、ローカルのキーボードやマウスの入力、サーバーのアクティビティ、ウィンドウのフォーカスの変更などアクティビティを自動的に検出します。
自動的に検出されないアクティビティには、ディスクや CPU のアクティビティ、ビデオの表示などがあります。」
`
となります。

つまり条件は

1. ローカルのキーボードやマウスの入力があった場合
2. サーバーのアクティビティが変更された場合
3. ウィンドウのフォーカスの変更された場合

の３つです。
上記のいずれかが発生するとディスプレイまたはシステムを動作状態を監視している
アイドルタイマーがリセットされスクリーンセーバーやスリープなどが抑制されます。
スクリーンセーバーの抑止アプリには条件１の入力イベントをエミュレートして抑制
する方法をよく見るためこちらのアプリも作成してみました。

## ⧉C#のソースファイルをコンパイルするには

C#のソースファイルをコンパイルするには[.NET Framework 4.0][]が必要です。
Windows10以上であれば標準でインストールされています。
VisualStdioは無くてもコンパイル可能です。

コンパイル用のバッチファイルを[リポジトリ](#リポジトリを公開しています)にて公開しています。

このバッチファイルを利用するには**images**フォルダの下に同名の拡張子が**ico**の
**アイコンデータ**も合わせて必要です。

バッチファイルにソースファイルをドラック・アンド・ドロップするか
バッチファイルを参考に直接コマンドを実行してください。

バッチファイルは32ビット用、64ビット用があります。
また、コマンドプロンプトから起動した時にConsole出力できるデバッグ用の
バッチファイルもあります。

|ファイル名|用途|
|:--|:--|
|CSC_CLI64_DragDropHere.bat|64ビット・コマンドプロンプト対応|
|CSC_Win32_DragDropHere.bat|32ビット・Windows用|
|CSC_Win64_DragDropHere.bat|64ビット・Windows用|

<details><summary>64bit Windows用のバッチファイル</summary>

```batch:CSC_Win64_DragDropHere.bat
@echo off

:: 遅延展開 バグの原因になる場合があるので注意;
setlocal enabledelayedexpansion

:: エスケープシーケンスを登録;
for /F "tokens=1,2 delims=#" %%a in ('"prompt #$H#$E# & echo on & for %%b in (1) do rem"') do (set ESC=%%b)

:: UTF-8などで保存されたバッチファイルのShiftJis対策;
chcp 65001 > nul

:: 実行時のフォルダに移動;
pushd "%~dp0"

:: コマンドプロンプトを変更;
set prompt=%ESC%[104m$P$G%ESC%[0m

:: C# のコンパイラ
:: 32bit
:: set CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
:: 64bit
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

:: 引数１が空でなければ、拡張子を".cs"に仮定変更してCSCに渡す;
if "%1" == "" (goto :EndProcess)
@echo on
%CSC% /t:winexe %~n1.cs /resource:%0 /resource:%~n1.cs /win32icon:images\%~n1.ico /platform:x64
@echo off

echo %~n1|clip

:: 終了処理（エクスプローラーから起動されていたらプロンプト表示）;
:EndProcess
echo %cmdcmdline% | find /i "%~f0" > nul
if %errorlevel% equ 0 (cmd /k)

```

</details>

環境や用途に合わせて選択してください。

:::note info
会社の端末でもコンパイルできました。
:::

## ⧉ウイルス対策ソフトの誤検知について

本アプリの開発動機もそうでしたが、本アプリもなんどもコンパイルを繰り返すと
突然、セキュリティー引っかかることがあります。
原因については「[ウィルスが検出されました！それ、いつもの誤検出です][]」が参考
になります。
結局のところ除外リストへの登録など対処療法しかないようです。

## ⧉ライセンス

[MIT license][] のもと公開しています。
コンパイル済みの実行ファイルを含めて配布、改変など自由に行ってください。

Copyright (c) 2024 [Aromatibus][].

[Aromatibus]: https://github.com/Aromatibus
[MIT license]: https://github.com/Aromatibus/SuppressScreenSaver/blob/main/LICENSE

[.NET Framework 4.0]: https://dotnet.microsoft.com/ja-jp/download/dotnet-framework/net40
[SetThreadExecutionState関数]: https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-setthreadexecutionstate

[ウィルスが検出されました！それ、いつもの誤検出です]: https://all.undo.jp/asr/1st/document/10_04.html
