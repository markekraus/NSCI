﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NSCI.UI.Controls
{
    public class TextBlock : Control
    {
        private string text;

        public string Text
        {
            get => this.text; set
            {
                this.text = value?.Replace("\t", "    ").Replace("\r", "");

            }
        }

        private string[] renderLines = new string[0];


        protected override Size MeasureOverride(Size availableSize)
        {
            availableSize = base.MeasureOverride(availableSize);


            var lines = this.text?.Split('\n') ?? new string[0];
            var maxLineLength = lines.Max(x => x.Length);
            int lineheight = lines.Length;
            if (maxLineLength > availableSize.Width)
            {
                maxLineLength = availableSize.Width;
                if (maxLineLength == 0)
                    maxLineLength = 1;
                lineheight = lines.Select(x => Math.Ceiling(x.Length / (double)maxLineLength)).Cast<int>().Sum();
            }

            if (lineheight < (Height ?? 0))
            {
                if (Height <= availableSize.Height)
                    lineheight = Height.Value;
                

            }

            return new Size(maxLineLength, lineheight);
        }

        protected override void ArrangeOverride(Size finalSize)
        {
            base.ArrangeOverride(finalSize);
            var stringbuffer = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                if (i != 0 && i % finalSize.Width == 0 && text[i] != '\n')
                    stringbuffer.AppendLine();
                stringbuffer.Append(text[i]);
            }
            this.renderLines = stringbuffer.ToString().Replace("\r", "").Split('\n');
        }

        protected override void RenderCore(IRenderFrame frame)
        {
            for (int y = 0; y < frame.Height; y++)
                for (int x = 0; x < frame.Width; x++)
                    frame[x, y] = new ColoredKey(y < renderLines.Length && x < renderLines[y].Length ? renderLines[y][x] : ' ', this.ActualForeground, this.ActuellBackground);

        }
    }
}
