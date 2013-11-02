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
using Windows.Graphics.Display;
using Matrix = SharpDX.Matrix;

namespace RemoteTerminal.Terminals
{
    /// <summary>
    /// Display an overlay text with FPS and ms/frame counters.
    /// </summary>
    class ScreenDisplayRenderer : IDisposable
    {
        private readonly Dictionary<Color, Brush> brushes = new Dictionary<Color, Brush>();

        private readonly ScreenDisplay screenDisplay;
        private readonly IRenderableScreen screen;

        TextFormat textFormatNormal;
        TextFormat textFormatBold;

        private ScreenFontMetrics physicalFontMetrics;

        //TextFormat textFormat;

        /// <summary>
        /// Initializes a new instance of <see cref="FpsRenderer"/> class.
        /// </summary>
        public ScreenDisplayRenderer(ScreenDisplay screenDisplay, IRenderableScreen screen)
        {
            this.screenDisplay = screenDisplay;
            this.screen = screen;
        }

        public virtual void Initialize(DeviceManager deviceManager)
        {
            this.physicalFontMetrics = this.screenDisplay.FontMetrics * (DisplayProperties.LogicalDpi / 96.0f);

            //deviceManager.ContextDirect2D.TextAntialiasMode = TextAntialiasMode.Grayscale;
            deviceManager.ContextDirect2D.AntialiasMode = AntialiasMode.Aliased;
            this.textFormatNormal = new TextFormat(deviceManager.FactoryDirectWrite, this.screenDisplay.ColorTheme.FontFamily, FontWeight.Normal, FontStyle.Normal, this.physicalFontMetrics.FontSize) { TextAlignment = TextAlignment.Leading, ParagraphAlignment = ParagraphAlignment.Near };
            this.textFormatBold = new TextFormat(deviceManager.FactoryDirectWrite, this.screenDisplay.ColorTheme.FontFamily, FontWeight.Bold, FontStyle.Normal, this.physicalFontMetrics.FontSize) { TextAlignment = TextAlignment.Leading, ParagraphAlignment = ParagraphAlignment.Near };
        }

        public virtual void Render(TargetBase target)
        {
            Point drawingPosition = new Point(0, 0);
            SurfaceImageSourceTarget surfaceImageSourceTarget = target as SurfaceImageSourceTarget;
            if (surfaceImageSourceTarget != null)
            {
                drawingPosition = surfaceImageSourceTarget.DrawingPosition;
            }

            IRenderableScreenCopy screenCopy = this.screen.GetScreenCopy();

            var context2D = target.DeviceManager.ContextDirect2D;

            context2D.BeginDraw();
            context2D.Transform = Matrix.Identity;
            context2D.Clear(this.GetColor(ScreenColor.DefaultBackground));

            // 1. Paint backgrounds
            {
                RectangleF rect = new RectangleF();
                var lines = screenCopy.Cells;
                for (int y = 0; y < lines.Length; y++)
                {
                    var cols = lines[y];

                    rect.Top = drawingPosition.Y + (y * this.physicalFontMetrics.CellHeight);
                    rect.Bottom = drawingPosition.Y + (rect.Top + this.physicalFontMetrics.CellHeight);

                    ScreenColor currentBackgroundColor = cols.Length > 0 ? cols[0].BackgroundColor : ScreenColor.DefaultBackground;
                    ScreenColor cellBackgroundColor;
                    int blockStart = 0;
                    for (int x = 0; x <= cols.Length; x++) // loop once above the upper bound
                    {
                        var cell = cols[x < cols.Length ? x : x - 1];

                        bool isCursor = !screenCopy.CursorHidden && y == screenCopy.CursorRow && x == screenCopy.CursorColumn;
                        cellBackgroundColor = isCursor ? ScreenColor.CursorBackground : cell.BackgroundColor;
                        if (cellBackgroundColor != currentBackgroundColor || x == cols.Length)
                        {
                            rect.Left = drawingPosition.X + (blockStart * this.physicalFontMetrics.CellWidth);
                            rect.Right = drawingPosition.X + (x * this.physicalFontMetrics.CellWidth);

                            Brush backgroundBrush = this.GetBrush(context2D, this.GetColor(currentBackgroundColor));
                            if (currentBackgroundColor == ScreenColor.CursorBackground && !screenCopy.HasFocus)
                            {
                                rect.Right = rect.Right - 1.0f;
                                context2D.DrawRectangle(rect, backgroundBrush);
                            }
                            else
                            {
                                context2D.FillRectangle(rect, backgroundBrush);
                            }

                            blockStart = x;

                            currentBackgroundColor = cellBackgroundColor;
                        }
                    }
                }
            }

            // 2. Paint foregrounds
            {
                RectangleF rect = new RectangleF();
                var lines = screenCopy.Cells;
                for (int y = 0; y < lines.Length; y++)
                {
                    var cols = lines[y];

                    rect.Top = drawingPosition.Y + (y * this.physicalFontMetrics.CellHeight);
                    rect.Bottom = drawingPosition.Y + (rect.Top + this.physicalFontMetrics.CellHeight);

                    ScreenColor currentForegroundColor = cols.Length > 0 ? cols[0].ForegroundColor : ScreenColor.DefaultForeground;
                    ScreenCellModifications currentCellModifications = cols.Length > 0 ? cols[0].Modifications : ScreenCellModifications.None;
                    ScreenColor cellForegroundColor;
                    int blockStart = 0;
                    for (int x = 0; x <= cols.Length; x++) // loop once above the upper bound
                    {
                        var cell = cols[x < cols.Length ? x : x - 1];

                        bool isCursor = !screenCopy.CursorHidden && y == screenCopy.CursorRow && x == screenCopy.CursorColumn;
                        cellForegroundColor = isCursor && screenCopy.HasFocus ? ScreenColor.CursorForeground : cell.ForegroundColor;
                        if (cellForegroundColor != currentForegroundColor || cell.Modifications != currentCellModifications || x == cols.Length)
                        {
                            rect.Left = drawingPosition.X + (blockStart * this.physicalFontMetrics.CellWidth);
                            rect.Right = drawingPosition.X + (x * this.physicalFontMetrics.CellWidth);

                            Brush foregroundBrush = this.GetBrush(context2D, this.GetColor(currentForegroundColor));
                            TextFormat textFormat = this.textFormatNormal;
                            if (currentCellModifications.HasFlag(ScreenCellModifications.Bold))
                            {
                                textFormat = this.textFormatBold;
                            }

                            string text = new string(cols.Skip(blockStart).Take(x - blockStart).Select(c => char.IsWhiteSpace(c.Character) ? ' ' : c.Character).ToArray()).TrimEnd();
                            context2D.DrawText(text, textFormat, rect, foregroundBrush, DrawTextOptions.Clip);

                            if (currentCellModifications.HasFlag(ScreenCellModifications.Underline))
                            {
                                var point1 = new Vector2(rect.Left, rect.Bottom - 1.0f) + drawingPosition;
                                var point2 = new Vector2(rect.Right, rect.Bottom - 1.0f) + drawingPosition;
                                context2D.DrawLine(point1, point2, foregroundBrush);
                            }

                            blockStart = x;

                            currentForegroundColor = cellForegroundColor;
                        }
                    }
                }
            }

            context2D.EndDraw();
        }

        private Color GetColor(ScreenColor screenColor)
        {
            var color = this.screenDisplay.ColorTheme.ColorTable[screenColor];
            return new Color(color.R, color.G, color.B, color.A);
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

            if (this.textFormatNormal != null)
            {
                this.textFormatNormal.Dispose();
            }

            if (this.textFormatBold != null)
            {
                this.textFormatBold.Dispose();
            }
        }
    }
}
