// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;
using Stratum.Desktop.Services;
using Stratum.Desktop.ViewModels;

namespace Stratum.Test.Integration
{
    public class CategoryFilterIntegrationTest : IDisposable
    {
        private readonly Mock<IAuthenticatorRepository> _mockAuthRepo;
        private readonly Mock<ICategoryRepository> _mockCategoryRepo;
        private readonly Mock<IAuthenticatorCategoryRepository> _mockAuthCategoryRepo;
        private readonly Mock<PreferenceManager> _mockPreferenceManager;
        private readonly MainViewModel _viewModel;

        public CategoryFilterIntegrationTest()
        {
            _mockAuthRepo = new Mock<IAuthenticatorRepository>();
            _mockCategoryRepo = new Mock<ICategoryRepository>();
            _mockAuthCategoryRepo = new Mock<IAuthenticatorCategoryRepository>();
            _mockPreferenceManager = new Mock<PreferenceManager>();

            // Setup test data
            var categories = new List<Category>
            {
                new Category { Id = "1", Name = "Work" },
                new Category { Id = "2", Name = "Personal" },
                new Category { Id = "3", Name = "Banking" }
            };

            var authenticators = new List<Authenticator>
            {
                new Authenticator { Secret = "secret1", Issuer = "Google", Username = "work@example.com" },
                new Authenticator { Secret = "secret2", Issuer = "GitHub", Username = "personal@example.com" }
            };

            _mockAuthRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(authenticators);
            _mockCategoryRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(categories);
            _mockAuthCategoryRepo.Setup(x => x.GetAllForCategoryAsync(It.IsAny<Category>()))
                .ReturnsAsync(new List<AuthenticatorCategory>());

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
        public void CategoryFilterControl_HasRequiredDependencyProperties()
        {
            // Assert that all required dependency properties are defined
            Assert.NotNull(CategoryFilterControl.CategoriesProperty);
            Assert.NotNull(CategoryFilterControl.SelectedCategoryProperty);
            Assert.NotNull(CategoryFilterControl.SelectCategoryCommandProperty);
            Assert.NotNull(CategoryFilterControl.CurrentCategoryNameProperty);
        }

        [Fact]
        public void CategoryFilterControl_DefaultValues_AreCorrect()
        {
            // Assert default values
            Assert.Null(_control.Categories);
            Assert.Null(_control.SelectedCategory);
            Assert.Null(_control.SelectCategoryCommand);
            Assert.Equal("All", _control.CurrentCategoryName);
        }

        [Fact]
        public void CategoryFilterControl_WhenCategoriesSet_PropertyIsUpdated()
        {
            // Arrange
            var categories = new ObservableCollection<Category>
            {
                new Category { Id = "1", Name = "Work" },
                new Category { Id = "2", Name = "Personal" }
            };

            // Act
            _control.Categories = categories;

            // Assert
            Assert.Equal(categories, _control.Categories);
        }

        [Fact]
        public void CategoryFilterControl_WhenSelectedCategorySet_CurrentCategoryNameUpdates()
        {
            // Arrange
            var category = new Category { Id = "1", Name = "Work" };

            // Act
            _control.SelectedCategory = category;

            // Assert
            Assert.Equal(category, _control.SelectedCategory);
            Assert.Equal("Work", _control.CurrentCategoryName);
        }

        [Fact]
        public void CategoryFilterControl_WhenSelectedCategorySetToNull_CurrentCategoryNameIsAll()
        {
            // Arrange
            _control.SelectedCategory = new Category { Id = "1", Name = "Work" };

            // Act
            _control.SelectedCategory = null;

            // Assert
            Assert.Null(_control.SelectedCategory);
            Assert.Equal("All", _control.CurrentCategoryName);
        }

        [Fact]
        public async Task MainViewModel_InitializeAsync_LoadsCategoriesWithAllOption()
        {
            // Act
            await _viewModel.InitializeAsync();

            // Assert
            Assert.NotEmpty(_viewModel.Categories);
            Assert.Equal("All", _viewModel.Categories.First().Name);
            Assert.Null(_viewModel.Categories.First().Id);
        }

        [Fact]
        public async Task MainViewModel_SelectCategoryCommand_UpdatesSelectedCategoryAndCurrentName()
        {
            // Arrange
            await _viewModel.InitializeAsync();
            var workCategory = _viewModel.Categories.FirstOrDefault(c => c.Name == "Work");
            Assert.NotNull(workCategory);

            // Act
            _viewModel.SelectCategoryCommand.Execute(workCategory);

            // Assert
            Assert.Equal(workCategory, _viewModel.SelectedCategory);
            Assert.Equal("Work", _viewModel.CurrentCategoryName);
        }

        [Fact]
        public async Task MainViewModel_SelectAllCategory_ClearsSelectedCategory()
        {
            // Arrange
            await _viewModel.InitializeAsync();
            var allCategory = _viewModel.Categories.First(c => c.Name == "All");
            
            // First select a specific category
            var workCategory = _viewModel.Categories.FirstOrDefault(c => c.Name == "Work");
            _viewModel.SelectCategoryCommand.Execute(workCategory);
            
            // Act - Select "All"
            _viewModel.SelectCategoryCommand.Execute(allCategory);

            // Assert
            Assert.Equal(allCategory, _viewModel.SelectedCategory);
            Assert.Equal("All", _viewModel.CurrentCategoryName);
        }

        [Fact]
        public void MainViewModel_CurrentCategoryName_HandlesEdgeCases()
        {
            // Test null category
            _viewModel.SelectedCategory = null;
            Assert.Equal("All", _viewModel.CurrentCategoryName);

            // Test category with empty name
            _viewModel.SelectedCategory = new Category { Id = "1", Name = "" };
            Assert.Equal("All", _viewModel.CurrentCategoryName);

            // Test category with null name
            _viewModel.SelectedCategory = new Category { Id = "1", Name = null };
            Assert.Equal("All", _viewModel.CurrentCategoryName);

            // Test category with valid name
            _viewModel.SelectedCategory = new Category { Id = "1", Name = "Work" };
            Assert.Equal("Work", _viewModel.CurrentCategoryName);
        }
    }
}