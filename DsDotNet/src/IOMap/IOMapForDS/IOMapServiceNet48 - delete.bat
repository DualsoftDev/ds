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

REM ���� �̸� ����
set SERVICE_NAME=IOMapService

REM ���񽺰� �����ϴ��� Ȯ��
sc query %SERVICE_NAME% >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo The service %SERVICE_NAME% does not exist.
    
)

REM ���� ����
echo Stopping the service %SERVICE_NAME%...
sc stop %SERVICE_NAME%

REM ���񽺰� ������ ������ ������ ��� (��: 2�� ���)
timeout /t 2 /nobreak

REM ���� ����
echo Deleting the service %SERVICE_NAME%...
sc delete %SERVICE_NAME%

REM ��� Ȯ��
if %ERRORLEVEL% equ 0 (
    echo The service %SERVICE_NAME% was deleted successfully.
) else (
    echo Failed to delete the service %SERVICE_NAME%.
)

echo Data exePath: %~dp0..\..\..\bin\net48\IOMapService.exe


echo end!
pause
