// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System.Threading.Tasks;

namespace Stratum.Desktop.Services
{
    public interface IStartupService
    {
        Task<bool> IsEnabledAsync();
        Task SetEnabledAsync(bool enabled, bool silentStart = false);
        Task<bool> IsSilentStartEnabledAsync();
    }
}