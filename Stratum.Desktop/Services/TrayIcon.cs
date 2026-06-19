// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;

namespace Stratum.Desktop.Services
{
    /// <summary>
    /// 基于 Win32 Shell_NotifyIcon 的系统托盘图标，纯 P/Invoke 实现，
    /// 替代 System.Windows.Forms.NotifyIcon，从而移除对 WindowsForms 程序集的依赖。
    /// 通过一个隐藏的 HwndSource 窗口接收托盘鼠标回调消息：
    ///   - 左键双击 -> DoubleClick 事件
    ///   - 右键抬起 -> 弹出 WPF ContextMenu
    /// 图标直接从当前 exe 的 ApplicationIcon 资源中提取，无需 System.Drawing。
    /// </summary>
    public sealed class TrayIcon : IDisposable
    {
        private const int WM_USER = 0x0400;
        private const int WM_TRAYICON = WM_USER + 1;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_RBUTTONUP = 0x0205;

        private const int NIM_ADD = 0x0;
        private const int NIM_DELETE = 0x2;
        private const int NIF_MESSAGE = 0x1;
        private const int NIF_ICON = 0x2;
        private const int NIF_TIP = 0x4;

        private const int WS_POPUP = unchecked((int)0x80000000);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NOTIFYICONDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uID;
            public int uFlags;
            public int uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool Shell_NotifyIcon(int message, ref NOTIFYICONDATA data);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern uint ExtractIconEx(string file, int index, IntPtr[] largeIcons, IntPtr[] smallIcons, uint count);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private HwndSource _window;
        private NOTIFYICONDATA _data;
        private IntPtr _hIcon;
        private bool _added;
        private readonly ContextMenu _contextMenu;

        /// <summary>左键双击托盘图标时触发。</summary>
        public event Action DoubleClick;

        public TrayIcon(string tooltip, ContextMenu contextMenu)
        {
            _contextMenu = contextMenu;

            // 隐藏窗口用于接收托盘回调消息（普通顶层窗口而非 message-only，
            // 以便右键菜单弹出时可通过 SetForegroundWindow 获得前台焦点）
            var parameters = new HwndSourceParameters("StratumTrayWindow", 1, 1)
            {
                WindowStyle = WS_POPUP
            };
            _window = new HwndSource(parameters);
            _window.AddHook(WndProc);

            _hIcon = LoadAppIcon();

            _data = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _window.Handle,
                uID = 1,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage = WM_TRAYICON,
                hIcon = _hIcon,
                szTip = tooltip ?? string.Empty
            };
        }

        /// <summary>托盘图标是否可见（添加到通知区域）。</summary>
        public bool Visible
        {
            get => _added;
            set
            {
                if (value && !_added)
                {
                    if (Shell_NotifyIcon(NIM_ADD, ref _data))
                    {
                        _added = true;
                    }
                }
                else if (!value && _added)
                {
                    Shell_NotifyIcon(NIM_DELETE, ref _data);
                    _added = false;
                }
            }
        }

        private static IntPtr LoadAppIcon()
        {
            try
            {
                var exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath))
                {
                    return IntPtr.Zero;
                }

                var large = new IntPtr[1];
                var small = new IntPtr[1];
                ExtractIconEx(exePath, 0, large, small, 1);

                // 优先用小图标（适合托盘 16x16），并释放未使用的大图标句柄
                if (small[0] != IntPtr.Zero)
                {
                    if (large[0] != IntPtr.Zero)
                    {
                        DestroyIcon(large[0]);
                    }
                    return small[0];
                }
                return large[0];
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg != WM_TRAYICON)
            {
                return IntPtr.Zero;
            }

            var mouseMessage = lParam.ToInt32();
            if (mouseMessage == WM_LBUTTONDBLCLK)
            {
                DoubleClick?.Invoke();
                handled = true;
            }
            else if (mouseMessage == WM_RBUTTONUP)
            {
                ShowContextMenu();
                handled = true;
            }

            return IntPtr.Zero;
        }

        private void ShowContextMenu()
        {
            if (_contextMenu == null)
            {
                return;
            }

            // 让隐藏窗口成为前台，确保点击菜单外部时菜单能正常关闭
            SetForegroundWindow(_window.Handle);
            _contextMenu.Placement = PlacementMode.MousePoint;
            _contextMenu.IsOpen = true;
        }

        public void Dispose()
        {
            Visible = false;

            if (_hIcon != IntPtr.Zero)
            {
                DestroyIcon(_hIcon);
                _hIcon = IntPtr.Zero;
            }

            _window?.Dispose();
            _window = null;
        }
    }
}
