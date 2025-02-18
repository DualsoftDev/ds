@echo off
cd /d "%~dp0"

rem 현재 실행된 bat 파일의 디렉터리 내 Control\Wrapper 폴더 경로를 변수에 저장
set CONTROL_PATH=%~dp0Control\
set WRAPPER_PATH=%~dp0Control\Wrapper\

set CLSID_PATH1=HKEY_CLASSES_ROOT\WOW6432Node\CLSID\{F0B1A112-BFCB-4DA3-9535-C296D69A17E0}
set CLSID_PATH2=HKEY_CLASSES_ROOT\WOW6432Node\CLSID\{174DD3F4-961E-4833-A4D2-6BFFE5DDFC6C}

rem ====== 첫 번째 CLSID {F0B1A112-BFCB-4DA3-9535-C296D69A17E0} 등록 ======
reg add "%CLSID_PATH1%" /ve /t REG_SZ /d "ActUtlWrap Class" /f
reg add "%CLSID_PATH1%\LocalServer32" /ve /t REG_SZ /d "\"%CONTROL_PATH%ActUtlType64.exe\"" /f
reg add "%CLSID_PATH1%\LocalServer32" /v "ServerExecutable" /t REG_SZ /d "%CONTROL_PATH%ActUtlType64.exe" /f
reg add "%CLSID_PATH1%\ProgID" /ve /t REG_SZ /d "ActUtlType64.ActUtlWrap.1" /f
reg add "%CLSID_PATH1%\TypeLib" /ve /t REG_SZ /d "{719A5FAB-EB1C-4B52-98BC-F8C9F6912D04}" /f
reg add "%CLSID_PATH1%\Version" /ve /t REG_SZ /d "1.0" /f
reg add "%CLSID_PATH1%\VersionIndependentProgID" /ve /t REG_SZ /d "ActUtlType64.ActUtlWrap" /f

rem ====== 두 번째 CLSID {174DD3F4-961E-4833-A4D2-6BFFE5DDFC6C} 등록 ======
reg add "%CLSID_PATH2%" /ve /t REG_SZ /d "PSFactoryBuffer" /f
reg add "%CLSID_PATH2%\InProcServer32" /ve /t REG_SZ /d "%CONTROL_PATH%ActUtlType64PS.dll" /f
reg add "%CLSID_PATH2%\InProcServer32" /v "ThreadingModel" /t REG_SZ /d "Both" /f

rem ====== Control\Wrapper 폴더 내 DLL 파일들 등록 ======
@echo off
cd /d "%~dp0"

rem 현재 실행된 bat 파일의 디렉터리 내 Control\Wrapper 폴더 경로 설정
set WRAPPER_PATH=%~dp0Control\Wrapper\

rem 관리자 권한 확인
openfiles >nul 2>&1
if %errorlevel% neq 0 (
    echo This script must be run as an administrator.
    echo Please right-click the batch file and select "Run as administrator."
    pause
    exit /b
)
rem ====== Control\Wrapper 폴더 내 DLL 파일들을 regsvr32로 등록 ======
for %%F in (
    ActProgDataLogging64PS.dll
    ActProgType64PS.dll
    ActSupportMsg64.dll
    ActSupportMsg64PS.dll
    ActUtlDataLogging64PS.dll
    ActUtlType64PS.dll
) do (
    if exist "%WRAPPER_PATH%%%F" (
        regsvr32 /s /i "%WRAPPER_PATH%%%F"
        if %errorlevel% neq 0 (
            echo [ERR] %%F regsvr32 fail!
        ) else (
            echo [OK] %%F regsvr32 ok.
        )
    ) else (
        echo [NG] %%F not found!
    )
)

echo regsvr32 process complete.
pause