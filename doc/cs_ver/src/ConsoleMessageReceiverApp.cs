using System;
using System.Runtime.InteropServices;

namespace ConsoleMessageReceiverApp
{
    class Program
    {
        const int WM_CUSTOM_MESSAGE = 0x8000; // カスタムメッセージのID

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        static void Main(string[] args)
        {
            Console.WriteLine("待機中...");
            while (true)
            {
                IntPtr consoleWindow = FindWindow(null, "ConsoleMessageReceiverApp"); // 自身のウィンドウを探す
                if (consoleWindow != IntPtr.Zero)
                {
                    break;
                }
            }
            Console.WriteLine("メッセージを受け取りました！");
        }
    }
}