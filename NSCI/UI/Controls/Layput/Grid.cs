﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using NDProperty;

namespace NSCI.UI.Controls.Layput
{
    public partial class Grid : ItemsControl
    {
        [NDPAttach]
        private static void OnRowChanged(OnChangedArg<int, UIElement> arg) { }
        [NDPAttach]
        private static void OnColumnChanged(OnChangedArg<int, UIElement> arg) { }

        [DefaultValue(1)]
        [NDPAttach]
        private static void OnRowSpanChanged(OnChangedArg<int, UIElement> arg) { }
        [DefaultValue(1)]
        [NDPAttach]
        private static void OnColumnSpanChanged(OnChangedArg<int, UIElement> arg) { }

        public ObservableCollection<ISizeDefinition> RowDefinitions { get; } = new ObservableCollection<ISizeDefinition>();
        public ObservableCollection<ISizeDefinition> ColumnDefinitions { get; } = new ObservableCollection<ISizeDefinition>();

        public Grid()
        {
            RowDefinitions.CollectionChanged += (sender, e) => InvalidateArrange();
            ColumnDefinitions.CollectionChanged += (sender, e) => InvalidateArrange();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            availableSize = EnsureMinMaxWidthHeight(availableSize);

            foreach (var item in Items)
                item.Measure(availableSize);


            var rows = this.RowDefinitions as IList<ISizeDefinition>;
            var columns = this.ColumnDefinitions as IList<ISizeDefinition>;
            if (rows.Count == 0)
                rows = new ISizeDefinition[] { new RelativSizeDefinition() };
            if (columns.Count == 0)
                columns = new ISizeDefinition[] { new RelativSizeDefinition() };
            int[] columnWidth = new int[columns.Count];
            int[] rowHeight = new int[rows.Count];

            // calculate singelspan items.

            for (int i = 0; i < columnWidth.Length; i++)
            {
                if (columns[i] is FixSizeDefinition f)
                    columnWidth[i] = f.Size;
                else
                    columnWidth[i] = Items
                        .Where(x => Grid.ColumnSpan[x].Value == 1 && Grid.Column[x].Value == i)
                        .Max(x => x.DesiredSize.Width);
            }
            for (int i = 0; i < rowHeight.Length; i++)
            {
                if (rows[i] is FixSizeDefinition f)
                    rowHeight[i] = f.Size;
                else
                    rowHeight[i] = Items
                        .Where(x => Grid.RowSpan[x].Value == 1 && Grid.Row[x].Value == i)
                        .Max(x => x.DesiredSize.Height);
            }

            // calculate multispan items
            foreach (var item in Items)
            {

                var row = Grid.Row[item].Value;
                var column = Grid.Column[item].Value;
                var rowSpan = Grid.RowSpan[item].Value;
                var columnSpan = Grid.ColumnSpan[item].Value;

                if (rowSpan == 1 && columnSpan == 1)
                    continue; // Nothing to do here. First step should be enough

                var xFrom = Math.Min(columns.Count - 1, column);
                var xTo = Math.Min(columns.Count - 1, column + columnSpan);
                var yFrom = Math.Min(rows.Count - 1, row);
                var yTo = Math.Min(rows.Count - 1, row + rowSpan);

                var usedWidth = columnWidth.Skip(xFrom).Take(columnSpan).Sum();
                var usedHeight = rowHeight.Skip(yFrom).Take(rowSpan).Sum();

                if (usedWidth > item.DesiredSize.Width && columnSpan != 1) // we need more space from relativ Size Elements
                {
                    var totalRelativSizeColumns = columns.Skip(xFrom).Take(columnSpan).OfType<RelativSizeDefinition>().Sum(x => x.Size);
                    if (totalRelativSizeColumns > 0)
                    {
                        var missingWidth = item.DesiredSize.Width - usedWidth;

                        for (int x = xFrom; x < xTo; x++)
                            if (columns[x] is RelativSizeDefinition r)
                                columnWidth[x] += (int)Math.Ceiling(missingWidth * r.Size / totalRelativSizeColumns);
                    }
                }

                if (usedHeight > item.DesiredSize.Height && rowSpan != 1) // we need more space from relativ Size Elements
                {
                    var totalRelativSizeColumns = rows.Skip(yFrom).Take(rowSpan).OfType<RelativSizeDefinition>().Sum(x => x.Size);
                    if (totalRelativSizeColumns > 0)
                    {
                        var missingHeight = item.DesiredSize.Height - usedHeight;

                        for (int y = yFrom; y < yTo; y++)
                            if (rows[y] is RelativSizeDefinition r)
                                rowHeight[y] += (int)Math.Ceiling(missingHeight * r.Size / totalRelativSizeColumns);
                    }
                }

            }

            // Ensure ratio between colums and rows
            var relativeWidthRatio = columns.Zip(columnWidth, (c, w) =>
            {
                if (c is RelativSizeDefinition r)
                    return w / r.Size;
                return 0.0;
            }).Max();
            var relativeHeightRatio = rows.Zip(rowHeight, (c, h) =>
            {
                if (c is RelativSizeDefinition r)
                    return h / r.Size;
                return 0.0;
            }).Max();



            for (int i = 0; i < columns.Count; i++)
                if (columns[i] is RelativSizeDefinition r)
                    columnWidth[i] = (int)Math.Round(r.Size * relativeWidthRatio);

            for (int i = 0; i < rows.Count; i++)
                if (rows[i] is RelativSizeDefinition r)
                    rowHeight[i] = (int)Math.Round(r.Size * relativeHeightRatio);

            // Ensure min max on columns and rows

            for (int i = 0; i < columns.Count; i++)
            {
                if (columns[i].Min.HasValue)
                    columnWidth[i] = Math.Max(columns[i].Min.Value, columnWidth[i]);
                if (rows[i].Max.HasValue)
                    columnWidth[i] = Math.Min(columns[i].Max.Value, columnWidth[i]);
            }

            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Min.HasValue)
                    rowHeight[i] = Math.Max(rows[i].Min.Value, rowHeight[i]);
                if (rows[i].Max.HasValue)
                    rowHeight[i] = Math.Min(rows[i].Max.Value, rowHeight[i]);
            }

            return new Size(columnWidth.Sum(), rowHeight.Sum());
        }

        protected override void ArrangeOverride(Size finalSize)
        {
            //base.ArrangeOverride(finalSize);

            var rows = this.RowDefinitions as IList<ISizeDefinition>;
            var columns = this.ColumnDefinitions as IList<ISizeDefinition>;
            if (rows.Count == 0)
                rows = new ISizeDefinition[] { new RelativSizeDefinition() };
            if (columns.Count == 0)
                columns = new ISizeDefinition[] { new RelativSizeDefinition() };
            int[] columnWidth = new int[columns.Count];
            int[] rowHeight = new int[rows.Count];

            var availableWidth = finalSize.Width;

            for (int i = 0; i < columnWidth.Length; i++)
                if (columns[i] is FixSizeDefinition f)
                    if (availableWidth > f.Size)
                    {
                        columnWidth[i] = f.Size;
                        availableWidth -= f.Size;
                    }
                    else
                    {
                        columnWidth[i] = availableWidth;
                        availableWidth = 0;
                    }



            for (int i = 0; i < columnWidth.Length; i++)
                if (columns[i] is AutoSizeDefinition a)
                {
                    var desiredWidth = Items.Where(x => Column[x].Value == i && ColumnSpan[x].Value == 1).Max(x => x.DesiredSize.Width);
                    if (availableWidth > desiredWidth)
                    {
                        columnWidth[i] = desiredWidth;
                        availableWidth -= desiredWidth;
                    }
                    else
                    {
                        columnWidth[i] = availableWidth;
                        availableWidth = 0;
                    }
                }

            var totalRelativeSize = columns.OfType<RelativSizeDefinition>().Sum(x => x.Size);
            if (totalRelativeSize > 0)
            {
                var widthRatio = availableWidth / totalRelativeSize;
                for (int i = 0; i < columnWidth.Length; i++)
                {
                    if (columns[i] is RelativSizeDefinition r)
                    {
                        var desiredWidth = (int)Math.Ceiling(r.Size * widthRatio);
                        if (availableWidth > desiredWidth)
                        {
                            columnWidth[i] = desiredWidth;
                            availableWidth -= desiredWidth;
                        }
                        else
                        {
                            columnWidth[i] = availableWidth;
                            availableWidth = 0;
                        }
                    }
                }
            }
        }
    }

    public interface ISizeDefinition
    {
        int? Min { get; }
        int? Max { get; }
    }
    public struct AutoSizeDefinition : ISizeDefinition
    {
        public int? Min { get; set; }

        public int? Max { get; set; }
    }
    public struct FixSizeDefinition : ISizeDefinition
    {
        public int? Min { get; set; }

        public int? Max { get; set; }
        public int Size { get; set; }
    }
    public struct RelativSizeDefinition : ISizeDefinition
    {
        public int? Min { get; set; }

        public int? Max { get; set; }
        public double Size { get; set; }

    }

}
