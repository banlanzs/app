// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Stratum.Core.Entity;

namespace Stratum.Desktop.Controls
{
    public partial class CategoryFilterControl : UserControl
    {
        #region Dependency Properties

        /// <summary>
        /// Categories collection dependency property
        /// </summary>
        public static readonly DependencyProperty CategoriesProperty =
            DependencyProperty.Register(
                nameof(Categories),
                typeof(IEnumerable),
                typeof(CategoryFilterControl),
                new PropertyMetadata(null));

        /// <summary>
        /// Selected category dependency property
        /// </summary>
        public static readonly DependencyProperty SelectedCategoryProperty =
            DependencyProperty.Register(
                nameof(SelectedCategory),
                typeof(Category),
                typeof(CategoryFilterControl),
                new PropertyMetadata(null, OnSelectedCategoryChanged));

        /// <summary>
        /// Select category command dependency property
        /// </summary>
        public static readonly DependencyProperty SelectCategoryCommandProperty =
            DependencyProperty.Register(
                nameof(SelectCategoryCommand),
                typeof(ICommand),
                typeof(CategoryFilterControl),
                new PropertyMetadata(null));

        /// <summary>
        /// Current category name dependency property (computed from SelectedCategory)
        /// </summary>
        public static readonly DependencyProperty CurrentCategoryNameProperty =
            DependencyProperty.Register(
                nameof(CurrentCategoryName),
                typeof(string),
                typeof(CategoryFilterControl),
                new PropertyMetadata("All"));

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the categories collection
        /// </summary>
        public IEnumerable Categories
        {
            get => (IEnumerable)GetValue(CategoriesProperty);
            set => SetValue(CategoriesProperty, value);
        }

        /// <summary>
        /// Gets or sets the selected category
        /// </summary>
        public Category SelectedCategory
        {
            get => (Category)GetValue(SelectedCategoryProperty);
            set => SetValue(SelectedCategoryProperty, value);
        }

        /// <summary>
        /// Gets or sets the select category command
        /// </summary>
        public ICommand SelectCategoryCommand
        {
            get => (ICommand)GetValue(SelectCategoryCommandProperty);
            set => SetValue(SelectCategoryCommandProperty, value);
        }

        /// <summary>
        /// Gets or sets the current category name for display
        /// </summary>
        public string CurrentCategoryName
        {
            get => (string)GetValue(CurrentCategoryNameProperty);
            set => SetValue(CurrentCategoryNameProperty, value);
        }

        #endregion

        #region Constructor

        public CategoryFilterControl()
        {
            InitializeComponent();
            
            // Subscribe to popup events for arrow animation
            CategoryPopup.Opened += CategoryPopup_Opened;
            CategoryPopup.Closed += CategoryPopup_Closed;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the selected category property change
        /// </summary>
        private static void OnSelectedCategoryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CategoryFilterControl control)
            {
                var category = e.NewValue as Category;
                control.CurrentCategoryName = category?.Name ?? "All";
            }
        }

        /// <summary>
        /// Animates the dropdown arrow when popup opens
        /// </summary>
        private void CategoryPopup_Opened(object sender, System.EventArgs e)
        {
            AnimateArrow(180);
        }

        /// <summary>
        /// Animates the dropdown arrow when popup closes
        /// </summary>
        private void CategoryPopup_Closed(object sender, System.EventArgs e)
        {
            AnimateArrow(0);
        }

        /// <summary>
        /// Animates the dropdown arrow rotation
        /// </summary>
        private void AnimateArrow(double toAngle)
        {
            var animation = new DoubleAnimation
            {
                To = toAngle,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            ArrowRotation.BeginAnimation(RotateTransform.AngleProperty, animation);
        }

        #endregion
    }
}