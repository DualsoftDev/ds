@echo off
setlocal enabledelayedexpansion

:: 폴더 경로 설정 (현재 폴더)
set DLL_PATH=%~dp0

:: regsvr32 실행 파일 경로 (64비트)
set REGSVR32_PATH=C:\Windows\System32\regsvr32.exe

:: 로그 파일 생성
set LOG_FILE=%DLL_PATH%\RegisterDLLs.log
echo --- DLL 등록 시작: %DATE% %TIME% --- > "%LOG_FILE%"

:: 모든 DLL 파일을 등록 (실패해도 계속 진행)
for %%f in ("%DLL_PATH%\*.dll") do (
    echo Registering: %%f >> "%LOG_FILE%"
    %REGSVR32_PATH% "%%f" >> "%LOG_FILE%" 2>&1
    if !errorlevel! neq 0 (
        echo [실패] %%f >> "%LOG_FILE%"
    ) else (
        echo [성공] %%f >> "%LOG_FILE%"
    )
)

echo --- DLL 등록 완료: %DATE% %TIME% --- >> "%LOG_FILE%"
echo DLL 등록 완료. 로그 파일을 확인하세요: "%LOG_FILE%"
pause
