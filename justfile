# Windows 用　常駐型スクリーンセイバー抑制プログラム

# PowerShell をデフォルトシェルに設定して 'sh' 不足のエラーを回避します
set shell := ["powershell.exe", "-NoProfile", "-Command"]

# レシピ一覧
default:
    @just --list

# デバッグビルドと実行
run:
    cargo run

# リリースビルド
release:
    cargo build --release --target x86_64-pc-windows-msvc
    @Write-Host "ビルド完了: .\target\x86_64-pc-windows-msvc\release\SuppressScreenSaver.exe"

# クリーンアップ
clean:
    if (Test-Path "target") { Remove-Item -Recurse -Force "target" }
    cargo clean
