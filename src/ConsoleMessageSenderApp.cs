using System;
using System.Runtime.InteropServices;

namespace ConsoleMessageSenderApp
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
            IntPtr receiverHandle = FindWindow(null, "ConsoleMessageReceiverApp"); // 受信側のウィンドウを探す
            if (receiverHandle != IntPtr.Zero)
            {
                SendMessage(receiverHandle, WM_CUSTOM_MESSAGE, IntPtr.Zero, IntPtr.Zero);
            }
        }
    }
}