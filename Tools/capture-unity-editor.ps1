# Unity 에디터 전체 창을 캡처해 PNG로 저장한다.
# 사용: .\Tools\capture-unity-editor.ps1 -OutFile "Docs/Media/2026-07-30_F02_editor.png"
# 주의: Unity 창이 최소화되어 있으면 안 되고, 다른 창에 가려진 부분은 가린 창이 찍힌다.
param(
    [Parameter(Mandatory = $true)][string]$OutFile
)

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class Win32Capture {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
"@

$unity = Get-Process -Name "Unity" -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
if ($null -eq $unity) {
    Write-Error "Unity 에디터 프로세스를 찾을 수 없습니다."
    exit 1
}

[Win32Capture]::SetForegroundWindow($unity.MainWindowHandle) | Out-Null
Start-Sleep -Milliseconds 600

$rect = New-Object Win32Capture+RECT
[Win32Capture]::GetWindowRect($unity.MainWindowHandle, [ref]$rect) | Out-Null
$width = $rect.Right - $rect.Left
$height = $rect.Bottom - $rect.Top
if ($width -le 0 -or $height -le 0) {
    Write-Error "Unity 창 크기를 읽지 못했습니다 (최소화 상태?)."
    exit 1
}

$bmp = New-Object System.Drawing.Bitmap($width, $height)
$gfx = [System.Drawing.Graphics]::FromImage($bmp)
$gfx.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bmp.Size)
$gfx.Dispose()

$dir = Split-Path -Parent $OutFile
if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Force $dir | Out-Null }
$bmp.Save($OutFile, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Output "saved: $OutFile ($width x $height)"
