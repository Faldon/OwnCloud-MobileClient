using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Controls;

namespace Nextcloud.Extensions
{
    public static class GridExtensions
    {
        public static void SetGridRows(this Grid target, int count) {
            target.RowDefinitions.Clear();
            for (int i = 0; i < count; i++) {
                target.RowDefinitions.Add(new RowDefinition() { Height = new Windows.UI.Xaml.GridLength(target.ActualWidth / 7) });
            }
        }

        public static void SetGridColumns(this Grid target, int count) {
            target.ColumnDefinitions.Clear();
            for (int i = 0; i < count; i++) {
                target.ColumnDefinitions.Add(new ColumnDefinition());
            }
        }

        public static int GetGridRows(this Grid target) {
            return target.RowDefinitions.Count;
        }
    }
}
