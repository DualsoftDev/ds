@echo off
setlocal enabledelayedexpansion

:: Define source and target templates
@REM set "TEMPLATES=./Apps/CommonAppSettings.json.template ./Apps/DSPilot/DSPilot.Winform.Controller/App.config.template ./Apps/HMI.Obsolete/AppHMI/App.config.template ./Apps/OfficeAddIn/PowerPointAddInHelper/App.config.template ./Apps/DSpa/DSpa/appsettings.json.template ./src/IOHub/IO.Core/zmqsettings.json.template ./src/IOHub/ThirdParty/LS/IOClient.Xgi/zmqhw.json.template ./src/UnitTest/Engine.TestSimulator/App.config.template ./src/Web/DsWebApp.Server/appsettings.json.template"

set TEMPLATES=^
    Apps\CommonAppSettings.json.template ^
    Apps\DSPilot\DSPilot.Winform.Controller\App.config.template ^
    Apps\HMI.Obsolete\AppHMI\App.config.template ^
    Apps\OfficeAddIn\PowerPointAddInHelper\App.config.template ^
    Apps\DSpa\DSpa\appsettings.json.template ^
    src\IOHub\IO.Core\zmqsettings.json.template ^
    src\IOHub\ThirdParty\LS\IOClient.Xgi\zmqhw.json.template ^
    src\UnitTest\Engine.TestSimulator\App.config.template ^
    src\Web\DsWebApp.Server\appsettings.json.template

:: Process each template
for %%T in (%TEMPLATES%) do (
    set "TARGET=%%~dpnT"

    :: Check if source file exists
    if not exist "%%T" (
        echo "[ERROR] Source file %%T does not exist."
        exit /b 1
    )

    :: Check if target file already exists
    if exist "!TARGET!" (
        echo :: Skip !TARGET! -- already exists.
    ) else (
        :: Copy source to target
        copy "%%T" "!TARGET!" >nul
        if errorlevel 1 (
            echo "Failed to copy %%T to !TARGET!"
            exit /b 1
        )
        echo Copied !TARGET!
    )
)

endlocal


@REM exit











@REM @echo off
@REM setlocal enabledelayedexpansion

@REM :: 템플릿 파일 목록 (경로 수정)
@REM set templates=^
@REM     Apps\CommonAppSettings.json.template ^
@REM     Apps\DSPilot\DSPilot.Winform.Controller\App.config.template ^
@REM     Apps\HMI.Obsolete\AppHMI\App.config.template ^
@REM     Apps\OfficeAddIn\PowerPointAddInHelper\App.config.template ^
@REM     Apps\DSpa\DSpa\appsettings.json.template ^
@REM     src\IOHub\IO.Core\zmqsettings.json.template ^
@REM     src\IOHub\ThirdParty\LS\IOClient.Xgi\zmqhw.json.template ^
@REM     src\UnitTest\Engine.TestSimulator\App.config.template ^
@REM     src\Web\DsWebApp.Server\appsettings.json.template

@REM :: 각 템플릿 파일을 확인하고 복사
@REM for %%T in (%templates%) do (
@REM     echo Processing "%%T"
    
@REM     if exist "%%T" (
@REM         set "target=%%~dpnT"
@REM         set "target=!target:.template=!"

@REM         set dummyEval="%%T"
@REM         set dummyEval2="!target!"
@REM         ::echo !target!  <-- 여기를 추가해서 값이 유지되는지 확인
@REM         ::echo [DEBUG] Template Exists: "%%T"
@REM         ::rem echo [DEBUG] Target Path: "!target!"

@REM         if exist "!target!" (
@REM             echo Exists "!target!"
@REM         ) else (
@REM             copy "%%T" "!target!"
@REM             echo Copied "%%T" to "!target!"
@REM         )
@REM     ) else (
@REM         echo [ERROR] File not found: "%%T"
@REM     )
@REM )

@REM endlocal
