// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using Autofac;
using Serilog;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;
using Stratum.Desktop.Services;
using Stratum.Desktop.ViewModels;

namespace Stratum.Desktop.Views
{
    public partial class CategoryAssignmentDialog : Window
    {
        private readonly ILogger _log = Log.ForContext<CategoryAssignmentDialog>();
        private readonly ICategoryRepository _categoryRepository;
        private readonly IAuthenticatorCategoryRepository _authenticatorCategoryRepository;
        private readonly AuthenticatorViewModel _authenticator;
        private readonly ObservableCollection<CategorySelectionViewModel> _categories;

        public CategoryAssignmentDialog(AuthenticatorViewModel authenticator)
        {
            InitializeComponent();
            
            _authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
            _categoryRepository = App.Container.Resolve<ICategoryRepository>();
            _authenticatorCategoryRepository = App.Container.Resolve<IAuthenticatorCategoryRepository>();
            _categories = new ObservableCollection<CategorySelectionViewModel>();

            // Set authenticator info
            AuthenticatorIssuer.Text = _authenticator.Issuer;
            AuthenticatorUsername.Text = _authenticator.Username;
            AuthenticatorUsername.Visibility = _authenticator.HasUsername ? Visibility.Visible : Visibility.Collapsed;
            AuthenticatorIcon.Text = _authenticator.IssuerInitial;

            CategoriesListBox.ItemsSource = _categories;
            
            Loaded += CategoryAssignmentDialog_Loaded;
        }

        private async void CategoryAssignmentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadCategoriesAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var categories = await _categoryRepository.GetAllAsync();
                var bindings = await _authenticatorCategoryRepository.GetAllForAuthenticatorAsync(_authenticator.Auth);
                var assignedCategoryIds = new HashSet<string>(bindings.Select(b => b.CategoryId));

                _categories.Clear();
                foreach (var category in categories.OrderBy(c => c.Ranking))
                {
                    _categories.Add(new CategorySelectionViewModel
                    {
                        Category = category,
                        IsSelected = assignedCategoryIds.Contains(category.Id)
                    });
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to load categories");
                MessageBox.Show($"{LocalizationManager.GetString("Error")}: {ex.Message}",
                    LocalizationManager.GetString("Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Remove all existing bindings
                await _authenticatorCategoryRepository.DeleteAllForAuthenticatorAsync(_authenticator.Auth);

                // Add new bindings for selected categories
                var selectedCategories = _categories.Where(c => c.IsSelected).Select(c => c.Category);
                foreach (var category in selectedCategories)
                {
                    var binding = new AuthenticatorCategory
                    {
                        AuthenticatorSecret = _authenticator.Auth.Secret,
                        CategoryId = category.Id
                    };
                    await _authenticatorCategoryRepository.CreateAsync(binding);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to save category assignments");
                MessageBox.Show($"{LocalizationManager.GetString("Error")}: {ex.Message}",
                    LocalizationManager.GetString("Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class CategorySelectionViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;

        public Category Category { get; set; }
        public string Name => Category?.Name ?? "";

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}