@echo off
setlocal enabledelayedexpansion

set ROOT=%~dp0
set BUILD_DIR=%TEMP%\Zaya.Screenshot\build
set STAGEDIR=%TEMP%\Zaya.Screenshot\staging

if "%CI%"=="true" (
    set BUILD_CONFIG=Release
) else (
    set BUILD_CONFIG=Debug
)

echo === Building Zaya.Screenshot.Impl.Windows (%BUILD_CONFIG%) ===

rmdir /s /q "%BUILD_DIR%" 2>nul

dotnet publish "%ROOT%src\Zaya.Screenshot.Impl.Windows\Zaya.Screenshot.Impl.Windows.csproj" -c %BUILD_CONFIG% -o "%BUILD_DIR%" --no-self-contained
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

set TFM_DIR=%BUILD_DIR%

echo === Copying DLLs ===

rmdir /s /q "%STAGEDIR%" 2>nul
mkdir "%STAGEDIR%"

copy /y "%TFM_DIR%\Zaya.Screenshot.Impl.Windows.dll" "%STAGEDIR%"
if %ERRORLEVEL% neq 0 (
    echo ERROR: DLL not found
    exit /b 1
)

echo === Detecting versions ===

for /f "tokens=*" %%a in ('findstr /i "<Version>" "%ROOT%src\Zaya.Screenshot\Zaya.Screenshot.csproj"') do set INF_LINE=%%a
set INF_LINE=!INF_LINE:^<Version^>=!
set INF_LINE=!INF_LINE:^</Version^>=!
set INF_MAJOR=!INF_LINE:~0,1!
if "!INF_MAJOR!"=="" set INF_MAJOR=1

for /f "tokens=*" %%a in ('findstr /i "<Version>" "%ROOT%src\Zaya.Screenshot.Impl.Windows\Zaya.Screenshot.Impl.Windows.csproj"') do set IMPL_LINE=%%a
set IMPL_LINE=!IMPL_LINE:^<Version^>=!
set IMPL_LINE=!IMPL_LINE:^</Version^>=!
if "!IMPL_LINE!"=="" set IMPL_LINE=1.0.0

echo === Generating plugin.json ===

set PLUGIN_JSON=%STAGEDIR%\plugin.json

echo {>"%PLUGIN_JSON%"
echo   "id": "GraphicsCapture",>>"%PLUGIN_JSON%"
echo   "type": "capture",>>"%PLUGIN_JSON%"
echo   "interface": "Zaya.Screenshot",>>"%PLUGIN_JSON%"
echo   "interfaceVersion": "!INF_MAJOR!.0.0",>>"%PLUGIN_JSON%"
echo   "pluginVersion": "!IMPL_LINE!">>"%PLUGIN_JSON%"
echo }>>"%PLUGIN_JSON%"

set PLUGIN_ZIP=Zaya.Screenshot.Impl.Windows-!IMPL_LINE!.zip
echo === Creating plugin.zip ===

rmdir /s /q "%ROOT%out" 2>nul
mkdir "%ROOT%out" 2>nul
powershell -Command "Compress-Archive -Path '%STAGEDIR%\*' -DestinationPath '%ROOT%out\%PLUGIN_ZIP%' -Force"
echo   out\%PLUGIN_ZIP%

echo === Packing NuGet packages ===

dotnet pack "%ROOT%src\Zaya.Screenshot\Zaya.Screenshot.csproj" -c %BUILD_CONFIG% -o "%ROOT%out" --no-build
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Cleaning up ===

rmdir /s /q "%STAGEDIR%" 2>nul
rmdir /s /q "%BUILD_DIR%" 2>nul

echo === Done: version !IMPL_LINE! ===
