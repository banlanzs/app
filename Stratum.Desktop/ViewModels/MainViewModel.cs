// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using Autofac;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;
using Stratum.Desktop.Services;

namespace Stratum.Desktop.ViewModels
{
    public sealed class ReorderRequest
    {
        public AuthenticatorViewModel Dragged { get; init; }
        public AuthenticatorViewModel Target { get; init; }
    }

    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ILogger _log = Log.ForContext<MainViewModel>();
        private readonly IAuthenticatorRepository _authenticatorRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IAuthenticatorCategoryRepository _authenticatorCategoryRepository;
        private readonly PreferenceManager _preferenceManager;
        private readonly System.Timers.Timer _updateTimer;
        private int _updateInProgress;

        private string _searchText = "";
        private Category _selectedCategory;
        private ObservableCollection<AuthenticatorViewModel> _authenticators;
        private ObservableCollection<AuthenticatorViewModel> _filteredAuthenticators;
        private ObservableCollection<Category> _categories;
        private AuthenticatorViewModel _selectedAuthenticator;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel(
            IAuthenticatorRepository authenticatorRepository,
            ICategoryRepository categoryRepository,
            IAuthenticatorCategoryRepository authenticatorCategoryRepository,
            PreferenceManager preferenceManager)
        {
            _authenticatorRepository = authenticatorRepository;
            _categoryRepository = categoryRepository;
            _authenticatorCategoryRepository = authenticatorCategoryRepository;
            _preferenceManager = preferenceManager;

            _authenticators = new ObservableCollection<AuthenticatorViewModel>();
            _filteredAuthenticators = new ObservableCollection<AuthenticatorViewModel>();
            _categories = new ObservableCollection<Category>();

            _updateTimer = new System.Timers.Timer(1000);
            _updateTimer.Elapsed += UpdateTimer_Elapsed;
            _updateTimer.AutoReset = true;

            InitializeCommands();
        }

        private void InitializeCommands()
        {
            AddAuthenticatorCommand = new RelayCommand(AddAuthenticator);
            EditAuthenticatorCommand = new RelayCommand<AuthenticatorViewModel>(EditAuthenticator);
            DeleteAuthenticatorCommand = new RelayCommand<AuthenticatorViewModel>(DeleteAuthenticator);
            CopyCodeCommand = new RelayCommand<AuthenticatorViewModel>(CopyCode);
            ShowQrCodeCommand = new RelayCommand<AuthenticatorViewModel>(ShowQrCode);
            IncrementCounterCommand = new RelayCommand<AuthenticatorViewModel>(IncrementCounter);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            FocusSearchCommand = new RelayCommand(FocusSearch);
            ClearSearchCommand = new RelayCommand(ClearSearch);
            SelectCategoryCommand = new RelayCommand<Category>(OnSelectCategory);
            ReorderAuthenticatorsCommand = new RelayCommand<object>(req =>
            {
                if (req == null) return;

                // Handle both click (single item) and drag-drop (request object)
                var reqType = req.GetType();
                if (reqType.GetProperty("Dragged") != null && reqType.GetProperty("Target") != null)
                {
                    var dragged = reqType.GetProperty("Dragged").GetValue(req) as AuthenticatorViewModel;
                    var target = reqType.GetProperty("Target").GetValue(req) as AuthenticatorViewModel;
                    if (dragged != null && target != null && !ReferenceEquals(dragged, target))
                    {
                        ReorderAuthenticators(dragged, target);
                    }
                }
                else if (req is AuthenticatorViewModel auth)
                {
                    // Single click - copy code
                    CopyCode(auth);
                }
            });
        }

        public async Task InitializeAsync()
        {
            try
            {
                await LoadAuthenticatorsAsync();
                await LoadCategoriesAsync();
                _updateTimer.Start();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to initialize MainViewModel");
                MessageBox.Show(string.Format(LocalizationManager.GetString("LoadFailed"), ex.Message), LocalizationManager.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadAuthenticatorsAsync()
        {
            var auths = await _authenticatorRepository.GetAllAsync();
            _authenticators.Clear();

            foreach (var auth in auths.OrderBy(a => a.Ranking))
            {
                _authenticators.Add(new AuthenticatorViewModel(auth));
            }

            await WarmUpIconsAsync();

            ApplyFilter();
            OnPropertyChanged(nameof(AuthenticatorCount));
            OnPropertyChanged(nameof(IsEmpty));
        }

        private async Task WarmUpIconsAsync()
        {
            var iconKeys = _authenticators.Select(auth => auth.Icon)
                .Where(icon => !string.IsNullOrEmpty(icon))
                .Distinct()
                .ToList();

            if (iconKeys.Count == 0)
            {
                return;
            }

            try
            {
                var iconResolver = App.Container.Resolve<IconResolver>();
                await iconResolver.WarmUpAsync(iconKeys);
            }
            catch (Exception ex)
            {
                _log.Debug(ex, "Failed to warm up icon cache");
            }
        }

        private async Task LoadCategoriesAsync()
        {
            var cats = await _categoryRepository.GetAllAsync();
            _categories.Clear();
            _categories.Add(new Category { Id = null, Name = "All" });

            foreach (var cat in cats.OrderBy(c => c.Ranking))
            {
                _categories.Add(cat);
            }

            OnPropertyChanged(nameof(Categories));
        }

        private void ApplyFilter()
        {
            // Run filter asynchronously to avoid UI thread blocking
            _ = ApplyFilterAsync();
        }

        private async Task ApplyFilterAsync()
        {
            var filtered = _authenticators.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                var search = _searchText.ToLowerInvariant();
                filtered = filtered.Where(a =>
                    a.Issuer.ToLowerInvariant().Contains(search) ||
                    (!string.IsNullOrEmpty(a.Username) && a.Username.ToLowerInvariant().Contains(search)));
            }

            if (_selectedCategory != null && !string.IsNullOrEmpty(_selectedCategory.Id))
            {
                var bindings = await _authenticatorCategoryRepository.GetAllForCategoryAsync(_selectedCategory).ConfigureAwait(false);
                var secrets = new HashSet<string>(bindings.Select(b => b.AuthenticatorSecret));
                filtered = filtered.Where(a => secrets.Contains(a.Auth.Secret));
            }

            // Update UI on UI thread
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _filteredAuthenticators.Clear();
                foreach (var auth in filtered)
                {
                    _filteredAuthenticators.Add(auth);
                }

                OnPropertyChanged(nameof(FilteredAuthenticators));
                OnPropertyChanged(nameof(IsEmpty));
            });
        }

        private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Interlocked.Exchange(ref _updateInProgress, 1) == 1)
            {
                return;
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                Interlocked.Exchange(ref _updateInProgress, 0);
                return;
            }

            dispatcher.InvokeAsync(() =>
            {
                foreach (var auth in _authenticators)
                {
                    if (auth.IsTimeBased)
                    {
                        auth.UpdateCode();
                    }
                }
            }).Task.ContinueWith(_ => Interlocked.Exchange(ref _updateInProgress, 0));
        }

        private async void AddAuthenticator()
        {
            var dialog = new Views.AddAuthenticatorDialog
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                try
                {
                    await _authenticatorRepository.CreateAsync(dialog.Result);
                    _authenticators.Add(new AuthenticatorViewModel(dialog.Result));
                    ApplyFilter();
                    OnPropertyChanged(nameof(AuthenticatorCount));
                    OnPropertyChanged(nameof(IsEmpty));
                    _log.Information("Added authenticator for {Issuer}", dialog.Result.Issuer);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Failed to add authenticator");
                    MessageBox.Show(string.Format(LocalizationManager.GetString("AddFailed"), ex.Message), LocalizationManager.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void EditAuthenticator(AuthenticatorViewModel auth)
        {
            if (auth == null) return;

            var dialog = new Views.AddAuthenticatorDialog
            {
                Owner = Application.Current.MainWindow
            };

            dialog.LoadAuthenticator(auth.Auth);

            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                try
                {
                    var updated = dialog.Result;
                    auth.Auth.Issuer = updated.Issuer;
                    auth.Auth.Username = updated.Username;
                    auth.Auth.Secret = updated.Secret;
                    auth.Auth.Pin = updated.Pin;
                    auth.Auth.Algorithm = updated.Algorithm;
                    auth.Auth.Digits = updated.Digits;
                    auth.Auth.Period = updated.Period;
                    auth.Auth.Counter = updated.Counter;
                    auth.Auth.Icon = updated.Icon;
                    await _authenticatorRepository.UpdateAsync(auth.Auth);
                    auth.RefreshFromAuth();

                    ApplyFilter();
                    _log.Information("Updated authenticator for {Issuer}", updated.Issuer);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Failed to update authenticator");
                    MessageBox.Show(string.Format(LocalizationManager.GetString("UpdateFailed"), ex.Message), LocalizationManager.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void DeleteAuthenticator(AuthenticatorViewModel auth)
        {
            if (auth == null) return;

            var result = MessageBox.Show(
                $"Delete authenticator for {auth.Issuer}?",
                LocalizationManager.GetString("ConfirmDeleteTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _authenticatorRepository.DeleteAsync(auth.Auth);
                    await _authenticatorCategoryRepository.DeleteAllForAuthenticatorAsync(auth.Auth);
                    _authenticators.Remove(auth);
                    ApplyFilter();
                    OnPropertyChanged(nameof(AuthenticatorCount));
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Failed to delete authenticator");
                    MessageBox.Show(string.Format(LocalizationManager.GetString("DeleteFailed"), ex.Message), LocalizationManager.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CopyCode(AuthenticatorViewModel auth)
        {
            if (auth == null) return;

            try
            {
                Clipboard.SetText(auth.Code);
                auth.ShowCopiedFeedback();
                _log.Information("Copied code for {Issuer}", auth.Issuer);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to copy code");
            }
        }

        private void ShowQrCode(AuthenticatorViewModel auth)
        {
            if (auth == null) return;
            var dialog = new Views.QrCodeDialog(auth.Auth)
            {
                Owner = Application.Current.MainWindow
            };
            dialog.ShowDialog();
        }

        private async void IncrementCounter(AuthenticatorViewModel auth)
        {
            if (auth == null) return;

            try
            {
                auth.Auth.Counter++;
                await _authenticatorRepository.UpdateAsync(auth.Auth);
                _log.Information("Incremented counter for {Issuer}", auth.Issuer);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to increment counter");
                MessageBox.Show(string.Format(LocalizationManager.GetString("IncrementFailed"), ex.Message), LocalizationManager.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenSettings()
        {
            var settingsWindow = new Views.SettingsWindow
            {
                Owner = Application.Current.MainWindow
            };
            settingsWindow.ShowDialog();
        }

        private void FocusSearch()
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.FocusSearchBox();
        }

        private void ClearSearch()
        {
            SearchText = string.Empty;
        }

        private void OnSelectCategory(Category category)
        {
            try
            {
                SelectedCategory = category;
                OnPropertyChanged(nameof(CurrentCategoryName));
                ApplyFilter();
                _log.Information("Selected category: {CategoryName}", category?.Name ?? "All");
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to select category");
                // Reset to default state on error
                SelectedCategory = null;
                OnPropertyChanged(nameof(CurrentCategoryName));
            }
        }

        public void SortAuthenticators(string sortType)
        {
            try
            {
                IEnumerable<AuthenticatorViewModel> sorted = sortType switch
                {
                    "NameAsc" => _authenticators.OrderBy(a => a.Issuer).ThenBy(a => a.Username),
                    "NameDesc" => _authenticators.OrderByDescending(a => a.Issuer).ThenByDescending(a => a.Username),
                    "CopyCountAsc" => _authenticators.OrderBy(a => a.CopyCount).ThenBy(a => a.Issuer),
                    "CopyCountDesc" => _authenticators.OrderByDescending(a => a.CopyCount).ThenBy(a => a.Issuer),
                    "Custom" => _authenticators.OrderBy(a => a.Auth.Ranking).ThenBy(a => a.Issuer),
                    _ => _authenticators.OrderBy(a => a.Auth.Ranking).ThenBy(a => a.Issuer)
                };

                var sortedList = sorted.ToList();
                _authenticators.Clear();
                
                foreach (var auth in sortedList)
                {
                    _authenticators.Add(auth);
                }

                ApplyFilter();
                _log.Information("Sorted authenticators by {SortType}", sortType);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to sort authenticators");
            }
        }

        public async void ReorderAuthenticators(AuthenticatorViewModel draggedAuth, AuthenticatorViewModel targetAuth)
        {
            try
            {
                var draggedIndex = _authenticators.IndexOf(draggedAuth);
                var targetIndex = _authenticators.IndexOf(targetAuth);

                if (draggedIndex == -1 || targetIndex == -1 || draggedIndex == targetIndex)
                    return;

                // Remove the dragged item
                _authenticators.RemoveAt(draggedIndex);

                // Adjust target index if necessary
                if (draggedIndex < targetIndex)
                    targetIndex--;

                // Insert at new position
                _authenticators.Insert(targetIndex, draggedAuth);

                // Update ranking in database
                for (int i = 0; i < _authenticators.Count; i++)
                {
                    _authenticators[i].Auth.Ranking = i;
                    await _authenticatorRepository.UpdateAsync(_authenticators[i].Auth);
                }

                ApplyFilter();
                _log.Information("Reordered authenticator {Issuer} from position {OldIndex} to {NewIndex}", 
                    draggedAuth.Issuer, draggedIndex, targetIndex);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to reorder authenticators");
                // Reload authenticators to restore original order
                await LoadAuthenticatorsAsync();
            }
        }

        public ICommand AddAuthenticatorCommand { get; private set; }
        public ICommand EditAuthenticatorCommand { get; private set; }
        public ICommand DeleteAuthenticatorCommand { get; private set; }
        public ICommand CopyCodeCommand { get; private set; }
        public ICommand ShowQrCodeCommand { get; private set; }
        public ICommand IncrementCounterCommand { get; private set; }
        public ICommand OpenSettingsCommand { get; private set; }
        public ICommand FocusSearchCommand { get; private set; }
        public ICommand ClearSearchCommand { get; private set; }
        public ICommand SelectCategoryCommand { get; private set; }
        public ICommand ReorderAuthenticatorsCommand { get; private set; }

        public ObservableCollection<AuthenticatorViewModel> Authenticators => _authenticators;
        public ObservableCollection<AuthenticatorViewModel> FilteredAuthenticators => _filteredAuthenticators;
        public ObservableCollection<Category> Categories => _categories;

        public int AuthenticatorCount => _authenticators.Count;
        public bool IsEmpty => _authenticators.Count == 0;

        public string CurrentCategoryName => string.IsNullOrEmpty(_selectedCategory?.Name) ? "All" : _selectedCategory.Name;

        public ValidatorDisplayMode DisplayMode
        {
            get => _preferenceManager.Preferences.DisplayMode;
            set
            {
                if (_preferenceManager.Preferences.DisplayMode != value)
                {
                    _preferenceManager.Preferences.DisplayMode = value;
                    OnPropertyChanged();
                }
            }
        }

        public ValidatorColumnLayout ColumnLayout
        {
            get => _preferenceManager.Preferences.ColumnLayout;
            set
            {
                if (_preferenceManager.Preferences.ColumnLayout != value)
                {
                    _preferenceManager.Preferences.ColumnLayout = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    ApplyFilter();
                }
            }
        }

        public Category SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory != value)
                {
                    _selectedCategory = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentCategoryName));
                    ApplyFilter();
                }
            }
        }

        public AuthenticatorViewModel SelectedAuthenticator
        {
            get => _selectedAuthenticator;
            set
            {
                if (_selectedAuthenticator != value)
                {
                    _selectedAuthenticator = value;
                    OnPropertyChanged();
                }
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
        }
    }
}
