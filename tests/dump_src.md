# Tree

```text
src/
├─ config.rs
├─ main.rs
├─ monitor.rs
├─ screen_off.rs
├─ suppress.rs
└─ tray.rs
```

# text

config.rs

```text
// アプリケーション識別名
pub const APP_NAME: &str = "Suppress Screen Saver";
pub const MUTEX_NAME: &str = "SuppressScreenSaverMutex";

// ウィンドウメッセージID（カスタム）
pub const WM_TRAY_ICON: u32 = 0x0400 + 1; // WM_USER + 1

// アイコンのリソースID
pub const ICON_ID: u16 = 1;
pub const TRAY_ID: u32 = 1;

// コンテキストメニュー用ID
pub const IDM_EXIT: usize = 1001;

// 四隅の判定しきい値 (px)
pub const CORNER_THRESHOLD_PX: i32 = 10;

// 四隅に留まってから画面OFFにするまでの時間 (ミリ秒)
pub const CORNER_TIMEOUT_MS: u64 = 3000;

// 画面OFFから復帰するために必要なマウス移動距離 (px)
pub const WAKE_THRESHOLD_PX: i32 = 5;

// マウス監視のインターバル (ミリ秒)
pub const MONITOR_INTERVAL_MS: u32 = 100;

// 黒画面ウィンドウのクラス名
pub const BLACK_SCREEN_CLASS: &str = "BlackScreenWindowClass";
```

main.rs

```text
#![windows_subsystem = "windows"]

mod config;
mod monitor;
mod screen_off;
mod suppress;
mod tray;

use crate::monitor::MouseMonitor;
use crate::tray::TrayManager;
use windows::core::PCWSTR;
use windows::Win32::Foundation::{ERROR_ALREADY_EXISTS, HWND, LPARAM, WPARAM};
use windows::Win32::System::Threading::CreateMutexW;
use windows::Win32::UI::WindowsAndMessaging::{
    DispatchMessageW, FindWindowW, MsgWaitForMultipleObjects, PeekMessageW, SendMessageW,
    TranslateMessage, MSG, PM_REMOVE, QS_ALLINPUT, WM_LBUTTONDOWN, WM_LBUTTONUP, WM_QUIT,
};

fn main() {
    unsafe {
        let mutex_name: Vec<u16> = config::MUTEX_NAME.encode_utf16().chain(Some(0)).collect();

        let _h_mutex = CreateMutexW(None, true, PCWSTR(mutex_name.as_ptr()))
            .expect("Failed to create mutex");

        // GetLastError() が Result 型を返すため、Err の中身を比較する
        let last_err = windows::Win32::Foundation::GetLastError();
        if let Err(e) = last_err {
            if e.code() == ERROR_ALREADY_EXISTS.to_hresult() {
                let app_name_wide: Vec<u16> = config::APP_NAME.encode_utf16().chain(Some(0)).collect();
                let hwnd = FindWindowW(None, PCWSTR(app_name_wide.as_ptr()));

                if hwnd.0 != 0 {
                    let _ = SendMessageW(hwnd, WM_LBUTTONDOWN, WPARAM(0), LPARAM(0));
                    let _ = SendMessageW(hwnd, WM_LBUTTONUP, WPARAM(0), LPARAM(0));
                }
                return;
            }
        }

        suppress::suppress_screensaver();
        let _tray = TrayManager::new();
        let mut monitor = MouseMonitor::new();

        let mut msg = MSG::default();
        loop {
            while PeekMessageW(&mut msg, HWND(0), 0, 0, PM_REMOVE).as_bool() {
                if msg.message == WM_QUIT {
                    suppress::reset_suppression();
                    return;
                }
                TranslateMessage(&msg);
                DispatchMessageW(&msg);
            }

            monitor.tick();
            MsgWaitForMultipleObjects(None, false, config::MONITOR_INTERVAL_MS, QS_ALLINPUT);
        }
    }
}
```

monitor.rs

```text
use crate::config;
use crate::screen_off::BlackScreenManager;
use std::time::{Duration, Instant};
use windows::Win32::Foundation::POINT;
use windows::Win32::UI::WindowsAndMessaging::{
    GetCursorPos, GetSystemMetrics, SM_CXVIRTUALSCREEN, SM_CYVIRTUALSCREEN,
    SM_XVIRTUALSCREEN, SM_YVIRTUALSCREEN,
};

#[derive(PartialEq)]
enum MonitorState {
    Idle,
    Counting(Instant),
    // POINTはPartialEqを実装していないため、x, y座標を個別に保持する
    ScreenOff(i32, i32),
}

pub struct MouseMonitor {
    state: MonitorState,
    screen_manager: BlackScreenManager,
}

impl MouseMonitor {
    pub fn new() -> Self {
        Self {
            state: MonitorState::Idle,
            screen_manager: BlackScreenManager::new(),
        }
    }

    pub unsafe fn tick(&mut self) {
        let mut current_pos = POINT::default();
        if GetCursorPos(&mut current_pos).is_err() { return; }

        let sx = GetSystemMetrics(SM_XVIRTUALSCREEN);
        let sy = GetSystemMetrics(SM_YVIRTUALSCREEN);
        let sw = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        let sh = GetSystemMetrics(SM_CYVIRTUALSCREEN);

        match self.state {
            MonitorState::Idle => {
                if self.is_in_corner(current_pos, sx, sy, sw, sh) {
                    self.state = MonitorState::Counting(Instant::now());
                }
            }
            MonitorState::Counting(start_time) => {
                if !self.is_in_corner(current_pos, sx, sy, sw, sh) {
                    self.state = MonitorState::Idle;
                } else if start_time.elapsed() >= Duration::from_millis(config::CORNER_TIMEOUT_MS) {
                    self.state = MonitorState::ScreenOff(current_pos.x, current_pos.y);
                    self.screen_manager.show();
                }
            }
            MonitorState::ScreenOff(saved_x, saved_y) => {
                let dx = (current_pos.x - saved_x).abs();
                let dy = (current_pos.y - saved_y).abs();
                if dx > config::WAKE_THRESHOLD_PX || dy > config::WAKE_THRESHOLD_PX {
                    self.screen_manager.hide();
                    self.state = MonitorState::Idle;
                }
            }
        }
    }

    fn is_in_corner(&self, pos: POINT, sx: i32, sy: i32, sw: i32, sh: i32) -> bool {
        let t = config::CORNER_THRESHOLD_PX;
        let top_left = pos.x < sx + t && pos.y < sy + t;
        let top_right = pos.x > sx + sw - t && pos.y < sy + t;
        let bottom_left = pos.x < sx + t && pos.y > sy + sh - t;
        let bottom_right = pos.x > sx + sw - t && pos.y > sy + sh - t;
        top_left || top_right || bottom_left || bottom_right
    }
}
```

screen_off.rs

```text
use crate::config;
use windows::core::{w, PCWSTR};
use windows::Win32::Foundation::{HWND, LPARAM, LRESULT, WPARAM};
use windows::Win32::Graphics::Gdi::{GetStockObject, BLACK_BRUSH, HBRUSH};
use windows::Win32::UI::WindowsAndMessaging::{
    CreateWindowExW, DefWindowProcW, DestroyWindow, GetSystemMetrics, RegisterClassW,
    ShowWindow, CS_HREDRAW, CS_VREDRAW, SM_CXVIRTUALSCREEN, SM_CYVIRTUALSCREEN,
    SM_XVIRTUALSCREEN, SM_YVIRTUALSCREEN, SW_HIDE, SW_SHOW, WNDCLASSW, WS_EX_TOOLWINDOW,
    WS_EX_TOPMOST, WS_POPUP,
};

pub struct BlackScreenManager {
    hwnd: Option<HWND>,
}

impl BlackScreenManager {
    pub fn new() -> Self {
        Self { hwnd: None }
    }

    unsafe fn create_window(&mut self) {
        let instance = windows::Win32::System::LibraryLoader::GetModuleHandleW(None)
            .expect("Failed to get module handle");

        let class_name_wide: Vec<u16> = config::BLACK_SCREEN_CLASS
            .encode_utf16().chain(std::iter::once(0)).collect();
        let class_name = PCWSTR(class_name_wide.as_ptr());

        let wnd_class = WNDCLASSW {
            style: CS_HREDRAW | CS_VREDRAW,
            lpfnWndProc: Some(Self::black_window_proc),
            hInstance: instance.into(),
            hbrBackground: HBRUSH(GetStockObject(BLACK_BRUSH).0),
            lpszClassName: class_name,
            ..Default::default()
        };

        // クラスが既に登録されている場合はエラーになるが、無視して進める
        let _ = RegisterClassW(&wnd_class);

        let x = GetSystemMetrics(SM_XVIRTUALSCREEN);
        let y = GetSystemMetrics(SM_YVIRTUALSCREEN);
        let width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        let height = GetSystemMetrics(SM_CYVIRTUALSCREEN);

        let hwnd = CreateWindowExW(
            WS_EX_TOPMOST | WS_EX_TOOLWINDOW,
            class_name,
            w!("BlackScreen"),
            WS_POPUP,
            x, y, width, height,
            None, None, instance, None,
        );

        if hwnd.0 != 0 { self.hwnd = Some(hwnd); }
    }

    pub unsafe fn show(&mut self) {
        if self.hwnd.is_none() { self.create_window(); }
        if let Some(hwnd) = self.hwnd { ShowWindow(hwnd, SW_SHOW); }
    }

    pub unsafe fn hide(&mut self) {
        if let Some(hwnd) = self.hwnd { ShowWindow(hwnd, SW_HIDE); }
    }

    unsafe extern "system" fn black_window_proc(
        hwnd: HWND, msg: u32, wparam: WPARAM, lparam: LPARAM
    ) -> LRESULT {
        DefWindowProcW(hwnd, msg, wparam, lparam)
    }
}

impl Drop for BlackScreenManager {
    fn drop(&mut self) {
        if let Some(hwnd) = self.hwnd { unsafe { let _ = DestroyWindow(hwnd); } }
    }
}
```

suppress.rs

```text
// windows 0.52 では SetThreadExecutionState 関連は Power モジュールにあります
use windows::Win32::System::Power::{
    SetThreadExecutionState, ES_CONTINUOUS, ES_DISPLAY_REQUIRED, ES_SYSTEM_REQUIRED,
    EXECUTION_STATE,
};

/// スクリーンセーバーとスリープを抑止します
pub fn suppress_screensaver() {
    unsafe {
        // 画面とシステムのアイドルタイマーをリセットし、抑止状態を維持するフラグを設定
        let flags: EXECUTION_STATE = ES_CONTINUOUS | ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED;
        SetThreadExecutionState(flags);
    }
}

/// 抑止状態を解除し、OSの既定設定に戻します
pub fn reset_suppression() {
    unsafe {
        // ES_CONTINUOUS のみを渡すことで、以前に設定した REQUIRED フラグを解除します
        SetThreadExecutionState(ES_CONTINUOUS);
    }
}
```

tray.rs

```text
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
```

