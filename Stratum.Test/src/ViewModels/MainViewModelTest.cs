// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;
using Stratum.Desktop.Services;
using Stratum.Desktop.ViewModels;

namespace Stratum.Test.ViewModels
{
    public class MainViewModelTest : IDisposable
    {
        private readonly Mock<IAuthenticatorRepository> _mockAuthRepo;
        private readonly Mock<ICategoryRepository> _mockCategoryRepo;
        private readonly Mock<IAuthenticatorCategoryRepository> _mockAuthCategoryRepo;
        private readonly Mock<PreferenceManager> _mockPreferenceManager;
        private readonly MainViewModel _viewModel;

        public MainViewModelTest()
        {
            _mockAuthRepo = new Mock<IAuthenticatorRepository>();
            _mockCategoryRepo = new Mock<ICategoryRepository>();
            _mockAuthCategoryRepo = new Mock<IAuthenticatorCategoryRepository>();
            _mockPreferenceManager = new Mock<PreferenceManager>();

            // Setup default returns for repositories
            _mockAuthRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Authenticator>());
            _mockCategoryRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Category>());

            _viewModel = new MainViewModel(
                _mockAuthRepo.Object,
                _mockCategoryRepo.Object,
                _mockAuthCategoryRepo.Object,
                _mockPreferenceManager.Object);
        }

        public void Dispose()
        {
            _viewModel?.Dispose();
        }

        [Fact]
        public void CurrentCategoryName_WhenSelectedCategoryIsNull_ReturnsAll()
        {
            // Arrange
            _viewModel.SelectedCategory = null;

            // Act
            var result = _viewModel.CurrentCategoryName;

            // Assert
            Assert.Equal("All", result);
        }

        [Fact]
        public void CurrentCategoryName_WhenSelectedCategoryHasName_ReturnsName()
        {
            // Arrange
            var category = new Category { Id = "1", Name = "Work" };
            _viewModel.SelectedCategory = category;

            // Act
            var result = _viewModel.CurrentCategoryName;

            // Assert
            Assert.Equal("Work", result);
        }

        [Fact]
        public void CurrentCategoryName_WhenSelectedCategoryNameIsEmpty_ReturnsAll()
        {
            // Arrange
            var category = new Category { Id = "1", Name = "" };
            _viewModel.SelectedCategory = category;

            // Act
            var result = _viewModel.CurrentCategoryName;

            // Assert
            Assert.Equal("All", result);
        }

        [Fact]
        public void CurrentCategoryName_WhenSelectedCategoryNameIsNull_ReturnsAll()
        {
            // Arrange
            var category = new Category { Id = "1", Name = null };
            _viewModel.SelectedCategory = category;

            // Act
            var result = _viewModel.CurrentCategoryName;

            // Assert
            Assert.Equal("All", result);
        }

        [Fact]
        public void SelectedCategory_WhenChanged_TriggersPropertyChangedForCurrentCategoryName()
        {
            // Arrange
            var propertyChangedEvents = new List<string>();
            _viewModel.PropertyChanged += (sender, e) => propertyChangedEvents.Add(e.PropertyName);

            var category = new Category { Id = "1", Name = "Personal" };

            // Act
            _viewModel.SelectedCategory = category;

            // Assert
            Assert.Contains("CurrentCategoryName", propertyChangedEvents);
        }

        [Fact]
        public void SelectCategoryCommand_WhenExecuted_UpdatesSelectedCategoryAndTriggersPropertyChanged()
        {
            // Arrange
            var propertyChangedEvents = new List<string>();
            _viewModel.PropertyChanged += (sender, e) => propertyChangedEvents.Add(e.PropertyName);

            var category = new Category { Id = "2", Name = "Banking" };

            // Act
            _viewModel.SelectCategoryCommand.Execute(category);

            // Assert
            Assert.Equal(category, _viewModel.SelectedCategory);
            Assert.Contains("CurrentCategoryName", propertyChangedEvents);
        }

        [Fact]
        public void SelectCategoryCommand_WhenExecutedWithNull_SetsSelectedCategoryToNullAndCurrentCategoryNameToAll()
        {
            // Arrange
            // First set a category
            _viewModel.SelectedCategory = new Category { Id = "1", Name = "Work" };

            // Act
            _viewModel.SelectCategoryCommand.Execute(null);

            // Assert
            Assert.Null(_viewModel.SelectedCategory);
            Assert.Equal("All", _viewModel.CurrentCategoryName);
        }
    }
}