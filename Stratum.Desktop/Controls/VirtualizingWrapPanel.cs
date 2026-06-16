// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Stratum.Desktop.Controls
{
    /// <summary>
    /// VirtualizingWrapPanel with support for uniform item sizing
    /// </summary>
    public class VirtualizingWrapPanel : VirtualizingPanel, IScrollInfo
    {
        private const double ItemWidth = 320;
        private const double ItemHeight = 140;
        private const double ItemMargin = 16;

        private Size _extent = new Size(0, 0);
        private Size _viewport = new Size(0, 0);
        private Point _offset = new Point(0, 0);
        private ScrollViewer _scrollOwner;

        protected override Size MeasureOverride(Size availableSize)
        {
            UpdateScrollInfo(availableSize);

            var itemsControl = ItemsControl.GetItemsOwner(this);
            var itemCount = itemsControl?.Items.Count ?? 0;

            if (itemCount == 0)
            {
                return new Size(0, 0);
            }

            // Calculate columns that fit in the available width
            var columns = Math.Max(1, (int)((availableSize.Width + ItemMargin) / (ItemWidth + ItemMargin)));
            var rows = (int)Math.Ceiling((double)itemCount / columns);

            // Calculate which items are visible
            var firstVisibleRow = (int)(_offset.Y / (ItemHeight + ItemMargin));
            var lastVisibleRow = (int)((_offset.Y + _viewport.Height) / (ItemHeight + ItemMargin)) + 1;

            firstVisibleRow = Math.Max(0, firstVisibleRow);
            lastVisibleRow = Math.Min(rows, lastVisibleRow);

            // Virtualize: only create containers for visible items
            var generator = ItemContainerGenerator;
            var startPos = generator.GeneratorPositionFromIndex(firstVisibleRow * columns);
            var childIndex = (startPos.Offset == 0) ? startPos.Index : startPos.Index + 1;

            using (generator.StartAt(startPos, GeneratorDirection.Forward, true))
            {
                for (int row = firstVisibleRow; row < lastVisibleRow; row++)
                {
                    for (int col = 0; col < columns; col++)
                    {
                        var itemIndex = row * columns + col;
                        if (itemIndex >= itemCount)
                            break;

                        bool isNewlyRealized;
                        var child = generator.GenerateNext(out isNewlyRealized) as UIElement;

                        if (isNewlyRealized)
                        {
                            if (childIndex >= InternalChildren.Count)
                            {
                                AddInternalChild(child);
                            }
                            else
                            {
                                InsertInternalChild(childIndex, child);
                            }
                            generator.PrepareItemContainer(child);
                        }

                        child?.Measure(new Size(ItemWidth, ItemHeight));
                        childIndex++;
                    }
                }
            }

            // Clean up unrealized children
            CleanUpItems(firstVisibleRow * columns, lastVisibleRow * columns);

            // Update extent
            _extent = new Size(
                columns * (ItemWidth + ItemMargin) - ItemMargin,
                rows * (ItemHeight + ItemMargin) - ItemMargin
            );

            return _extent;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var generator = ItemContainerGenerator;
            var columns = Math.Max(1, (int)((finalSize.Width + ItemMargin) / (ItemWidth + ItemMargin)));

            for (int i = 0; i < InternalChildren.Count; i++)
            {
                var child = InternalChildren[i];
                var itemIndex = generator.IndexFromGeneratorPosition(new GeneratorPosition(i, 0));

                if (itemIndex < 0)
                    continue;

                var row = itemIndex / columns;
                var col = itemIndex % columns;

                var x = col * (ItemWidth + ItemMargin);
                var y = row * (ItemHeight + ItemMargin) - _offset.Y;

                child.Arrange(new Rect(x, y, ItemWidth, ItemHeight));
            }

            return finalSize;
        }

        private void CleanUpItems(int minIndex, int maxIndex)
        {
            var generator = ItemContainerGenerator;
            var children = InternalChildren;

            for (int i = children.Count - 1; i >= 0; i--)
            {
                var childGeneratorPos = new GeneratorPosition(i, 0);
                var itemIndex = generator.IndexFromGeneratorPosition(childGeneratorPos);

                if (itemIndex < minIndex || itemIndex >= maxIndex)
                {
                    generator.Remove(childGeneratorPos, 1);
                    RemoveInternalChildRange(i, 1);
                }
            }
        }

        private void UpdateScrollInfo(Size availableSize)
        {
            _viewport = availableSize;
            _scrollOwner?.InvalidateScrollInfo();
        }

        #region IScrollInfo Implementation

        public bool CanHorizontallyScroll { get; set; }
        public bool CanVerticallyScroll { get; set; }

        public double ExtentHeight => _extent.Height;
        public double ExtentWidth => _extent.Width;

        public double ViewportHeight => _viewport.Height;
        public double ViewportWidth => _viewport.Width;

        public double HorizontalOffset => _offset.X;
        public double VerticalOffset => _offset.Y;

        public ScrollViewer ScrollOwner
        {
            get => _scrollOwner;
            set => _scrollOwner = value;
        }

        public void LineUp() => SetVerticalOffset(VerticalOffset - 20);
        public void LineDown() => SetVerticalOffset(VerticalOffset + 20);
        public void LineLeft() => SetHorizontalOffset(HorizontalOffset - 20);
        public void LineRight() => SetHorizontalOffset(HorizontalOffset + 20);

        public void PageUp() => SetVerticalOffset(VerticalOffset - ViewportHeight);
        public void PageDown() => SetVerticalOffset(VerticalOffset + ViewportHeight);
        public void PageLeft() => SetHorizontalOffset(HorizontalOffset - ViewportWidth);
        public void PageRight() => SetHorizontalOffset(HorizontalOffset + ViewportWidth);

        public void MouseWheelUp() => SetVerticalOffset(VerticalOffset - 48);
        public void MouseWheelDown() => SetVerticalOffset(VerticalOffset + 48);
        public void MouseWheelLeft() => SetHorizontalOffset(HorizontalOffset - 48);
        public void MouseWheelRight() => SetHorizontalOffset(HorizontalOffset + 48);

        public void SetHorizontalOffset(double offset)
        {
            offset = Math.Max(0, Math.Min(offset, ExtentWidth - ViewportWidth));
            if (Math.Abs(offset - _offset.X) > 0.1)
            {
                _offset.X = offset;
                InvalidateMeasure();
                _scrollOwner?.InvalidateScrollInfo();
            }
        }

        public void SetVerticalOffset(double offset)
        {
            offset = Math.Max(0, Math.Min(offset, ExtentHeight - ViewportHeight));
            if (Math.Abs(offset - _offset.Y) > 0.1)
            {
                _offset.Y = offset;
                InvalidateMeasure();
                _scrollOwner?.InvalidateScrollInfo();
            }
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            return rectangle;
        }

        #endregion
    }
}
