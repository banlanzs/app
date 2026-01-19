// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Windows;
using System.Windows.Controls;

namespace Stratum.Desktop.Controls
{
    public partial class NavigationRail : UserControl
    {
        public event EventHandler<string> NavigationChanged;

        public NavigationRail()
        {
            InitializeComponent();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationChanged?.Invoke(this, "Settings");
        }

        private void ListButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to home/list view
            NavigationChanged?.Invoke(this, "List");
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Trigger add authenticator
            NavigationChanged?.Invoke(this, "Add");
        }

        private void CategoriesButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationChanged?.Invoke(this, "Categories");
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationChanged?.Invoke(this, "About");
        }

        public void SelectItem(string tag)
        {
            // For the new bottom navigation, we don't need to manage checked states
            // since we're using regular buttons instead of radio buttons
        }
    }
}
