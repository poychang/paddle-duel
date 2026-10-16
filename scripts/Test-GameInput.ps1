#requires -Version 5.1
param(
    [Parameter(Mandatory = $true)][string]$ExecutablePath,
    [switch]$AllowInteractiveTest,
    [string]$FailureScreenshotPath
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (-not $AllowInteractiveTest) { throw 'Pass -AllowInteractiveTest to launch and operate the game with real keyboard input.' }
if (Get-Process -Name PaddleDuel.App -ErrorAction SilentlyContinue) { throw 'Close the existing game before testing.' }
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing
Add-Type -ReferencedAssemblies System.Drawing @'
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
public static class GameInputProbe {
    [StructLayout(LayoutKind.Sequential)] struct INPUT { public uint type; public INPUTUNION data; }
    [StructLayout(LayoutKind.Explicit)] struct INPUTUNION {
        [FieldOffset(0)] public KEYBDINPUT keyboard;
        [FieldOffset(0)] public MOUSEINPUT mouse;
    }
    [StructLayout(LayoutKind.Sequential)] struct KEYBDINPUT {
        public ushort key, scan; public uint flags, time; public UIntPtr extra;
    }
    [StructLayout(LayoutKind.Sequential)] struct MOUSEINPUT {
        public int x, y; public uint data, flags, time; public UIntPtr extra;
    }
    [DllImport("user32.dll", SetLastError=true)] static extern uint SendInput(uint count, INPUT[] inputs, int size);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr window, int command);
    delegate bool EnumWindowCallback(IntPtr window, IntPtr parameter);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowCallback callback, IntPtr parameter);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window, out uint pid);
    public static IntPtr WindowForProcess(int processId) {
        IntPtr found=IntPtr.Zero;
        EnumWindows((window, parameter) => {
            uint pid; GetWindowThreadProcessId(window, out pid);
            if (pid==processId && IsWindowVisible(window)) { found=window; return false; }
            return true;
        }, IntPtr.Zero);
        if (found==IntPtr.Zero) throw new InvalidOperationException("No visible game HWND.");
        return found;
    }
    public static uint ForegroundProcess() {
        uint pid; GetWindowThreadProcessId(GetForegroundWindow(), out pid); return pid;
    }
    public static void Key(ushort key, bool down) {
        Keys(new[] { key }, down);
    }
    public static void Keys(ushort[] keys, bool down) {
        var inputs = new INPUT[keys.Length];
        for (int i=0; i<keys.Length; i++)
            inputs[i] = new INPUT { type=1, data=new INPUTUNION {
                keyboard=new KEYBDINPUT { key=keys[i], flags=down ? 0u : 2u } } };
        if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT))) != inputs.Length)
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }
    public static int[] Paddles(int left, int top, int width, int height, string failurePath) {
        using (var image = new Bitmap(width, height))
        using (var graphics = Graphics.FromImage(image)) {
            graphics.CopyFromScreen(left, top, 0, 0, image.Size);
            var bitmapData=image.LockBits(new Rectangle(0,0,width,height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var pixels=new byte[bitmapData.Stride*height];
            int stride=bitmapData.Stride;
            try { Marshal.Copy(bitmapData.Scan0, pixels, 0, pixels.Length); }
            finally { image.UnlockBits(bitmapData); }
            var result = new[] { -1, -1 };
            for (int side=0; side<2; side++) {
                int longest=0;
                for (int x=(int)(width*(side==0 ? .05 : .60)); x<width*(side==0 ? .40 : .95); x+=2) {
                    int start=-1;
                    for (int y=height/10; y<height; y++) {
                        int index=y*stride+x*4;
                        bool bright=pixels[index]>220 && pixels[index+1]>220 && pixels[index+2]>220;
                        if (bright && start<0) start=y;
                        if (!bright && start>=0) {
                            int length=y-start;
                            if (length>height*.09 && length<height*.4 && length>longest) {
                                result[side]=start; longest=length;
                            }
                            start=-1;
                        }
                    }
                }
                if (result[side]<0) {
                    if (!String.IsNullOrEmpty(failurePath)) image.Save(failurePath, System.Drawing.Imaging.ImageFormat.Png);
                    throw new InvalidOperationException("Cannot locate rendered paddle "+side);
                }
            }
            return result;
        }
    }
}
'@
$process = Start-Process -FilePath (Resolve-Path -LiteralPath $ExecutablePath).Path -PassThru
$window = $null
try {
    $deadline = (Get-Date).AddSeconds(20)
    do {
        $window = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst(
            [System.Windows.Automation.TreeScope]::Children,
            [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $process.Id))
        if ($window -and -not $window.Current.IsOffscreen) { break }
        Start-Sleep -Milliseconds 100
    } while ((Get-Date) -lt $deadline)
    if (-not $window) { throw 'Game window did not appear.' }
    $handle = [GameInputProbe]::WindowForProcess($process.Id)
    [GameInputProbe]::SetForegroundWindow($handle) | Out-Null
    function Find-Control([string]$Id) {
        $element = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
            [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $Id))
        if (-not $element -or $element.Current.IsOffscreen) { throw "Missing visible control: $Id" }
        return $element
    }
    function Invoke-Control([string]$Id) {
        $element = Find-Control $Id
        ([System.Windows.Automation.InvokePattern]$element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)).Invoke()
        Start-Sleep -Milliseconds 180
    }
    function Read-Paddles {
        if ([GameInputProbe]::ForegroundProcess() -ne $process.Id) {
            throw "Game lost foreground during keyboard verification: expected PID $($process.Id), actual $([GameInputProbe]::ForegroundProcess())."
        }
        $r = $window.Current.BoundingRectangle
        return ,([GameInputProbe]::Paddles([int]$r.X, [int]$r.Y, [int]$r.Width, [int]$r.Height, $FailureScreenshotPath))
    }
    function Tap([int]$Key) {
        [GameInputProbe]::Key($Key, $true)
        Start-Sleep -Milliseconds 40
        [GameInputProbe]::Key($Key, $false)
        Start-Sleep -Milliseconds 100
    }
    function Hold([int[]]$Keys, [int]$Milliseconds = 180) {
        try {
            [GameInputProbe]::Keys([uint16[]]$Keys, $true)
            Start-Sleep -Milliseconds $Milliseconds
        } finally {
            [GameInputProbe]::Keys([uint16[]]$Keys, $false)
        }
        Start-Sleep -Milliseconds 60
    }
    function Assert-Still([int[]]$Before, [int[]]$After, [string]$Scenario) {
        if ([Math]::Abs($Before[0]-$After[0]) -gt 3 -or [Math]::Abs($Before[1]-$After[1]) -gt 3) {
            throw "$Scenario moved paddles: $Before -> $After"
        }
        Write-Output "PASS: $Scenario ($Before -> $After)"
    }
    Start-Sleep -Milliseconds 800
    (Find-Control 'StartButton').SetFocus()
    $quota = (Find-Control 'QuotaStatusText').Current.Name
    Invoke-Control 'StartButton'
    $before = Read-Paddles
    Hold @(0x57, 0x26)
    $up = Read-Paddles
    if ($up[0] -ge $before[0]-15 -or $up[1] -ge $before[1]-15) { throw "W + Up failed: $before -> $up" }
    Write-Output "PASS: simultaneous W + Up moves both paddles upward ($before -> $up)"
    Start-Sleep -Milliseconds 200
    Assert-Still $up (Read-Paddles) 'released keys'
    Hold @(0x53, 0x28)
    $down = Read-Paddles
    if ($down[0] -le $up[0]+15 -or $down[1] -le $up[1]+15) { throw "S + Down failed: $up -> $down" }
    Write-Output "PASS: simultaneous S + Down moves both paddles downward ($up -> $down)"
    try {
        [GameInputProbe]::Keys([uint16[]]@(0x57, 0x53, 0x26, 0x28), $true)
        Start-Sleep -Milliseconds 60
        $before = Read-Paddles
        Start-Sleep -Milliseconds 180
        Assert-Still $before (Read-Paddles) 'opposing directions cancel while held'
    } finally {
        [GameInputProbe]::Keys([uint16[]]@(0x57, 0x53, 0x26, 0x28), $false)
    }
    (Find-Control 'FullScreenButton').SetFocus()
    Hold @(0x57, 0x26)
    $titleFocus = Read-Paddles
    if ($titleFocus[0] -ge $down[0]-15 -or $titleFocus[1] -ge $down[1]-15) { throw 'Gameplay failed while a title-bar button held keyboard focus.' }
    Write-Output 'PASS: gameplay keys still work with title-bar focus'
    Tap 0x1B
    Find-Control 'ResumeMatchButton' | Out-Null
    Invoke-Control 'ResumeMatchButton'
    $before = Read-Paddles
    Start-Sleep -Milliseconds 200
    Assert-Still $before (Read-Paddles) 'pause/resume clears held input'
    [GameInputProbe]::Key(0x57, $true)
    Invoke-Control 'InformationButton'
    [GameInputProbe]::Key(0x57, $false)
    Invoke-Control 'CloseInformationButton'
    $before = Read-Paddles
    Start-Sleep -Milliseconds 200
    Assert-Still $before (Read-Paddles) 'information overlay clears held input'
    Invoke-Control 'SettingsButton'
    [GameInputProbe]::ShowWindow($handle, 6) | Out-Null
    Start-Sleep -Milliseconds 150
    [GameInputProbe]::ShowWindow($handle, 9) | Out-Null
    [GameInputProbe]::SetForegroundWindow($handle) | Out-Null
    Start-Sleep -Milliseconds 150
    Invoke-Control 'CloseSettingsButton'
    Find-Control 'ResumeMatchButton' | Out-Null
    Write-Output 'PASS: focus loss during settings requires explicit resume'
    Invoke-Control 'InformationButton'
    Invoke-Control 'CloseInformationButton'
    Find-Control 'ResumeMatchButton' | Out-Null
    Invoke-Control 'ResumeMatchButton'
    Invoke-Control 'FullScreenButton'
    $before = Read-Paddles
    Hold @(0x53, 0x28)
    $after = Read-Paddles
    if ($after[0] -le $before[0]+15 -or $after[1] -le $before[1]+15) { throw 'Controls failed after display-mode toggle.' }
    Write-Output 'PASS: both paddles respond after window/full-screen toggle'
    Invoke-Control 'FullScreenButton'
    Tap 0x1B
    Invoke-Control 'AbandonMatchButton'
    if ((Find-Control 'QuotaStatusText').Current.Name -ne $quota) { throw 'Abandoning consumed a free play.' }
    Write-Output 'PASS: minimize pauses; abandon does not consume a play'
    Invoke-Control 'OnePlayerButton'
    $before = Read-Paddles
    Hold @(0x57)
    $after = Read-Paddles
    if ($after[0] -ge $before[0]-15) { throw 'Single-player W control failed.' }
    Write-Output "PASS: single-player W moves the human paddle ($($before[0]) -> $($after[0]))"
    $before = $after
    Hold @(0x53)
    $after = Read-Paddles
    if ($after[0] -le $before[0]+15) { throw 'Single-player S control failed.' }
    Write-Output "PASS: single-player S moves the human paddle ($($before[0]) -> $($after[0]))"
    Tap 0x1B
    Invoke-Control 'AbandonMatchButton'
    if ((Find-Control 'QuotaStatusText').Current.Name -ne $quota) { throw 'Single-player abandon consumed a free play.' }
} finally {
    foreach ($key in @(0x57, 0x53, 0x26, 0x28, 0x1B)) { [GameInputProbe]::Key($key, $false) }
    if ($window -and -not $process.HasExited) {
        ([System.Windows.Automation.WindowPattern]$window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern)).Close()
    }
    if (-not $process.WaitForExit(5000)) { Stop-Process -Id $process.Id }
}
