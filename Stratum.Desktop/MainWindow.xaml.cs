// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Autofac;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;
using Stratum.Core.Entity;
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
        private StackPanel _categoryItemsPanel;
        private Button _allCategoryButton;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            StateChanged += MainWindow_StateChanged;
            Closing += MainWindow_Closing;

            // Setup sidebar categories
            SetupSidebar();
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
            LoadCategoryButtons();
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

        private void SetupSidebar()
        {
            // Add "All" category button
            _allCategoryButton = new Button
            {
                Style = (Style)FindResource("SidebarItemStyle"),
                Tag = "Active",
                Content = CreateSidebarContent("全部认证器", "M3 3h7v7H3zM14 3h7v7h-7zM3 14h7v7H3zM14 14h7v7h-7z")
            };
            _allCategoryButton.Click += (s, e) => {
                NavigateToHome();
                _viewModel?.SelectCategoryCommand.Execute(null);
                UpdateSidebarActiveState(_allCategoryButton);
            };
            SidebarPanel.Children.Add(_allCategoryButton);

            // Add separator
            SidebarPanel.Children.Add(new TextBlock
            {
                Text = "分类",
                FontSize = 12,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                Foreground = (System.Windows.Media.Brush)FindResource("MutedBrush"),
                Margin = new Thickness(12, 16, 12, 8)
            });

            // Container for dynamic category buttons
            _categoryItemsPanel = new StackPanel();
            SidebarPanel.Children.Add(_categoryItemsPanel);

            // Add Categories button
            var categoriesButton = new Button
            {
                Style = (Style)FindResource("SidebarItemStyle"),
                Content = CreateSidebarContent("管理分类", "M3 7h18M3 12h18M3 17h18")
            };
            categoriesButton.Click += (s, e) => {
                NavigateToCategories();
                UpdateSidebarActiveState(categoriesButton);
            };
            SidebarPanel.Children.Add(categoriesButton);

            // Add separator
            SidebarPanel.Children.Add(new TextBlock
            {
                Text = "数据",
                FontSize = 12,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                Foreground = (System.Windows.Media.Brush)FindResource("MutedBrush"),
                Margin = new Thickness(12, 16, 12, 8)
            });

            // Add Backup button
            var backupButton = new Button
            {
                Style = (Style)FindResource("SidebarItemStyle"),
                Content = CreateSidebarContent("备份与恢复", "M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z")
            };
            backupButton.Click += (s, e) => {
                NavigateToBackup();
                UpdateSidebarActiveState(backupButton);
            };
            SidebarPanel.Children.Add(backupButton);

            // Add Import button
            var importButton = new Button
            {
                Style = (Style)FindResource("SidebarItemStyle"),
                Content = CreateSidebarContent("导入数据", "M12 2v10M16 8l-4-4-4 4M3 16v4a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-4")
            };
            importButton.Click += (s, e) => {
                NavigateToImport();
            };
            SidebarPanel.Children.Add(importButton);

            // Add separator
            SidebarPanel.Children.Add(new TextBlock
            {
                Text = "关于",
                FontSize = 12,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                Foreground = (System.Windows.Media.Brush)FindResource("MutedBrush"),
                Margin = new Thickness(12, 16, 12, 8)
            });

            // Add About button
            var aboutButton = new Button
            {
                Style = (Style)FindResource("SidebarItemStyle"),
                Content = CreateSidebarContent("关于 Stratum", "M12 22a10 10 0 1 1 0-20 10 10 0 0 1 0 20zM12 8v4M12 16h.01")
            };
            aboutButton.Click += (s, e) => {
                NavigateToAbout();
                UpdateSidebarActiveState(aboutButton);
            };
            SidebarPanel.Children.Add(aboutButton);
        }

        private void LoadCategoryButtons()
        {
            if (_categoryItemsPanel == null || _viewModel == null) return;

            _categoryItemsPanel.Children.Clear();

            foreach (var category in _viewModel.Categories)
            {
                if (string.IsNullOrEmpty(category.Id)) continue;

                var iconPath = "M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z";
                var catButton = new Button
                {
                    Style = (Style)FindResource("SidebarItemStyle"),
                    Content = CreateSidebarContent(category.Name, iconPath)
                };
                catButton.Click += (s, e) => {
                    NavigateToHome();
                    _viewModel.SelectCategoryCommand.Execute(category);
                    UpdateSidebarActiveState(catButton);
                };
                _categoryItemsPanel.Children.Add(catButton);
            }
        }

        private void UpdateSidebarActiveState(Button activeButton)
        {
            foreach (var child in SidebarPanel.Children)
            {
                if (child is Button btn)
                {
                    btn.Tag = btn == activeButton ? "Active" : null;
                }
            }
        }

        private StackPanel CreateSidebarContent(string label, string iconPath)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };

            var path = new System.Windows.Shapes.Path
            {
                Data = System.Windows.Media.Geometry.Parse(iconPath),
                Stroke = System.Windows.Media.Brushes.Transparent,
                Fill = System.Windows.Media.Brushes.Transparent,
                StrokeThickness = 1.8,
                Width = 16,
                Height = 16,
                Stretch = System.Windows.Media.Stretch.Uniform,
                Margin = new Thickness(0, 0, 12, 0)
            };
            path.SetBinding(System.Windows.Shapes.Path.StrokeProperty, new System.Windows.Data.Binding("Foreground")
            {
                RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.FindAncestor, typeof(Button), 1)
            });

            panel.Children.Add(path);
            panel.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });

            return panel;
        }

        private void SelectCategory(Category category)
        {
            // Update active state
            foreach (var child in SidebarPanel.Children)
            {
                if (child is Button btn)
                {
                    btn.Tag = null;
                }
            }

            _viewModel?.SelectCategoryCommand.Execute(category);
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

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeButton_Click(sender, e);
            }
            else
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToSettings();
        }

        private void NavigateToHome()
        {
            if (_homePanel == null)
            {
                _homePanel = new HomePanel { DataContext = _viewModel };
            }
            ContentPanel.Content = _homePanel;
        }

        private void NavigateToSettings()
        {
            if (_settingsPanel == null)
            {
                _settingsPanel = new SettingsPanel();
            }
            ContentPanel.Content = _settingsPanel;
        }

        private void NavigateToCategories()
        {
            if (_categoriesPanel == null)
            {
                _categoriesPanel = new CategoriesPanel();
            }
            ContentPanel.Content = _categoriesPanel;
        }

        private void NavigateToImport()
        {
            var importDialog = new Views.ImportDialog
            {
                Owner = this
            };
            importDialog.ShowDialog();
        }

        private void NavigateToBackup()
        {
            if (_backupPanel == null)
            {
                _backupPanel = new BackupPanel();
            }
            ContentPanel.Content = _backupPanel;
        }

        private void NavigateToAbout()
        {
            if (_aboutPanel == null)
            {
                _aboutPanel = new AboutPanel();
            }
            ContentPanel.Content = _aboutPanel;
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
