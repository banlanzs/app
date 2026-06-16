// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Serilog;
using Stratum.Core.Entity;
using Stratum.Desktop.Behaviors;
using Stratum.Desktop.ViewModels;

namespace Stratum.Desktop.Panels
{
    public partial class HomePanel : UserControl
    {
        private static readonly ILogger _log = Log.ForContext<HomePanel>();
        private MainViewModel _currentViewModel;

        public HomePanel()
        {
            InitializeComponent();
            DataContextChanged += HomePanel_DataContextChanged;
        }

        private void HomePanel_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _log.Information("HomePanel_DataContextChanged: Old = {Old}, New = {New}",
                e.OldValue?.GetType().Name, e.NewValue?.GetType().Name);

            // Detach from old ViewModel
            if (_currentViewModel != null)
            {
                _currentViewModel.PropertyChanged -= ViewModel_PropertyChanged;
                _log.Information("Detached PropertyChanged listener from old ViewModel");
            }

            // Attach to new ViewModel
            if (e.NewValue is MainViewModel newViewModel)
            {
                _currentViewModel = newViewModel;
                _currentViewModel.PropertyChanged += ViewModel_PropertyChanged;
                _log.Information("Attached PropertyChanged listener to new ViewModel");
            }
            else
            {
                _currentViewModel = null;
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            _log.Debug("ViewModel_PropertyChanged: PropertyName = {PropertyName}", e.PropertyName);
        }

        public void FocusSearchBox()
        {
            SearchTextBox?.Focus();
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.ClearSearchCommand.Execute(null);
            }
            SearchTextBox?.Focus();
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && DataContext is MainViewModel viewModel)
            {
                viewModel.ClearSearchCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void GridViewButton_Click(object sender, RoutedEventArgs e)
        {
            ListViewButton.IsChecked = false;
            GridItemsControl.Visibility = Visibility.Visible;
            ListItemsControl.Visibility = Visibility.Collapsed;
        }

        private void ListViewButton_Click(object sender, RoutedEventArgs e)
        {
            GridViewButton.IsChecked = false;
            GridItemsControl.Visibility = Visibility.Collapsed;
            ListItemsControl.Visibility = Visibility.Visible;
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            // Theme toggle logic will be implemented later
            _log.Information("Theme toggle clicked");
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.AddAuthenticatorCommand.Execute(null);
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is AuthenticatorViewModel auth &&
                DataContext is MainViewModel viewModel)
            {
                viewModel.CopyCodeCommand.Execute(auth);
                ShowCopyFeedback(button);
            }
        }

        private async void ShowCopyFeedback(Button button)
        {
            var originalBackground = button.Background;
            var originalBorderBrush = button.BorderBrush;

            button.Background = (System.Windows.Media.Brush)FindResource("SuccessBrush");
            button.BorderBrush = (System.Windows.Media.Brush)FindResource("SuccessBrush");
            button.Foreground = System.Windows.Media.Brushes.White;

            await System.Threading.Tasks.Task.Delay(2000);

            button.Background = originalBackground;
            button.BorderBrush = originalBorderBrush;
            button.Foreground = (System.Windows.Media.Brush)FindResource("MutedBrush");
        }

        private void AssignCategory_Click(object sender, RoutedEventArgs e)
        {
            AuthenticatorViewModel auth = null;

            if (sender is MenuItem menuItem)
            {
                if (menuItem.Parent is ContextMenu contextMenu)
                {
                    // From context menu: DataContext is the MainViewModel
                    if (contextMenu.DataContext is MainViewModel &&
                        contextMenu.PlacementTarget is FrameworkElement element &&
                        element.DataContext is AuthenticatorViewModel targetAuth)
                    {
                        auth = targetAuth;
                    }
                    // Fallback: Tag-based binding
                    else if (contextMenu.DataContext is AuthenticatorViewModel threeDotAuth)
                    {
                        auth = threeDotAuth;
                    }
                }
            }

            if (auth != null && DataContext is MainViewModel viewModel)
            {
                var categoryDialog = new Views.CategoryAssignmentDialog(auth)
                {
                    Owner = Application.Current.MainWindow
                };

                if (categoryDialog.ShowDialog() == true)
                {
                    viewModel.SortAuthenticators("Custom");
                }
            }
        }

        private void ListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Bubble scroll event to parent ScrollViewer
            if (sender is ListBox listBox)
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(listBox);
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 3.0);
                    e.Handled = true;
                }
            }
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    return result;
                }
                var childResult = FindVisualChild<T>(child);
                if (childResult != null)
                {
                    return childResult;
                }
            }
            return null;
        }
    }
}
