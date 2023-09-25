@echo off
REM Check if script is run as administrator
NET SESSION >nul 2>&1
if %errorLevel% == 0 (
    echo Running as administrator
) else (
    echo Please run this script as an administrator!
    pause >nul
    exit
)

REM 서비스 이름 설정
set SERVICE_NAME=IOMapService

REM 서비스가 존재하는지 확인
sc query %SERVICE_NAME% >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo The service %SERVICE_NAME% does not exist.
    
)

REM 서비스 중지
echo Stopping the service %SERVICE_NAME%...
sc stop %SERVICE_NAME%

REM 서비스가 완전히 중지될 때까지 대기 (예: 2초 대기)
timeout /t 2 /nobreak

REM 서비스 삭제
echo Deleting the service %SERVICE_NAME%...
sc delete %SERVICE_NAME%

REM 결과 확인
if %ERRORLEVEL% equ 0 (
    echo The service %SERVICE_NAME% was deleted successfully.
) else (
    echo Failed to delete the service %SERVICE_NAME%.
)

echo Data exePath: %~dp0IOMapService.exe

timeout /t 2 /nobreak

echo end!
REM pause
