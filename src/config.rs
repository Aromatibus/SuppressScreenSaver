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

// 無操作状態で画面OFFにするまでの時間 (ミリ秒)
pub const IDLE_TIMEOUT_MS: u64 = 60000;

// 画面OFFから復帰するために必要なマウス移動距離 (px)
pub const WAKE_THRESHOLD_PX: i32 = 5;

// マウス監視のインターバル (ミリ秒)
pub const MONITOR_INTERVAL_MS: u32 = 100;

// 黒画面ウィンドウのクラス名
pub const BLACK_SCREEN_CLASS: &str = "BlackScreenWindowClass";
