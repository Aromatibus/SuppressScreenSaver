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
