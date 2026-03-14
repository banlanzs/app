// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Autofac;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;
using Stratum.Desktop.Panels;
using Stratum.Desktop.Services;
using Stratum.Desktop.ViewModels;

namespace Stratum.Desktop
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private PreferenceManager _preferenceManager;
        private HomePanel _homePanel;
        private SettingsPanel _settingsPanel;
        private CategoriesPanel _categoriesPanel;
        private BackupPanel _backupPanel;
        private AboutPanel _aboutPanel;
        private Forms.NotifyIcon _trayIcon;
        private bool _isExitRequested;
        private bool _isUiInitialized;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            StateChanged += MainWindow_StateChanged;
            Closing += MainWindow_Closing;
        }

        public async Task InitializeViewModelAsync()
        {
            await InitializeCoreAsync();

            // Only initialize UI if not in silent autostart mode
            if (!App.IsSilentAutostart)
            {
                await InitializeUIAsync();
            }
        }

        private Task InitializeCoreAsync()
        {
            _preferenceManager = App.Container.Resolve<PreferenceManager>();
            InitializeTrayIcon();

            if (App.IsSilentAutostart || _preferenceManager.Preferences.StartMinimized)
            {
                HideToTray();
            }

            return Task.CompletedTask;
        }

        private async Task InitializeUIAsync()
        {
            if (_isUiInitialized) return;

            _viewModel = App.Container.Resolve<MainViewModel>();
            DataContext = _viewModel;

            await _viewModel.InitializeAsync();

            ApplyInitialSorting();
            NavigateToHome();

            _isUiInitialized = true;

            // Ensure UI is fully ready after initialization
            await _viewModel.OnUIVisibleAsync();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void ApplyInitialSorting()
        {
            try
            {
                var sortMode = _preferenceManager.Preferences.SortMode;
                var sortType = sortMode switch
                {
                    SortMode.AlphabeticalAscending => "NameAsc",
                    SortMode.AlphabeticalDescending => "NameDesc",
                    SortMode.CopyCountDescending => "CopyCountDesc",
                    SortMode.Custom => "Custom",
                    _ => "Custom"
                };
                
                _viewModel.SortAuthenticators(sortType);
            }
            catch (Exception ex)
            {
                // Log error but don't crash the app
                System.Diagnostics.Debug.WriteLine($"Failed to apply initial sorting: {ex.Message}");
            }
        }

        private void NavigationRail_NavigationChanged(object sender, string tag)
        {
            switch (tag)
            {
                case "Home":
                    NavigateToHome();
                    break;
                case "Settings":
                    NavigateToSettings();
                    break;
                case "Categories":
                    NavigateToCategories();
                    break;
                case "List":
                    // Navigate to home/list view
                    NavigateToHome();
                    break;
                case "Add":
                    // Handle add authenticator
                    _viewModel.AddAuthenticatorCommand.Execute(null);
                    break;
                case "About":
                    NavigateToAbout();
                    break;
                case "Import":
                    NavigateToImport();
                    break;
                case "Backup":
                    NavigateToBackup();
                    break;
            }
        }

        private void NavigateToHome()
        {
            if (_homePanel == null)
            {
                _homePanel = new HomePanel { DataContext = _viewModel };
            }
            ContentFrame.Navigate(_homePanel);
        }

        private void NavigateToSettings()
        {
            if (_settingsPanel == null)
            {
                _settingsPanel = new SettingsPanel();
            }
            ContentFrame.Navigate(_settingsPanel);
        }

        private void NavigateToCategories()
        {
            if (_categoriesPanel == null)
            {
                _categoriesPanel = new CategoriesPanel();
            }
            ContentFrame.Navigate(_categoriesPanel);
        }

        private void NavigateToImport()
        {
            var importDialog = new Views.ImportDialog
            {
                Owner = this
            };
            importDialog.ShowDialog();

            NavigationRail.SelectItem("Home");
        }

        private void NavigateToBackup()
        {
            if (_backupPanel == null)
            {
                _backupPanel = new BackupPanel();
            }
            ContentFrame.Navigate(_backupPanel);
        }

        private void NavigateToAbout()
        {
            if (_aboutPanel == null)
            {
                _aboutPanel = new AboutPanel();
            }
            ContentFrame.Navigate(_aboutPanel);
        }

        public void FocusSearchBox()
        {
            _homePanel?.FocusSearchBox();
        }

        protected override void OnClosed(EventArgs e)
        {
            _trayIcon?.Dispose();
            _viewModel?.Dispose();
            base.OnClosed(e);
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized && _preferenceManager.Preferences.MinimizeToTray)
            {
                HideToTray();
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (_isExitRequested || !_preferenceManager.Preferences.MinimizeToTray)
            {
                return;
            }

            e.Cancel = true;
            HideToTray();
        }

        private void InitializeTrayIcon()
        {
            if (_trayIcon != null)
            {
                return;
            }

            Drawing.Icon icon;
            try
            {
                var resourceInfo = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/AppIcon.ico"));
                if (resourceInfo != null)
                {
                    using var iconStream = resourceInfo.Stream;
                    icon = new Drawing.Icon(iconStream);
                }
                else
                {
                    icon = Drawing.SystemIcons.Application;
                }
            }
            catch
            {
                icon = Drawing.SystemIcons.Application;
            }

            var contextMenu = new Forms.ContextMenuStrip();
            contextMenu.Items.Add("Open", null, (_, __) => ShowFromTray());
            contextMenu.Items.Add("Exit", null, (_, __) => ExitFromTray());

            _trayIcon = new Forms.NotifyIcon
            {
                Text = "Stratum",
                Icon = icon,
                Visible = false,
                ContextMenuStrip = contextMenu
            };

            _trayIcon.DoubleClick += (_, __) => ShowFromTray();
        }

        private async void ShowFromTray()
        {
            // Initialize UI on first show if not already initialized
            if (!_isUiInitialized)
            {
                await InitializeUIAsync();
            }
            else
            {
                // Notify ViewModel that UI is now visible (only if already initialized)
                if (_viewModel != null)
                {
                    await _viewModel.OnUIVisibleAsync();
                }
            }

            Show();
            WindowState = WindowState.Normal;
            Activate();
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
            }
        }

        private void HideToTray()
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = true;
            }

            Hide();
        }

        private void ExitFromTray()
        {
            _isExitRequested = true;
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
            }

            Close();
        }
    }
}
