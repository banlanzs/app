// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Stratum.Core.Entity;
using Stratum.Desktop.ViewModels;

namespace Stratum.Desktop.Panels
{
    public partial class HomePanel : UserControl
    {
        private bool _isDragging = false;
        private Point _startPoint;
        private AuthenticatorViewModel _draggedItem;

        public HomePanel()
        {
            InitializeComponent();
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

        private void AuthenticatorCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _startPoint = e.GetPosition(null);
                if (sender is Border border && border.DataContext is AuthenticatorViewModel auth)
                {
                    _draggedItem = auth;
                }
            }
        }

        private void AuthenticatorCard_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = _startPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    _isDragging = true;
                    if (sender is Border border && _draggedItem != null)
                    {
                        DragDrop.DoDragDrop(border, _draggedItem, DragDropEffects.Move);
                        _isDragging = false;
                    }
                }
            }
        }

        private void AuthenticatorCard_Click(object sender, MouseButtonEventArgs e)
        {
            // Only handle click if we weren't dragging
            if (!_isDragging && DataContext is MainViewModel viewModel && sender is Border border)
            {
                if (border.DataContext is AuthenticatorViewModel auth)
                {
                    viewModel.CopyCodeCommand.Execute(auth);
                }
            }
            _isDragging = false;
        }

        private void AuthenticatorCard_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(AuthenticatorViewModel)))
            {
                e.Effects = DragDropEffects.Move;
                // Change background color to indicate drop target
                if (sender is Border border)
                {
                    border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(227, 242, 253)); // Light blue
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void AuthenticatorCard_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(AuthenticatorViewModel)))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void AuthenticatorCard_DragLeave(object sender, DragEventArgs e)
        {
            // Restore original background when drag leaves
            if (sender is Border border)
            {
                border.ClearValue(Border.BackgroundProperty);
            }
        }

        private void AuthenticatorCard_Drop(object sender, DragEventArgs e)
        {
            // Restore original background
            if (sender is Border border)
            {
                border.ClearValue(Border.BackgroundProperty); // This will restore the style's background
            }

            if (e.Data.GetDataPresent(typeof(AuthenticatorViewModel)) && 
                sender is Border targetBorder &&
                targetBorder.DataContext is AuthenticatorViewModel targetAuth &&
                DataContext is MainViewModel viewModel)
            {
                var draggedAuth = (AuthenticatorViewModel)e.Data.GetData(typeof(AuthenticatorViewModel));
                if (draggedAuth != null && draggedAuth != targetAuth)
                {
                    viewModel.ReorderAuthenticators(draggedAuth, targetAuth);
                }
            }
        }

        // Three-dot menu button click
        private void MoreMenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.DataContext = button.Tag; // Set the authenticator as DataContext
                button.ContextMenu.IsOpen = true;
            }
        }

        // Context menu handlers
        private void CopyCode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && 
                menuItem.Parent is ContextMenu contextMenu &&
                contextMenu.DataContext is AuthenticatorViewModel auth &&
                DataContext is MainViewModel viewModel)
            {
                viewModel.CopyCodeCommand.Execute(auth);
            }
        }

        private void EditAuthenticator_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && 
                menuItem.Parent is ContextMenu contextMenu &&
                contextMenu.DataContext is AuthenticatorViewModel auth &&
                DataContext is MainViewModel viewModel)
            {
                viewModel.EditAuthenticatorCommand.Execute(auth);
            }
        }

        private void ShowQrCode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && 
                menuItem.Parent is ContextMenu contextMenu &&
                contextMenu.DataContext is AuthenticatorViewModel auth &&
                DataContext is MainViewModel viewModel)
            {
                viewModel.ShowQrCodeCommand.Execute(auth);
            }
        }

        private void DeleteAuthenticator_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && 
                menuItem.Parent is ContextMenu contextMenu &&
                contextMenu.DataContext is AuthenticatorViewModel auth &&
                DataContext is MainViewModel viewModel)
            {
                viewModel.DeleteAuthenticatorCommand.Execute(auth);
            }
        }

        private void AssignCategory_Click(object sender, RoutedEventArgs e)
        {
            AuthenticatorViewModel auth = null;
            
            // Handle both right-click context menu and three-dot button menu
            if (sender is MenuItem menuItem)
            {
                if (menuItem.Parent is ContextMenu contextMenu)
                {
                    // From right-click context menu
                    if (contextMenu.PlacementTarget is Border border && border.DataContext is AuthenticatorViewModel rightClickAuth)
                    {
                        auth = rightClickAuth;
                    }
                    // From three-dot button menu
                    else if (contextMenu.DataContext is AuthenticatorViewModel threeDotAuth)
                    {
                        auth = threeDotAuth;
                    }
                }
            }

            if (auth != null && DataContext is MainViewModel viewModel)
            {
                // Open category assignment dialog
                var categoryDialog = new Views.CategoryAssignmentDialog(auth)
                {
                    Owner = Application.Current.MainWindow
                };
                
                if (categoryDialog.ShowDialog() == true)
                {
                    // Category assignment is handled within the dialog
                    // Refresh the view if needed
                    viewModel.SortAuthenticators("Custom");
                }
            }
        }

        // Sort menu handlers
        private void SortMenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }

        private void SortByNameAsc_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.SortAuthenticators("NameAsc");
            }
        }

        private void SortByNameDesc_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.SortAuthenticators("NameDesc");
            }
        }

        private void SortByCopyCountAsc_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.SortAuthenticators("CopyCountAsc");
            }
        }

        private void SortByCopyCountDesc_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.SortAuthenticators("CopyCountDesc");
            }
        }

        private void SortCustom_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.SortAuthenticators("Custom");
            }
        }

        // Category filter handlers
        private void CategoryFilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && DataContext is MainViewModel viewModel)
            {
                // Clear existing menu items
                CategoryContextMenu.Items.Clear();

                // Add "All" option
                var allMenuItem = new MenuItem
                {
                    Header = "All",
                    Tag = null
                };
                allMenuItem.Click += CategoryMenuItem_Click;
                if (viewModel.SelectedCategory == null)
                {
                    allMenuItem.FontWeight = FontWeights.Bold;
                }
                CategoryContextMenu.Items.Add(allMenuItem);

                // Add separator if there are categories
                if (viewModel.Categories.Count > 1) // Count > 1 because "All" is always included
                {
                    CategoryContextMenu.Items.Add(new Separator());
                }

                // Add category options
                foreach (var category in viewModel.Categories)
                {
                    if (category.Id != null) // Skip the "All" category that's already added
                    {
                        var menuItem = new MenuItem
                        {
                            Header = category.Name,
                            Tag = category
                        };
                        menuItem.Click += CategoryMenuItem_Click;
                        
                        // Highlight selected category
                        if (viewModel.SelectedCategory?.Id == category.Id)
                        {
                            menuItem.FontWeight = FontWeights.Bold;
                        }
                        
                        CategoryContextMenu.Items.Add(menuItem);
                    }
                }

                // Show the context menu
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }

        private void CategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && DataContext is MainViewModel viewModel)
            {
                var category = menuItem.Tag as Category;
                viewModel.SelectedCategory = category;
            }
        }
    }
}
