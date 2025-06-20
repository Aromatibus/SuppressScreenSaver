using System;
using System.Drawing;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
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

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = false)]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll", SetLastError = false)]
        public static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
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
            }else{
                // 既に起動している場合は、起動している既存のトレイアイコンをクリックします
                // これによりメッセージボックスが表示されます
                // 既存のトレイアイコンをクリックするためのメッセージを送信します
                const int WM_LBUTTONDOWN = 0x0201;
                const int WM_LBUTTONUP = 0x0202;
                IntPtr handle = NativeMethods.FindWindow(null, MutexObjectName);
                if (handle != IntPtr.Zero)
                {
                    // マウスの左ボタンを押すメッセージを送信します
                    NativeMethods.SendMessage(handle, WM_LBUTTONDOWN, IntPtr.Zero, IntPtr.Zero);
                    // マウスの左ボタンを離すメッセージを送信します
                    NativeMethods.SendMessage(handle, WM_LBUTTONUP, IntPtr.Zero, IntPtr.Zero);
                }else{
                    // 既存のトレイアイコンが見つからない場合は、メッセージボックスを表示します
                    if (!IsMessageBoxDisplayed)
                    {
                        IsMessageBoxDisplayed = true;
                        MessageBox.Show(
                            "既にスクリーンセーバー抑止アプリケーションが起動しています。",
                            MutexObjectName,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                        IsMessageBoxDisplayed = false;
                    }
                }
            }
        }
    }
}
