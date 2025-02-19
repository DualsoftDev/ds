@echo off
setlocal

:: 대상 배치 파일 경로 설정
set TARGET_BAT="%~dp0nuget\PLC\Dual.PLC.Melsec.MXCom.FS\ACT\RegisterServicenDLLs.bat"

:: 관리자 권한 확인
openfiles >nul 2>&1
if %errorlevel% neq 0 (
    echo Running with administrator privileges...
    powershell -Command "Start-Process cmd -ArgumentList '/c \"\"%TARGET_BAT%\"\"' -Verb RunAs"
    exit /b
)

:: 관리자 권한으로 실행되었을 경우 직접 실행
echo Executing: %TARGET_BAT%
call %TARGET_BAT%
