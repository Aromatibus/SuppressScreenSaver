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
