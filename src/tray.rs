use crate::config;
use std::mem::size_of;
use windows::core::{PCWSTR, w};
use windows::Win32::Foundation::{HWND, LPARAM, LRESULT, WPARAM};
use windows::Win32::UI::Shell::{
    Shell_NotifyIconW, NIM_ADD, NIM_DELETE, NOTIFYICONDATAW, NIF_ICON, NIF_MESSAGE, NIF_TIP,
};
use windows::Win32::UI::WindowsAndMessaging::{
    CreatePopupMenu, CreateWindowExW, DefWindowProcW, DestroyWindow, GetCursorPos,
    MessageBoxW, PostQuitMessage, RegisterClassW, SetForegroundWindow, TrackPopupMenu,
    CW_USEDEFAULT, MB_ICONASTERISK, MB_OKCANCEL, MESSAGEBOX_RESULT, TPM_BOTTOMALIGN,
    TPM_LEFTALIGN, WM_COMMAND, WM_DESTROY, WM_LBUTTONDOWN, WM_LBUTTONUP, WM_RBUTTONUP,
    WNDCLASSW, WS_OVERLAPPEDWINDOW, MF_STRING, AppendMenuW, IDI_APPLICATION, LoadIconW,
};

pub struct TrayManager {
    pub hwnd: HWND,
}

impl TrayManager {
    pub unsafe fn new() -> Self {
        let instance = windows::Win32::System::LibraryLoader::GetModuleHandleW(None)
            .expect("Failed to get module handle");
        let class_name = w!("SuppressScreenSaverClass");

        let wnd_class = WNDCLASSW {
            lpfnWndProc: Some(Self::window_proc),
            hInstance: instance.into(),
            lpszClassName: class_name,
            ..Default::default()
        };
        let _ = RegisterClassW(&wnd_class);

        let title: Vec<u16> = config::APP_NAME.encode_utf16().chain(Some(0)).collect();
        let hwnd = CreateWindowExW(
            Default::default(), class_name, PCWSTR(title.as_ptr()), WS_OVERLAPPEDWINDOW,
            CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT,
            None, None, instance, None,
        );

        let manager = Self { hwnd };
        manager.add_icon();
        manager
    }

    unsafe fn add_icon(&self) {
        let mut nid = self.create_notify_data();
        nid.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP;
        nid.uCallbackMessage = config::WM_TRAY_ICON;

        let instance = windows::Win32::System::LibraryLoader::GetModuleHandleW(None)
            .expect("Failed to get module handle");

        nid.hIcon = LoadIconW(instance, PCWSTR(config::ICON_ID as usize as *const u16))
            .unwrap_or_else(|_| LoadIconW(None, IDI_APPLICATION).unwrap());

        let tip_name: Vec<u16> = config::APP_NAME.encode_utf16().chain(Some(0)).collect();
        let len = tip_name.len().min(nid.szTip.len() - 1);
        nid.szTip[..len].copy_from_slice(&tip_name[..len]);

        Shell_NotifyIconW(NIM_ADD, &nid);
    }

    unsafe fn create_notify_data(&self) -> NOTIFYICONDATAW {
        let mut nid = NOTIFYICONDATAW::default();
        nid.cbSize = size_of::<NOTIFYICONDATAW>() as u32;
        nid.hWnd = self.hwnd;
        nid.uID = config::TRAY_ID;
        nid
    }

    unsafe extern "system" fn window_proc(
        hwnd: HWND, msg: u32, wparam: WPARAM, lparam: LPARAM
    ) -> LRESULT {
        match msg {
            config::WM_TRAY_ICON => {
                match lparam.0 as u32 {
                    WM_LBUTTONUP => Self::show_info_message(hwnd),
                    WM_RBUTTONUP => Self::show_context_menu(hwnd),
                    _ => {}
                }
                LRESULT(0)
            }
            WM_COMMAND => {
                if wparam.0 == config::IDM_EXIT { let _ = DestroyWindow(hwnd); }
                LRESULT(0)
            }
            WM_LBUTTONDOWN => { Self::show_info_message(hwnd); LRESULT(0) }
            WM_DESTROY => {
                let mut nid = NOTIFYICONDATAW::default();
                nid.cbSize = size_of::<NOTIFYICONDATAW>() as u32;
                nid.hWnd = hwnd;
                nid.uID = config::TRAY_ID;
                Shell_NotifyIconW(NIM_DELETE, &nid);
                PostQuitMessage(0);
                LRESULT(0)
            }
            _ => DefWindowProcW(hwnd, msg, wparam, lparam),
        }
    }

    unsafe fn show_info_message(hwnd: HWND) {
        let title = w!("Suppress Screen Saver");
        let msg = w!("スクリーンセーバーを抑止しています\n\n終了するにはOKボタンを押してください");
        if MessageBoxW(hwnd, msg, title, MB_OKCANCEL | MB_ICONASTERISK) == MESSAGEBOX_RESULT(1) {
            let _ = DestroyWindow(hwnd);
        }
    }

    unsafe fn show_context_menu(hwnd: HWND) {
        if let Ok(h_menu) = CreatePopupMenu() {
            let _ = AppendMenuW(h_menu, MF_STRING, config::IDM_EXIT, w!("終了"));
            let mut pos = std::mem::zeroed();
            let _ = GetCursorPos(&mut pos);
            SetForegroundWindow(hwnd);
            TrackPopupMenu(h_menu, TPM_LEFTALIGN | TPM_BOTTOMALIGN, pos.x, pos.y, 0, hwnd, None);
        }
    }
}
