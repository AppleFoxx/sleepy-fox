@echo off
rem ---------------------------------------------------------------------------
rem  The Sleepy Fox - build with the compiler built into .NET Framework 4.8.
rem  No Visual Studio, no NuGet packages, no internet access required.
rem
rem  Usage: double-click build.cmd, or run it from a command prompt.
rem  Result: bin\SleepyFox.exe
rem ---------------------------------------------------------------------------
setlocal
cd /d "%~dp0"

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if not exist "%CSC%" (
    echo [SleepyFox] ERROR: compiler not found:
    echo [SleepyFox]   "%CSC%"
    echo [SleepyFox] Install .NET Framework 4.8, or use the 32-bit path
    echo [SleepyFox]   %%WINDIR%%\Microsoft.NET\Framework\v4.0.30319\csc.exe
    exit /b 1
)

if not exist "bin" mkdir "bin"

echo [SleepyFox] Compiling src\*.cs ...
"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 ^
    /out:bin\SleepyFox.exe ^
    /win32manifest:src\app.manifest ^
    /reference:System.dll ^
    /reference:System.Windows.Forms.dll ^
    /reference:System.Drawing.dll ^
    src\*.cs

if errorlevel 1 (
    echo [SleepyFox] ERROR: build failed.
    exit /b 1
)

echo [SleepyFox] OK: bin\SleepyFox.exe
endlocal
