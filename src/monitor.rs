use crate::config;
use crate::screen_off::BlackScreenManager;
use std::time::{Duration, Instant};
use windows::Win32::Foundation::POINT;
use windows::Win32::System::SystemInformation::GetTickCount;
use windows::Win32::UI::Input::KeyboardAndMouse::{GetLastInputInfo, LASTINPUTINFO};
use windows::Win32::UI::WindowsAndMessaging::{
    GetCursorPos, GetSystemMetrics, SM_CXVIRTUALSCREEN, SM_CYVIRTUALSCREEN,
    SM_XVIRTUALSCREEN, SM_YVIRTUALSCREEN,
};

#[derive(PartialEq)]
enum MonitorState {
    Idle,
    Counting(Instant),
    // 復帰判定用: 暗転開始時のマウスX, マウスY, 最終入力時刻(ms)
    ScreenOff(i32, i32, u32),
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

        let mut lii = LASTINPUTINFO::default();
        lii.cbSize = std::mem::size_of::<LASTINPUTINFO>() as u32;
        if GetLastInputInfo(&mut lii).is_err() { return; }

        let now_ms = GetTickCount();
        let idle_time_ms = (now_ms - lii.dwTime) as u64;

        match self.state {
            MonitorState::Idle => {
                self.handle_idle_state(current_pos, lii.dwTime, idle_time_ms);
            }
            MonitorState::Counting(start_time) => {
                self.handle_counting_state(current_pos, lii.dwTime, idle_time_ms, start_time);
            }
            MonitorState::ScreenOff(saved_x, saved_y, saved_input_time) => {
                self.handle_screen_off_state(current_pos, lii.dwTime, saved_x, saved_y, saved_input_time);
            }
        }
    }

    // 通常時の判定
    unsafe fn handle_idle_state(&mut self, pos: POINT, input_time: u32, idle_ms: u64) {
        if self.is_in_corner(pos) {
            self.state = MonitorState::Counting(Instant::now());
        } else if idle_ms >= config::IDLE_TIMEOUT_MS {
            self.transition_to_screen_off(pos, input_time);
        }
    }

    // 四隅カウント中の判定
    unsafe fn handle_counting_state(&mut self, pos: POINT, input_time: u32, idle_ms: u64, start: Instant) {
        if idle_ms >= config::IDLE_TIMEOUT_MS {
            self.transition_to_screen_off(pos, input_time);
        } else if !self.is_in_corner(pos) {
            self.state = MonitorState::Idle;
        } else if start.elapsed() >= Duration::from_millis(config::CORNER_TIMEOUT_MS) {
            self.transition_to_screen_off(pos, input_time);
        }
    }

    // 暗転中の復帰判定
    unsafe fn handle_screen_off_state(&mut self, pos: POINT, input_time: u32, sx: i32, sy: i32, st: u32) {
        if input_time == st { return; }

        let dx = (pos.x - sx).abs();
        let dy = (pos.y - sy).abs();
        let moved = dx > config::WAKE_THRESHOLD_PX || dy > config::WAKE_THRESHOLD_PX;
        let is_static_input = dx == 0 && dy == 0;

        // 指定距離以上の移動、または座標移動のない入力（キーボード/クリック）で復帰
        if moved || is_static_input {
            self.screen_manager.hide();
            self.state = MonitorState::Idle;
        } else {
            // しきい値未満の微細なマウス移動の場合は、入力時刻だけ更新して暗転を維持
            self.state = MonitorState::ScreenOff(sx, sy, input_time);
        }
    }

    unsafe fn transition_to_screen_off(&mut self, pos: POINT, input_time: u32) {
        self.state = MonitorState::ScreenOff(pos.x, pos.y, input_time);
        self.screen_manager.show();
    }

    fn is_in_corner(&self, pos: POINT) -> bool {
        unsafe {
            let sx = GetSystemMetrics(SM_XVIRTUALSCREEN);
            let sy = GetSystemMetrics(SM_YVIRTUALSCREEN);
            let sw = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            let sh = GetSystemMetrics(SM_CYVIRTUALSCREEN);

            let t = config::CORNER_THRESHOLD_PX;
            let top_left = pos.x < sx + t && pos.y < sy + t;
            let top_right = pos.x > sx + sw - t && pos.y < sy + t;
            let bottom_left = pos.x < sx + t && pos.y > sy + sh - t;
            let bottom_right = pos.x > sx + sw - t && pos.y > sy + sh - t;
            top_left || top_right || bottom_left || bottom_right
        }
    }
}
