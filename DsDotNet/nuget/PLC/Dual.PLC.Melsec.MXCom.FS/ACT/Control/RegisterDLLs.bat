@echo off
setlocal enabledelayedexpansion

:: 현재 실행되는 배치 파일의 폴더 경로 설정
set DLL_PATH=%~dp0

:: regsvr32 실행 파일 경로 (64비트)
set REGSVR32_PATH=C:\Windows\System32\regsvr32.exe

:: 로그 파일 설정
set LOG_FILE=%DLL_PATH%\RegisterDLLs.log
echo --- DLL Registration Start: %DATE% %TIME% --- > "%LOG_FILE%"
echo --- DLL Registration Start: %DATE% %TIME% ---

:: 관리자 권한 확인
openfiles >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Administrator privileges are required! >> "%LOG_FILE%"
    echo [ERROR] Administrator privileges are required! Please run as administrator.
    pause
    exit /b
)

:: 현재 폴더 내 모든 DLL 파일을 등록 (실패해도 계속 진행)
for %%f in ("%DLL_PATH%\*.dll") do (
    %REGSVR32_PATH% /s /i "%%f" >> "%LOG_FILE%" 2>&1
    if !errorlevel! neq 0 (
        echo [FAILED] %%f >> "%LOG_FILE%"
        echo [FAILED] %%f
    ) else (
        echo [SUCCESS] %%f >> "%LOG_FILE%"
        echo [SUCCESS] %%f
    )
)

echo --- DLL Registration Complete: %DATE% %TIME% --- >> "%LOG_FILE%"
echo --- DLL Registration Complete: %DATE% %TIME% ---
echo DLL Registration completed. Check the log file: "%LOG_FILE%"
pause
