// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Stratum.Desktop.Collections
{
    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        private bool _suppressNotification;

        public void ReplaceAll(IEnumerable<T> items)
        {
            _suppressNotification = true;
            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(item);
            }
            _suppressNotification = false;
            base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            base.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotification)
            {
                base.OnCollectionChanged(e);
            }
        }
    }
}
