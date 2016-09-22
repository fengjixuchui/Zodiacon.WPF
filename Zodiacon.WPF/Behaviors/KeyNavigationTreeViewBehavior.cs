﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;
using System.Windows.Threading;
using Zodiacon.WPF.Utilities;

namespace Zodiacon.WPF.Behaviors {
    public sealed class KeyNavigationTreeViewBehavior : Behavior<TreeView> {
        string _searchterm = string.Empty;
        DateTime _lastSearch = DateTime.Now;
        DispatcherTimer _timer;

        protected override void OnAttached() {
            base.OnAttached();

            AssociatedObject.KeyUp += AssociatedObject_KeyUp;
            AssociatedObject.TextInput += AssociatedObject_TextInput;

            _timer = new DispatcherTimer { Interval = Interval };
            _timer.Tick += _timer_Tick;
        }



        public TimeSpan Interval {
            get { return (TimeSpan)GetValue(IntervalProperty); }
            set { SetValue(IntervalProperty, value); }
        }

        public static readonly DependencyProperty IntervalProperty =
            DependencyProperty.Register(nameof(Interval), typeof(TimeSpan), typeof(KeyNavigationTreeViewBehavior), new PropertyMetadata(TimeSpan.FromSeconds(.5)));


        void _timer_Tick(object sender, EventArgs e) {
            _timer.Stop();

            if(string.IsNullOrEmpty(_searchterm))
                return;

            var item = AssociatedObject.SelectedItem as ITreeViewItem;
            if(item == null) return;

            var found = SearchSubTree(item);
            if(found != null) {
                var tv = GetTreeViewItemFromObject(found);
                Debug.Assert(tv != null);
                found.IsSelected = true;
            }
            else {
                NativeMethods.MessageBeep(0x30);    // short beep
            }

            _lastSearch = DateTime.Now;
            _searchterm = string.Empty;
        }

        void AssociatedObject_TextInput(object sender, TextCompositionEventArgs e) {
            _timer.Stop();
            _searchterm += e.Text;

            _timer.Start();
        }

        TreeViewItem GetTreeViewItemFromObject(ITreeViewItem item) {
            var indices = new List<int>(4);
            while(item.Parent != null) {
                indices.Add(item.Parent.SubItems.IndexOf(item));
                item = item.Parent;
            }
            indices.Add(0);

            // search the tree based on indices

            int index = indices.Count;
            var currentTreeView = AssociatedObject.ItemContainerGenerator.ContainerFromIndex(indices[--index]) as TreeViewItem;
            Debug.Assert(currentTreeView != null);
            while(index > 0) {
                var container = currentTreeView;
                currentTreeView = currentTreeView.ItemContainerGenerator.ContainerFromIndex(indices[--index]) as TreeViewItem;
                if(currentTreeView == null) {       // virtualized
                    GetPanelForTreeViewItem(container).BringIntoView(indices[index]);
                    currentTreeView = container.ItemContainerGenerator.ContainerFromIndex(indices[index]) as TreeViewItem;
                }
                Debug.Assert(currentTreeView != null);
            }

            return currentTreeView;
        }

        VirtualizingStackPanelEx GetPanelForTreeViewItem(TreeViewItem container) {
            container.ApplyTemplate();
            ItemsPresenter itemsPresenter =
                 (ItemsPresenter)container.Template.FindName("ItemsHost", container);
            if(itemsPresenter != null) {
                itemsPresenter.ApplyTemplate();
            }
            else {
                // The Tree template has not named the ItemsPresenter, 
                // so walk the descendants and find the child

                itemsPresenter = TreeViewHelper.FindVisualChild<ItemsPresenter>(container);
                if(itemsPresenter == null) {
                    container.UpdateLayout();

                    itemsPresenter = TreeViewHelper.FindVisualChild<ItemsPresenter>(container);
                }
            }

            var itemsHostPanel = (Panel)VisualTreeHelper.GetChild(itemsPresenter, 0);

            var children = itemsHostPanel.Children;

            var virtualizingPanel = itemsHostPanel as VirtualizingStackPanelEx;
            Debug.Assert(virtualizingPanel != null);

            return virtualizingPanel;
        }

        ITreeViewItem BinarySearch(IList<ITreeViewItem> items) {
            int index1 = 0, index2 = items.Count;
            string lower = _searchterm.ToLower();

            while(index1 != index2) {
                int i = (index1 + index2) / 2;
                if(items[i].Text.StartsWith(_searchterm, StringComparison.CurrentCultureIgnoreCase))
                    return items[i];
                if(items[i].Text.ToLower().CompareTo(lower) > 0)
                    index2 = i;
                else
                    index1 = i;
            }
            return null;
        }

        private ITreeViewItem SearchSubTree(ITreeViewItem item) {
            if(item.IsExpanded && item.SubItems != null) {
                foreach(var subItem in item.SubItems) {
                    if(subItem.Text.StartsWith(_searchterm, StringComparison.CurrentCultureIgnoreCase)) {
                        return subItem;
                    }
                    ITreeViewItem newItem;
                    if(subItem.IsExpanded && (newItem = SearchSubTree(subItem)) != null) {
                        return newItem;
                    }
                }
            }
            if(item.Parent != null) {
                foreach(var subItem in item.Parent.SubItems.Skip(item.Parent.SubItems.IndexOf(item) + 1)) {
                    if(subItem.Text.StartsWith(_searchterm, StringComparison.CurrentCultureIgnoreCase)) {
                        return subItem;
                    }
                }

            }
            return null;
        }

        protected override void OnDetaching() {
            AssociatedObject.KeyUp -= AssociatedObject_KeyUp;
            AssociatedObject.TextInput -= AssociatedObject_TextInput;

            base.OnDetaching();
        }

        void AssociatedObject_KeyUp(object sender, System.Windows.Input.KeyEventArgs e) {
            if(e.Key < Key.D0) {
                _searchterm = string.Empty;
            }
        }


    }
}
