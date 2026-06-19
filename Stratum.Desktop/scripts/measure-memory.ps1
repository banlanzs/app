# Copyright (C) 2024 Stratum Contributors
# SPDX-License-Identifier: GPL-3.0-only
#
# Stratum.Desktop 内存测量脚本（Working Set 口径，对应任务管理器"内存"列）
# 用法: powershell -NoProfile -ExecutionPolicy Bypass -File measure-memory.ps1 [-SettleSeconds 10]
#
# 测量三种场景的 WorkingSet：
#   1. 静默驻留态 (--autostart --silent)
#   2. 显示态前台峰值
#   3. 最小化到托盘（触发 WorkingSetTrimmer 裁剪）后 / 恢复显示后
#
# 单实例机制说明：每个场景测量前先杀光所有 Stratum 进程并归零，
# 避免后启动的实例被单实例转交而退出，导致测到 stub 进程的假数据。

param(
    [int]$SettleSeconds = 10,
    [string]$ExePath = ""
)

$ErrorActionPreference = "SilentlyContinue"
if ($ExePath) {
    $exe = (Resolve-Path $ExePath).Path
} else {
    $exe = (Resolve-Path (Join-Path $PSScriptRoot "..\bin\Release\net9.0-windows\win-x64\Stratum.exe")).Path
}

# 单引号 here-string：C# 源码中的双引号原样保留，无需转义
Add-Type @'
using System;
using System.Runtime.InteropServices;
public class NativeWin {
    [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr h, int nCmdShow);
}
'@
$SW_MINIMIZE = 6
$SW_RESTORE  = 9

function KillAll { Get-Process Stratum -ErrorAction SilentlyContinue | Stop-Process -Force; Start-Sleep 3 }
function WSmb($p){ $p.Refresh(); return [math]::Round($p.WorkingSet64/1MB,1) }

Write-Output "===== Stratum 内存测量 (WorkingSet) ====="
Write-Output ("exe: {0}" -f $exe)

# --- 场景1: 静默驻留态 ---
KillAll
Start-Process $exe -ArgumentList "--autostart","--silent" | Out-Null
Start-Sleep $SettleSeconds
$p = @(Get-Process Stratum)[0]
if($p){ Write-Output ("[1 Silent-Tray]   WS={0,7}MB  (PID={1}, alive={2})" -f (WSmb $p), $p.Id, (-not $p.HasExited)) }
else  { Write-Output "[1 Silent-Tray]   NO PROCESS (exited)" }

# --- 场景2: 显示态前台 -> 最小化裁剪 -> 恢复 ---
KillAll
Start-Process $exe | Out-Null
Start-Sleep $SettleSeconds
$p = @(Get-Process Stratum)[0]
if(-not $p){ Write-Output "[2 Visible]       NO PROCESS"; KillAll; exit }
$cnt = @(Get-Process Stratum).Count
Write-Output ("[2 Visible-FG]    WS={0,7}MB  (instances={1})" -f (WSmb $p), $cnt)

[NativeWin]::ShowWindowAsync($p.MainWindowHandle, $SW_MINIMIZE) | Out-Null
Start-Sleep 7
Write-Output ("[2 Min->Tray]     WS={0,7}MB  (trimmed)" -f (WSmb $p))

[NativeWin]::ShowWindowAsync($p.MainWindowHandle, $SW_RESTORE) | Out-Null
Start-Sleep 5
Write-Output ("[2 Restored-FG]   WS={0,7}MB  (page-in)" -f (WSmb $p))

KillAll
Write-Output "===== DONE ====="
