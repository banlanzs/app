// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Autofac;
using Serilog;
using Stratum.Desktop.Services;

namespace Stratum.Desktop.Behaviors
{
    public static class DragReorderBehavior
    {
        private static readonly ILogger _log = Log.ForContext(typeof(DragReorderBehavior));

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(DragReorderBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static readonly DependencyProperty ReorderCommandProperty =
            DependencyProperty.RegisterAttached("ReorderCommand", typeof(ICommand), typeof(DragReorderBehavior));

        public static readonly DependencyProperty ClickCommandProperty =
            DependencyProperty.RegisterAttached("ClickCommand", typeof(ICommand), typeof(DragReorderBehavior));

        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

        public static ICommand GetReorderCommand(DependencyObject obj) => (ICommand)obj.GetValue(ReorderCommandProperty);
        public static void SetReorderCommand(DependencyObject obj, ICommand value) => obj.SetValue(ReorderCommandProperty, value);

        public static ICommand GetClickCommand(DependencyObject obj) => (ICommand)obj.GetValue(ClickCommandProperty);
        public static void SetClickCommand(DependencyObject obj, ICommand value) => obj.SetValue(ClickCommandProperty, value);

        private static readonly DependencyProperty DragStateProperty =
            DependencyProperty.RegisterAttached("DragState", typeof(DragState), typeof(DragReorderBehavior));

        private sealed class DragState
        {
            public Point StartPoint;
            public object DraggedItem;
            public bool IsDragging;
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListBox listBox) return;

            _log.Information("OnIsEnabledChanged: IsEnabled changed from {OldValue} to {NewValue}", e.OldValue, e.NewValue);

            if ((bool)e.NewValue)
            {
                listBox.SetValue(DragStateProperty, new DragState());
                listBox.PreviewMouseLeftButtonDown += OnMouseDown;
                listBox.PreviewMouseMove += OnMouseMove;
                listBox.PreviewMouseLeftButtonUp += OnMouseUp;
                listBox.DragOver += OnDragOver;
                listBox.Drop += OnDrop;
                listBox.Unloaded += OnUnloaded;
                _log.Information("OnIsEnabledChanged: Event handlers attached");
            }
            else
            {
                Detach(listBox);
            }
        }

        private static void Detach(ListBox listBox)
        {
            listBox.PreviewMouseLeftButtonDown -= OnMouseDown;
            listBox.PreviewMouseMove -= OnMouseMove;
            listBox.PreviewMouseLeftButtonUp -= OnMouseUp;
            listBox.DragOver -= OnDragOver;
            listBox.Drop -= OnDrop;
            listBox.Unloaded -= OnUnloaded;
            listBox.ClearValue(DragStateProperty);
        }

        private static void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                Detach(listBox);
            }
        }

        private static void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ListBox listBox) return;
            var state = (DragState)listBox.GetValue(DragStateProperty);
            if (state == null)
            {
                _log.Warning("OnMouseDown: DragState is null!");
                return;
            }

            state.StartPoint = e.GetPosition(listBox);
            state.DraggedItem = GetItemUnderMouse(listBox, state.StartPoint);
            state.IsDragging = false;
            _log.Debug("OnMouseDown: StartPoint = {Point}, DraggedItem = {Item}", state.StartPoint, state.DraggedItem);
        }

        private static void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not ListBox listBox) return;
            var state = (DragState)listBox.GetValue(DragStateProperty);
            if (state == null || state.DraggedItem == null || state.IsDragging) return;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point mousePos = e.GetPosition(listBox);
                Vector diff = state.StartPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    state.IsDragging = true;
                    DragDrop.DoDragDrop(listBox, state.DraggedItem, DragDropEffects.Move);
                    state.IsDragging = false;
                    state.DraggedItem = null;
                }
            }
        }

        private static void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ListBox listBox) return;
            var state = (DragState)listBox.GetValue(DragStateProperty);
            if (state == null) return;

            if (!state.IsDragging)
            {
                // Re-perform hit-testing at mouse up position to ensure accuracy
                Point mousePos = e.GetPosition(listBox);
                var clickedItem = GetItemUnderMouse(listBox, mousePos);
                _log.Debug("OnMouseUp: clickedItem = {Item}", clickedItem);

                if (clickedItem != null)
                {
                    var cmd = GetClickCommand(listBox);
                    _log.Debug("OnMouseUp: ClickCommand = {Command}, CanExecute = {CanExecute}",
                        cmd, cmd?.CanExecute(clickedItem));

                    if (cmd != null && cmd.CanExecute(clickedItem))
                    {
                        cmd.Execute(clickedItem);
                        _log.Information("OnMouseUp: Executed ClickCommand for item");
                    }
                }
            }

            state.DraggedItem = null;
            state.IsDragging = false;
        }

        private static void OnDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(typeof(object)) ? DragDropEffects.Move : DragDropEffects.None;
        }

        private static void OnDrop(object sender, DragEventArgs e)
        {
            if (sender is not ListBox listBox) return;

            var dragged = e.Data.GetData(e.Data.GetFormats()[0]);
            var target = GetItemUnderMouse(listBox, e.GetPosition(listBox));

            if (dragged != null && target != null && !ReferenceEquals(dragged, target))
            {
                var cmd = GetReorderCommand(listBox);
                if (cmd != null)
                {
                    var request = new { Dragged = dragged, Target = target };
                    if (cmd.CanExecute(request))
                    {
                        cmd.Execute(request);
                    }
                }
            }
        }

        private static object GetItemUnderMouse(ListBox listBox, Point position)
        {
            var element = listBox.InputHitTest(position) as DependencyObject;
            while (element != null && element != listBox)
            {
                if (element is ListBoxItem item)
                {
                    return listBox.ItemContainerGenerator.ItemFromContainer(item);
                }
                element = VisualTreeHelper.GetParent(element);
            }
            return null;
        }
    }
}
