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

echo === Detecting versions ===

for /f "usebackq delims=" %%a in (`dotnet msbuild "%ROOT%src\Zaya.Screenshot\Zaya.Screenshot.csproj" -getProperty:Version -nologo -v:q`) do set IFACE=%%a
set IFACE=!IFACE: =!
if "!IFACE!"=="" set IFACE=1.0.0

for /f "tokens=1,2 delims=." %%a in ("!IFACE!") do set CHANNEL=%%a.%%b
if "!CHANNEL!"=="." set CHANNEL=1.0

for /f "usebackq delims=" %%a in (`dotnet msbuild "%ROOT%src\Zaya.Screenshot.Impl.Windows\Zaya.Screenshot.Impl.Windows.csproj" -getProperty:Version -nologo -v:q`) do set VER_WIN=%%a
set VER_WIN=!VER_WIN: =!
if "!VER_WIN!"=="" set VER_WIN=!IFACE!

set MAXVER=!VER_WIN!

echo   Interface=!IFACE!  UpdateChannel=!CHANNEL!  Plugin=!VER_WIN!

echo === Preparing output directory ===

rmdir /s /q "%ROOT%out" 2>nul
mkdir "%ROOT%out" 2>nul

echo !MAXVER!>"%ROOT%out\version.txt"
echo !CHANNEL!>"%ROOT%out\channel.txt"
del "%ROOT%out\plugins.versions.txt" 2>nul

> "%ROOT%out\interfaces.json" (
echo [
echo   {"interface":"Zaya.Screenshot","channel":"!CHANNEL!","version":"!MAXVER!","assets":["Zaya.Screenshot.Impl.Windows.zip"]}
echo ]
)

echo === Creating plugin.zip ===

rmdir /s /q "%STAGEDIR%" 2>nul
mkdir "%STAGEDIR%"

set TFM_DIR=%ROOT%src\Zaya.Screenshot.Impl.Windows\bin\%BUILD_CONFIG%\net8.0-windows10.0.19041.0

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
echo   "interfaceVersion": "!IFACE!",>>"%PLUGIN_JSON%"
echo   "pluginVersion": "!VER_WIN!">>"%PLUGIN_JSON%"
echo }>>"%PLUGIN_JSON%"

set PLUGIN_ZIP=Zaya.Screenshot.Impl.Windows.zip
powershell -Command "Compress-Archive -Path '%STAGEDIR%\*' -DestinationPath '%ROOT%out\%PLUGIN_ZIP%' -Force"
echo   out\%PLUGIN_ZIP%  pluginVersion=!VER_WIN!
echo %PLUGIN_ZIP%=!VER_WIN!>>"%ROOT%out\plugins.versions.txt"

echo === Packing NuGet packages ===

dotnet pack "%ROOT%src\Zaya.Screenshot.Impl.Windows\Zaya.Screenshot.Impl.Windows.csproj" -c %BUILD_CONFIG% -o "%ROOT%out" --no-build
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

dotnet pack "%ROOT%src\Zaya.Screenshot\Zaya.Screenshot.csproj" -c %BUILD_CONFIG% -o "%ROOT%out" --no-build
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Cleaning up ===

rmdir /s /q "%STAGEDIR%" 2>nul

echo === Done: interface !IFACE! updateChannel !CHANNEL! release !MAXVER! ===
goto :eof

:CopySatellites
    for /d %%d in ("%~1\*") do (
        if exist "%%d\*.resources.dll" (
            mkdir "%~2\%%~nxd" 2>nul
            copy /y "%%d\*" "%~2\%%~nxd\"
        )
    )
    exit /b
