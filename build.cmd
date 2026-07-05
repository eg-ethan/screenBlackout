@echo off
rem Builds ScreenBlackout.exe using the C# compiler that ships with Windows (.NET Framework 4.x).
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
    echo ERROR: csc.exe not found. .NET Framework 4.x is required.
    exit /b 1
)

"%CSC%" /nologo /target:winexe /optimize+ /out:ScreenBlackout.exe ^
    /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll ^
    ScreenBlackout.cs

if errorlevel 1 (
    echo BUILD FAILED
    exit /b 1
)
echo Built ScreenBlackout.exe
endlocal
