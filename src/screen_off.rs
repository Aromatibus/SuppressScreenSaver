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
