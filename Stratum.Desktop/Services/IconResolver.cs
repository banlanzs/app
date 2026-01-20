// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Serilog;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;

namespace Stratum.Desktop.Services
{
    public class IconResolver
    {
        private readonly ILogger _log = Log.ForContext<IconResolver>();
        private readonly ICustomIconRepository _customIconRepository;
        private readonly Dictionary<string, ImageSource> _builtInCache = new();
        private readonly Dictionary<string, ImageSource> _customCache = new();

        public IconResolver(ICustomIconRepository customIconRepository)
        {
            _customIconRepository = customIconRepository;
        }

        public ImageSource GetIcon(Authenticator authenticator)
        {
            if (authenticator == null)
            {
                return null;
            }

            var iconKey = authenticator.Icon;
            if (!string.IsNullOrEmpty(iconKey))
            {
                if (iconKey.StartsWith(CustomIcon.Prefix))
                {
                    var customId = iconKey[1..];
                    if (_customCache.TryGetValue(customId, out var cachedCustom))
                    {
                        return cachedCustom;
                    }

                    return null;
                }
                else if (_builtInCache.TryGetValue(iconKey, out var cachedBuiltIn))
                {
                    return cachedBuiltIn;
                }
            }

            var fallbackIcon = string.IsNullOrEmpty(iconKey) ? "default" : iconKey;
            var iconData = TryGetBuiltInIconBytes(fallbackIcon) ?? TryGetBuiltInIconBytes("default");
            if (iconData == null)
            {
                return null;
            }

            var decoded = TryDecodeImage(iconData);
            if (decoded != null)
            {
                _builtInCache[fallbackIcon] = decoded;
            }

            return decoded;
        }

        public async Task<ImageSource> GetIconAsync(Authenticator authenticator)
        {
            if (authenticator == null)
            {
                return null;
            }

            var iconKey = authenticator.Icon;
            if (!string.IsNullOrEmpty(iconKey))
            {
                if (iconKey.StartsWith(CustomIcon.Prefix))
                {
                    var customId = iconKey[1..];
                    if (_customCache.TryGetValue(customId, out var cachedCustom))
                    {
                        return cachedCustom;
                    }

                    var customIcon = await _customIconRepository.GetAsync(customId);
                    if (customIcon?.Data != null)
                    {
                        var image = TryDecodeImage(customIcon.Data);
                        if (image != null)
                        {
                            _customCache[customId] = image;
                            return image;
                        }
                    }
                }
                else if (_builtInCache.TryGetValue(iconKey, out var cachedBuiltIn))
                {
                    return cachedBuiltIn;
                }
            }

            var fallbackIcon = string.IsNullOrEmpty(iconKey) ? "default" : iconKey;
            var iconData = TryGetBuiltInIconBytes(fallbackIcon) ?? TryGetBuiltInIconBytes("default");
            if (iconData == null)
            {
                return null;
            }

            var decoded = TryDecodeImage(iconData);
            if (decoded != null)
            {
                _builtInCache[fallbackIcon] = decoded;
            }

            return decoded;
        }

        public async Task WarmUpAsync(IEnumerable<string> iconKeys)
        {
            if (iconKeys == null)
            {
                return;
            }

            var customIds = new HashSet<string>();
            foreach (var iconKey in iconKeys)
            {
                if (string.IsNullOrEmpty(iconKey) || !iconKey.StartsWith(CustomIcon.Prefix))
                {
                    continue;
                }

                var customId = iconKey[1..];
                if (!_customCache.ContainsKey(customId))
                {
                    customIds.Add(customId);
                }
            }

            foreach (var customId in customIds)
            {
                var customIcon = await _customIconRepository.GetAsync(customId);
                if (customIcon?.Data == null)
                {
                    continue;
                }

                var image = TryDecodeImage(customIcon.Data);
                if (image != null)
                {
                    _customCache[customId] = image;
                }
            }
        }

        private byte[] TryGetBuiltInIconBytes(string iconName)
        {
            try
            {
                var uri = new Uri($"pack://application:,,,/Assets/Icons/{iconName}.png", UriKind.Absolute);
                var info = System.Windows.Application.GetResourceStream(uri);
                if (info == null)
                {
                    return null;
                }

                using var ms = new MemoryStream();
                info.Stream.CopyTo(ms);
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }

        public void ClearCache()
        {
            _builtInCache.Clear();
            _customCache.Clear();
        }

        private ImageSource TryDecodeImage(byte[] data)
        {
            try
            {
                var image = new BitmapImage();
                using (var ms = new MemoryStream(data))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.DecodePixelWidth = 32;
                    image.StreamSource = ms;
                    image.EndInit();
                    image.Freeze();
                }

                return image;
            }
            catch (Exception ex)
            {
                _log.Debug("Failed to decode icon: {Error}", ex.Message);
                return null;
            }
        }
    }
}
