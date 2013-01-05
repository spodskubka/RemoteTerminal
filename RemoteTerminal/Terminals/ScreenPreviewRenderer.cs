using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonDX;
using RemoteTerminal.Screens;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using Matrix = SharpDX.Matrix;

namespace RemoteTerminal.Terminals
{
    /// <summary>
    /// Display an overlay text with FPS and ms/frame counters.
    /// </summary>
    class ScreenPreviewRenderer : IDisposable
    {
        private readonly Dictionary<Color, Brush> brushes = new Dictionary<Color, Brush>();

        ScreenPreview screenPreview;
        IRenderableScreen screen;

        private static readonly Color TerminalBackgroundColor = Color.Black;
        private const string TerminalFontFamily = "Consolas";

        TextFormat textFormatNormal;
        TextFormat textFormatBold;

        private const float CellFontSize = 17.0f / 5f;
        private const float CellWidth = 9.0f / 5f;
        private const float CellHeight = 20.0f / 5f;

        //TextFormat textFormat;

        /// <summary>
        /// Initializes a new instance of <see cref="FpsRenderer"/> class.
        /// </summary>
        public ScreenPreviewRenderer(ScreenPreview screenPreview, IRenderableScreen screen)
        {
            Show = true;
            this.screenPreview = screenPreview;
            this.screen = screen;
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

            IRenderableScreenCopy screenCopy = this.screen.GetScreenCopy();

            var context2D = target.DeviceManager.ContextDirect2D;

            context2D.BeginDraw();
            context2D.Transform = Matrix.Identity;
            context2D.Clear(TerminalBackgroundColor);

            RectangleF rect = new RectangleF();
            var lines = screenCopy.Cells;
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

                    bool isCursor = !screenCopy.CursorHidden && y == screenCopy.CursorRow && x == screenCopy.CursorColumn;
                    this.DrawCell(target, rect, cell, isCursor, screenCopy.HasFocus);
                }
            }

            context2D.EndDraw();
        }

        private void DrawCell(TargetBase target, RectangleF rect, IRenderableScreenCell cell, bool isCursor, bool hasFocus)
        {
            var context2D = target.DeviceManager.ContextDirect2D;

            // 1. Paint background
            {
                Color backgroundColor;
                if (isCursor && hasFocus)
                {
                    var color = this.screenPreview.CursorBackgroundColor;
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
                    var color = this.screenPreview.CursorBackgroundColor;
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

                if (cell.Character != ' ')
                {
                    TextFormat textFormat = this.textFormatNormal;
                    if (cell.Modifications.HasFlag(ScreenCellModifications.Bold))
                    {
                        textFormat = this.textFormatBold;
                    }

                    context2D.DrawText(cell.Character.ToString(), textFormat, rect, foregroundBrush, DrawTextOptions.Clip);
                }

                if (cell.Modifications.HasFlag(ScreenCellModifications.Underline))
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
