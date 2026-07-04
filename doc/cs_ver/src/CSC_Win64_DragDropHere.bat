@echo off

:: 遅延展開 バグの原因になる場合があるので注意;
setlocal enabledelayedexpansion

:: エスケープシーケンスを登録;
for /F "tokens=1,2 delims=#" %%a in ('"prompt #$H#$E# & echo on & for %%b in (1) do rem"') do (set ESC=%%b)

:: UTF-8などで保存されたバッチファイルのShiftJis対策;
chcp 65001 > nul

:: 実行時のフォルダに移動;
pushd "%~dp0"

:: コマンドプロンプトを変更;
set prompt=%ESC%[104m$P$G%ESC%[0m

:: C# のコンパイラ
:: 32bit
:: set CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
:: 64bit
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

:: 引数１が空でなければ、拡張子を".cs"に仮定変更してCSCに渡す;
if "%1" == "" (goto :EndProcess)
@echo on
%CSC% /t:winexe %~n1.cs /resource:%0 /resource:%~n1.cs /win32icon:images\%~n1.ico /platform:x64
@echo off

echo %~n1|clip

:: 終了処理（エクスプローラーから起動されていたらプロンプト表示）;
:EndProcess
echo %cmdcmdline% | find /i "%~f0" > nul
if %errorlevel% equ 0 (cmd /k)
