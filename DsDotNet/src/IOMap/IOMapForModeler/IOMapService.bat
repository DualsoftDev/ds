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
REM Install the service (assuming your service executable is named IOMap.exe)
sc create %SERVICE_NAME% binPath= %~dp0IOMapService.exe start= auto
REM 서비스가 실패할 경우 5초 후에 서비스를 자동으로 재시작합니다. reset= 60은 60초 동안 문제가 발생하지 않을 경우 실패 카운터를 재설정
sc failure %SERVICE_NAME% reset= 60 actions= restart/5000
REM Start the service
sc start %SERVICE_NAME%

REM 서비스 설명 설정
set SERVICE_DESCRIPTION=Dualsoft memory IO service 

REM 서비스 설명 업데이트
sc description %SERVICE_NAME% "%SERVICE_DESCRIPTION%"

timeout /t 20 /nobreak

echo end!
REM  pause
