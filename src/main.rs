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
