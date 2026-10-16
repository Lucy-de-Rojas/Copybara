@echo off
setlocal
cd /d "%~dp0"
set "CLIP_COMPILER=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CLIP_COMPILER%" set "CLIP_COMPILER=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CLIP_COMPILER%" (
  echo Windows .NET Framework compiler could not be found. Please report this message.
  pause
  exit /b 1
)
echo Building Copybara. If an older version is running, choose Quit app before updating.
"%CLIP_COMPILER%" /nologo /target:winexe /optimize+ /out:Copybara.exe /win32icon:Copybara.ico /resource:Copybara.png,Copybara.Mascot /resource:Copybara.ico,Copybara.Icon /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Security.dll MyClipboard.cs BackupSupport.cs
if errorlevel 1 (
  echo Copybara could not be built. Please send the error above to ChatGPT.
  pause
  exit /b 1
)
start "" "%~dp0Copybara.exe"
