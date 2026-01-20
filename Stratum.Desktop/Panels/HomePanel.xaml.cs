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
