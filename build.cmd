@echo off
setlocal enabledelayedexpansion

set ROOT=%~dp0
set STAGEDIR=%TEMP%\Zaya.Screenshot\staging

if "%CI%"=="true" (
    set BUILD_CONFIG=Release
) else (
    set BUILD_CONFIG=Debug
)

echo === Building Zaya.Screenshot.Impl.Windows (%BUILD_CONFIG%) ===

dotnet build "%ROOT%src\Zaya.Screenshot.Impl.Windows\Zaya.Screenshot.Impl.Windows.csproj" -c %BUILD_CONFIG%
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Detecting version ===

for /f "usebackq delims=" %%a in (`dotnet msbuild "%ROOT%src\Zaya.Screenshot.Impl.Windows\Zaya.Screenshot.Impl.Windows.csproj" -getProperty:Version -nologo -v:q`) do set VER=%%a
set VER=!VER: =!
if "!VER!"=="" set VER=0.4.0

for /f "tokens=1,2,3 delims=." %%a in ("!VER!") do (
    set VER_MAJOR=%%a
    set VER_MINOR=%%b
    set VER_PATCH=%%c
)
set CHANNEL=!VER_MAJOR!.!VER_MINOR!
echo   Version=!VER!  Channel=!CHANNEL!

echo === Preparing output directory ===

rmdir /s /q "%ROOT%out" 2>nul
mkdir "%ROOT%out" 2>nul

echo !VER!>"%ROOT%out\version.txt"
echo !CHANNEL!>"%ROOT%out\channel.txt"

echo === Creating plugin.zip ===

rmdir /s /q "%STAGEDIR%" 2>nul
mkdir "%STAGEDIR%"

set TFM_DIR=%ROOT%src\Zaya.Screenshot.Impl.Windows\bin\%BUILD_CONFIG%\net8.0-windows10.0.22621.0

copy /y "%TFM_DIR%\Zaya.Screenshot.Impl.Windows.dll" "%STAGEDIR%"
if %ERRORLEVEL% neq 0 (
    echo ERROR: DLL not found
    exit /b 1
)

call :CopySatellites "%TFM_DIR%" "%STAGEDIR%"

set PLUGIN_JSON=%STAGEDIR%\plugin.json

echo {>"%PLUGIN_JSON%"
echo   "id": "GraphicsCapture",>>"%PLUGIN_JSON%"
echo   "type": "capture",>>"%PLUGIN_JSON%"
echo   "interface": "Zaya.Screenshot",>>"%PLUGIN_JSON%"
echo   "interfaceVersion": "!VER!",>>"%PLUGIN_JSON%"
echo   "pluginVersion": "!VER!",>>"%PLUGIN_JSON%"
echo   "primitivesChannel": "!CHANNEL!">>"%PLUGIN_JSON%"
echo }>>"%PLUGIN_JSON%"

REM Stable asset name (no version in filename) for host updater.
set PLUGIN_ZIP=Zaya.Screenshot.Impl.Windows.zip
powershell -Command "Compress-Archive -Path '%STAGEDIR%\*' -DestinationPath '%ROOT%out\%PLUGIN_ZIP%' -Force"
echo   out\%PLUGIN_ZIP%

echo === Packing NuGet packages ===

dotnet pack "%ROOT%src\Zaya.Screenshot.Impl.Windows\Zaya.Screenshot.Impl.Windows.csproj" -c %BUILD_CONFIG% -o "%ROOT%out" --no-build
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

dotnet pack "%ROOT%src\Zaya.Screenshot\Zaya.Screenshot.csproj" -c %BUILD_CONFIG% -o "%ROOT%out" --no-build
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Cleaning up ===

rmdir /s /q "%STAGEDIR%" 2>nul

echo === Done: version !VER! channel !CHANNEL! ===
goto :eof

:CopySatellites
    for /d %%d in ("%~1\*") do (
        if exist "%%d\*.resources.dll" (
            mkdir "%~2\%%~nxd" 2>nul
            copy /y "%%d\*" "%~2\%%~nxd\"
        )
    )
    exit /b
