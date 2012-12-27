
// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonDX;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using Matrix = SharpDX.Matrix;

namespace RemoteTerminal.Terminals
{
    /// <summary>
    /// Display an overlay text with FPS and ms/frame counters.
    /// </summary>
    public class DrawingTerminalRenderer : IDisposable
    {
        private readonly Dictionary<Color, Brush> brushes = new Dictionary<Color, Brush>();

        DrawingTerminalDisplay display;
        private static readonly Color TerminalBackgroundColor = Color.Black;
        private const string TerminalFontFamily = "Consolas";

        TextFormat textFormatNormal;
        TextFormat textFormatBold;

        private const float CellFontSize = 17.0f;
        private const float CellWidth = 9.0f;
        private const float CellHeight = 20.0f;

        //TextFormat textFormat;

        /// <summary>
        /// Initializes a new instance of <see cref="FpsRenderer"/> class.
        /// </summary>
        public DrawingTerminalRenderer(DrawingTerminalDisplay display)
        {
            Show = true;
            this.display = display;
        }

        public bool Show { get; set; }

        public virtual void Initialize(DeviceManager deviceManager)
        {
            //deviceManager.ContextDirect2D.TextAntialiasMode = TextAntialiasMode.Grayscale;
            deviceManager.ContextDirect2D.AntialiasMode = AntialiasMode.Aliased;
            this.textFormatNormal = new TextFormat(deviceManager.FactoryDirectWrite, TerminalFontFamily, FontWeight.Normal, FontStyle.Normal, CellFontSize) { TextAlignment = TextAlignment.Leading, ParagraphAlignment = ParagraphAlignment.Center };
            this.textFormatBold = new TextFormat(deviceManager.FactoryDirectWrite, TerminalFontFamily, FontWeight.Bold, FontStyle.Normal, CellFontSize) { TextAlignment = TextAlignment.Leading, ParagraphAlignment = ParagraphAlignment.Center };
        }

        public virtual void Render(TargetBase target)
        {
            if (!Show)
                return;

            DrawingTerminalCell[][] displayClone;
            int cursorRowClone;
            int cursorColumnClone;
            bool cursorHiddenClone;
            bool hasFocusClone;

            lock (this.display.ChangeLock)
            {
                if (!this.display.Changed)
                {
                    return;
                }

                displayClone = this.display.Lines.Select(l => l.Cells.Select(c => c.Clone()).ToArray()).ToArray();
                cursorRowClone = this.display.CursorRow;
                cursorColumnClone = this.display.CursorColumn;
                cursorHiddenClone = this.display.CursorHidden;
                hasFocusClone = this.display.HasFocus;

                this.display.Changed = false;
            }

            //lock (this.display.ChangeLock)
            //{
            //    var display = this.display;
            //    var cursorRow = this.display.CursorRow;
            //    var cursorColumn = this.display.CursorColumn;
            //    var cursorHidden = this.display.CursorHidden;
            //    var hasFocus = this.display.HasFocus;

            var display = displayClone;
            var cursorRow = cursorRowClone;
            var cursorColumn = cursorColumnClone;
            var cursorHidden = cursorHiddenClone;
            var hasFocus = hasFocusClone;

            var context2D = target.DeviceManager.ContextDirect2D;

            context2D.BeginDraw();
            context2D.Transform = Matrix.Identity;
            context2D.Clear(TerminalBackgroundColor);

            RectangleF rect = new RectangleF();
            var lines = display;
            for (int y = 0; y < lines.Count(); y++)
            {
                var cols = lines[y];
                rect.Top = y * CellHeight;
                rect.Bottom = rect.Top + CellHeight;
                for (int x = 0; x < cols.Count(); x++)
                {
                    var cell = cols[x];
                    rect.Left = x * CellWidth;
                    rect.Right = rect.Left + CellWidth;

                    bool isCursor = !cursorHidden && y == cursorRow && x == cursorColumn;
                    this.DrawCell(target, rect, cell, isCursor, hasFocus);
                }
            }

            context2D.EndDraw();
            //}
        }

        private void DrawCell(TargetBase target, RectangleF rect, DrawingTerminalCell cell, bool isCursor, bool hasFocus)
        {
            var context2D = target.DeviceManager.ContextDirect2D;

            // 1. Paint background
            {
                Color backgroundColor;
                if (isCursor && hasFocus)
                {
                    var color = DrawingTerminalDisplay.CursorFormat.BackgroundColor;
                    backgroundColor = new Color(color.R, color.G, color.B, color.A);
                }
                else
                {
                    var color = cell.BackgroundColor;
                    backgroundColor = new Color(color.R, color.G, color.B, color.A);
                }

                if (backgroundColor != TerminalBackgroundColor)
                {
                    Brush backgroundBrush = GetBrush(context2D, backgroundColor);
                    context2D.FillRectangle(rect, backgroundBrush);
                }
            }

            // 2. Paint border
            {
                if (isCursor && !hasFocus)
                {
                    var color = DrawingTerminalDisplay.CursorFormat.BackgroundColor;
                    Color borderColor = new Color(color.R, color.G, color.B, color.A);
                    Brush borderBrush = GetBrush(context2D, borderColor);
                    context2D.DrawRectangle(rect, borderBrush);
                }
            }

            // 3. Paint foreground (character)
            {
                Color foregroundColor;
                if (isCursor && hasFocus)
                {
                    foregroundColor = Color.Black;
                }
                else
                {
                    var color = cell.ForegroundColor;
                    foregroundColor = new Color(color.R, color.G, color.B, color.A);
                }

                var foregroundBrush = GetBrush(context2D, foregroundColor);
                TextFormat textFormat = this.textFormatNormal;
                if (cell.Modifications.HasFlag(DrawingTerminalCellModifications.Bold))
                {
                    textFormat = this.textFormatBold;
                }

                context2D.DrawText(cell.Character.ToString(), textFormat, rect, foregroundBrush, DrawTextOptions.Clip);

                if (cell.Modifications.HasFlag(DrawingTerminalCellModifications.Underline))
                {
                    var point1 = new DrawingPointF(rect.Left, rect.Bottom - 1.0f);
                    var point2 = new DrawingPointF(rect.Right, rect.Bottom - 1.0f);
                    context2D.DrawLine(point1, point2, foregroundBrush);
                }
            }
        }

        private Brush GetBrush(RenderTarget renderTarget, Color color)
        {
            Brush brush;

            lock (this.brushes)
            {
                if (!this.brushes.TryGetValue(color, out brush))
                {
                    brush = new SolidColorBrush(renderTarget, color);
                    this.brushes[color] = brush;
                }
            }

            return brush;
        }

        public void Dispose()
        {
            foreach (var brush in this.brushes.Values)
            {
                brush.Dispose();
            }
        }
    }
}
