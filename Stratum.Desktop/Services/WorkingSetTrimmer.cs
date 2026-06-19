// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Stratum.Desktop.Services
{
    /// <summary>
    /// 主动裁剪当前进程的工作集（Working Set）。
    /// 先尽力回收托管堆，再请求操作系统将不活跃的物理页面换出，
    /// 从而显著降低任务管理器显示的内存占用。
    /// 仅应在窗口最小化 / 隐藏到托盘 / 失焦等非活跃场景调用，
    /// 避免在前台活跃时裁剪导致页面回读（page-in）延迟影响手感。
    /// </summary>
    public static class WorkingSetTrimmer
    {
        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr process, IntPtr minimumWorkingSetSize, IntPtr maximumWorkingSetSize);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        // 传入 (IntPtr)(-1) 表示"尽可能裁剪"，等价于 EmptyWorkingSet
        private static readonly IntPtr TrimToMinimum = new IntPtr(-1);

        /// <summary>
        /// 强制回收托管内存并把工作集交还给操作系统。
        /// 裁剪是尽力而为，任何失败都被吞掉，不影响功能。
        /// </summary>
        public static void Trim()
        {
            try
            {
                // 先压缩大对象堆并做一次激进的阻塞式压缩回收，让随后的工作集裁剪释放更多页面
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);

                SetProcessWorkingSetSize(GetCurrentProcess(), TrimToMinimum, TrimToMinimum);
            }
            catch
            {
                // 裁剪失败不应影响应用运行
            }
        }
    }
}
