using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;


public class MonitorPower
{
    private const int HWND_BROADCAST = 0xFFFF;
    private const uint WM_SYSCOMMAND = 0x112;
    private const uint SC_MONITORPOWER = 0xF170;

    private const int SC_MONITORPOWER_SAVE = 0x1;
    private const int SC_MONITORPOWER_OFF = 0x2;
    private const int SC_MONITORPOWER_RESTORE = 0xFFFF;


    class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = false)]
        public static extern IntPtr SendMessage(int hWnd, uint Msg, uint wParam, int lParam);
    }


    public static void PowerSave()
    {
        //省電力
        NativeMethods.SendMessage(
            HWND_BROADCAST,
            WM_SYSCOMMAND,
            SC_MONITORPOWER,
            SC_MONITORPOWER_SAVE
        );
    }


    public static void PowerOff()
    {
        //モニター停止
        NativeMethods.SendMessage(
            HWND_BROADCAST,
            WM_SYSCOMMAND,
            SC_MONITORPOWER,
            SC_MONITORPOWER_OFF
        );
    }


    public static void PowerOn()
    {
        //モニター復帰
        NativeMethods.SendMessage(
            HWND_BROADCAST,
            WM_SYSCOMMAND,
            SC_MONITORPOWER,
            SC_MONITORPOWER_RESTORE
        );
    }


    [STAThread]
    static void Main()
    {
        PowerOff();
    }
}
