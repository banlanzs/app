// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Win32;
using Serilog;

namespace Stratum.Desktop.Services
{
    public class WindowsStartupService : IStartupService
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RegistryValueName = "Stratum";
        private const string AutostartArg = "--autostart";
        private const string SilentArg = "--silent";

        private readonly ILogger _log = Log.ForContext<WindowsStartupService>();

        public Task<bool> IsEnabledAsync()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
                var value = key?.GetValue(RegistryValueName) as string;
                return Task.FromResult(!string.IsNullOrWhiteSpace(value));
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to read startup registry entry");
                return Task.FromResult(false);
            }
        }

        public Task<bool> IsSilentStartEnabledAsync()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
                var value = key?.GetValue(RegistryValueName) as string;
                return Task.FromResult(!string.IsNullOrWhiteSpace(value) && value.Contains(SilentArg));
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to read startup registry entry for silent mode");
                return Task.FromResult(false);
            }
        }

        public Task SetEnabledAsync(bool enabled, bool silentStart = false)
        {
            try
            {
                if (enabled)
                {
                    using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true);
                    var command = BuildCommand(silentStart);
                    key?.SetValue(RegistryValueName, command, RegistryValueKind.String);
                    _log.Information("Enabled Windows autostart: {Command}", command);
                }
                else
                {
                    using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                    if (key?.GetValue(RegistryValueName) != null)
                    {
                        key.DeleteValue(RegistryValueName, false);
                        _log.Information("Disabled Windows autostart");
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to {Action} startup registry entry", enabled ? "enable" : "disable");
            }

            return Task.CompletedTask;
        }

        private static string BuildCommand(bool silent)
        {
            var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
            if (string.IsNullOrEmpty(exePath))
            {
                throw new InvalidOperationException("Unable to determine application executable path");
            }

            var args = silent ? $"{AutostartArg} {SilentArg}" : AutostartArg;
            return $"\"{exePath}\" {args}";
        }
    }
}