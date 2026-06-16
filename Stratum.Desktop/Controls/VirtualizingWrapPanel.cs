// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Stratum.Desktop.Controls
{
    public class VirtualizingWrapPanel : VirtualizingPanel, IScrollInfo
    {
        private double _itemWidth = 320;
        private double _itemHeight = 180;
        private const double MinItemWidth = 300;
        private const double ItemSpacing = 16;

        private Size _extent;
        private Size _viewport;
        private Point _offset;
        private ScrollViewer _scrollOwner;
        private int _columns = 1;
        private int _rows;

        protected override Size MeasureOverride(Size availableSize)
        {
            if (double.IsInfinity(availableSize.Width) || double.IsNaN(availableSize.Width))
                availableSize.Width = 800;
            if (double.IsInfinity(availableSize.Height) || double.IsNaN(availableSize.Height))
                availableSize.Height = 600;

            var itemsControl = ItemsControl.GetItemsOwner(this);
            var itemCount = itemsControl?.Items.Count ?? 0;

            if (itemCount == 0)
            {
                _extent = _viewport = availableSize;
                return new Size(0, 0);
            }

            _viewport = availableSize;

            // 自适应列宽
            _columns = Math.Max(1, (int)(availableSize.Width / (MinItemWidth + ItemSpacing)));
            _itemWidth = (availableSize.Width - (_columns - 1) * ItemSpacing) / _columns;
            _rows = (int)Math.Ceiling((double)itemCount / _columns);

            _extent = new Size(availableSize.Width, _rows * (_itemHeight + ItemSpacing));

            // 可视区域的行范围
            var firstRow = Math.Max(0, (int)(_offset.Y / (_itemHeight + ItemSpacing)));
            var lastRow = Math.Min(_rows, (int)Math.Ceiling((_offset.Y + availableSize.Height) / (_itemHeight + ItemSpacing)) + 1);

            var generator = ItemContainerGenerator;
            if (generator == null)
                return new Size(availableSize.Width, Math.Max(0, _extent.Height - ItemSpacing));

            try
            {
                var startPos = generator.GeneratorPositionFromIndex(firstRow * _columns);
                var childIndex = startPos.Offset == 0 ? startPos.Index : startPos.Index + 1;

                using (generator.StartAt(startPos, GeneratorDirection.Forward, true))
                {
                    for (int row = firstRow; row < lastRow; row++)
                    {
                        for (int col = 0; col < _columns; col++)
                        {
                            var itemIndex = row * _columns + col;
                            if (itemIndex >= itemCount)
                                break;

                            bool isNewlyRealized;
                            var child = generator.GenerateNext(out isNewlyRealized) as UIElement;
                            if (child == null)
                                continue;

                            if (isNewlyRealized)
                            {
                                if (childIndex >= InternalChildren.Count)
                                    AddInternalChild(child);
                                else
                                    InsertInternalChild(childIndex, child);
                                generator.PrepareItemContainer(child);
                            }

                            child.Measure(new Size(_itemWidth, _itemHeight));
                            childIndex++;
                        }
                    }
                }

                CleanUpItems(firstRow * _columns, lastRow * _columns);
            }
            catch
            {
                // Ignore generator errors
            }

            return new Size(availableSize.Width, Math.Max(0, _extent.Height - ItemSpacing));
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var generator = ItemContainerGenerator;
            for (int i = 0; i < InternalChildren.Count; i++)
            {
                var child = InternalChildren[i];
                var itemIndex = generator?.IndexFromGeneratorPosition(new GeneratorPosition(i, 0)) ?? -1;
                if (itemIndex < 0) continue;

                var row = itemIndex / _columns;
                var col = itemIndex % _columns;

                var x = col * (_itemWidth + ItemSpacing);
                var y = row * (_itemHeight + ItemSpacing) - _offset.Y;

                child.Arrange(new Rect(x, y, _itemWidth, _itemHeight));
            }
            return finalSize;
        }

        private void CleanUpItems(int minIndex, int maxIndex)
        {
            var generator = ItemContainerGenerator;
            for (int i = InternalChildren.Count - 1; i >= 0; i--)
            {
                var pos = new GeneratorPosition(i, 0);
                var idx = generator?.IndexFromGeneratorPosition(pos) ?? -1;
                if (idx >= 0 && (idx < minIndex || idx >= maxIndex))
                {
                    generator.Remove(pos, 1);
                    RemoveInternalChildRange(i, 1);
                }
            }
        }

        #region IScrollInfo

        public bool CanHorizontallyScroll { get; set; }
        public bool CanVerticallyScroll { get; set; }
        public double ExtentHeight => _extent.Height;
        public double ExtentWidth => _extent.Width;
        public double ViewportHeight => _viewport.Height;
        public double ViewportWidth => _viewport.Width;
        public double HorizontalOffset => _offset.X;
        public double VerticalOffset => _offset.Y;
        public ScrollViewer ScrollOwner { get => _scrollOwner; set => _scrollOwner = value; }

        public void LineUp() => SetVerticalOffset(_offset.Y - 30);
        public void LineDown() => SetVerticalOffset(_offset.Y + 30);
        public void LineLeft() { }
        public void LineRight() { }
        public void PageUp() => SetVerticalOffset(_offset.Y - _viewport.Height);
        public void PageDown() => SetVerticalOffset(_offset.Y + _viewport.Height);
        public void PageLeft() { }
        public void PageRight() { }
        public void MouseWheelUp() => SetVerticalOffset(_offset.Y - 60);
        public void MouseWheelDown() => SetVerticalOffset(_offset.Y + 60);
        public void MouseWheelLeft() { }
        public void MouseWheelRight() { }

        public void SetHorizontalOffset(double offset) { }

        public void SetVerticalOffset(double offset)
        {
            offset = Math.Max(0, Math.Min(offset, Math.Max(0, _extent.Height - _viewport.Height)));
            if (Math.Abs(offset - _offset.Y) > 0.5)
            {
                _offset.Y = offset;
                InvalidateMeasure();
            }
        }

        public Rect MakeVisible(Visual visual, Rect rectangle) => rectangle;

        #endregion
    }
}
