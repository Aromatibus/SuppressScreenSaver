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
