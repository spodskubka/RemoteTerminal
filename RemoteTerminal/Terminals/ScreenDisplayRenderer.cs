// Remote Terminal, an SSH/Telnet terminal emulator for Microsoft Windows
// Copyright (C) 2012-2015 Stefan Podskubka
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Draws a screen display using DirectX (SharpDX).
    /// </summary>
    class ScreenDisplayRenderer : IDisposable
    {
        /// <summary>
        /// A cache of brushes (brush creation is expensive so they are cached).
        /// </summary>
        private readonly Dictionary<Color, Brush> brushes = new Dictionary<Color, Brush>();

        /// <summary>
        /// The screen display to draw on.
        /// </summary>
        private readonly ScreenDisplay screenDisplay;

        /// <summary>
        /// The renderable screen that should be drawn.
        /// </summary>
        private readonly IRenderableScreen screen;

        /// <summary>
        /// The text format for normal text.
        /// </summary>
        TextFormat textFormatNormal;

        /// <summary>
        /// The text format for bold text.
        /// </summary>
        TextFormat textFormatBold;

        /// <summary>
        /// The physical font metrics for this renderer (take DPI scaling into account).
        /// </summary>
        private ScreenFontMetrics physicalFontMetrics;

        //TextFormat textFormat;

        /// <summary>
        /// Initializes a new instance of <see cref="ScreenDisplayRenderer"/> class.
        /// </summary>
        public ScreenDisplayRenderer(ScreenDisplay screenDisplay, IRenderableScreen screen)
        {
            this.screenDisplay = screenDisplay;
            this.screen = screen;
        }

        /// <summary>
        /// Initializes the renderer.
        /// </summary>
        /// <param name="deviceManager">The DirectX device manager.</param>
        public virtual void Initialize(DeviceManager deviceManager)
        {
            this.physicalFontMetrics = this.screenDisplay.FontMetrics * (DisplayInformation.GetForCurrentView().LogicalDpi / 96.0f);

            //deviceManager.ContextDirect2D.TextAntialiasMode = TextAntialiasMode.Grayscale;
            deviceManager.ContextDirect2D.AntialiasMode = AntialiasMode.Aliased;
            this.textFormatNormal = new TextFormat(deviceManager.FactoryDirectWrite, this.screenDisplay.ColorTheme.FontFamily, FontWeight.Normal, FontStyle.Normal, this.physicalFontMetrics.FontSize) { TextAlignment = TextAlignment.Leading, ParagraphAlignment = ParagraphAlignment.Near, WordWrapping = WordWrapping.NoWrap };
            this.textFormatBold = new TextFormat(deviceManager.FactoryDirectWrite, this.screenDisplay.ColorTheme.FontFamily, FontWeight.Bold, FontStyle.Normal, this.physicalFontMetrics.FontSize) { TextAlignment = TextAlignment.Leading, ParagraphAlignment = ParagraphAlignment.Near, WordWrapping = WordWrapping.NoWrap };
        }

        /// <summary>
        /// Renders the screen.
        /// </summary>
        /// <param name="target">The Direct2D drawing target.</param>
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
                    rect.Bottom = rect.Top + this.physicalFontMetrics.CellHeight;

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
                    rect.Bottom = rect.Top + this.physicalFontMetrics.CellHeight;

                    ScreenColor currentForegroundColor = cols.Length > 0 ? cols[0].ForegroundColor : ScreenColor.DefaultForeground;
                    ScreenCellModifications currentCellModifications = cols.Length > 0 ? cols[0].Modifications : ScreenCellModifications.None;
                    bool currentCellUCSWIDE = cols.Length > 0 ? cols[0].Character == CjkWidth.UCSWIDE : false;
                    ScreenColor cellForegroundColor;
                    int blockStart = 0;
                    for (int x = 0; x <= cols.Length; x++) // loop once above the upper bound
                    {
                        var cell = cols[x < cols.Length ? x : x - 1];

                        bool isCursor = !screenCopy.CursorHidden && y == screenCopy.CursorRow && x == screenCopy.CursorColumn;
                        cellForegroundColor = isCursor && screenCopy.HasFocus ? ScreenColor.CursorForeground : cell.ForegroundColor;
                        if (currentCellUCSWIDE || cellForegroundColor != currentForegroundColor || cell.Modifications != currentCellModifications || x == cols.Length)
                        {
                            rect.Left = drawingPosition.X + (blockStart * this.physicalFontMetrics.CellWidth);
                            rect.Right = drawingPosition.X + (x * this.physicalFontMetrics.CellWidth);

                            Brush foregroundBrush = this.GetBrush(context2D, this.GetColor(currentForegroundColor));
                            TextFormat textFormat = this.textFormatNormal;
                            if (currentCellModifications.HasFlag(ScreenCellModifications.Bold))
                            {
                                textFormat = this.textFormatBold;
                            }

                            string text = new string(cols.Skip(blockStart).Take(x - blockStart).Select(c => char.IsWhiteSpace(c.Character) ? ' ' : c.Character).Where(ch => ch != CjkWidth.UCSWIDE).ToArray()).TrimEnd();

                            if (text.Length > 0)
                            {
                                context2D.DrawText(text, textFormat, rect, foregroundBrush, DrawTextOptions.Clip);
                            }

                            if (currentCellModifications.HasFlag(ScreenCellModifications.Underline))
                            {
                                var point1 = new Vector2(rect.Left, rect.Bottom - 1.0f) + drawingPosition;
                                var point2 = new Vector2(rect.Right, rect.Bottom - 1.0f) + drawingPosition;
                                context2D.DrawLine(point1, point2, foregroundBrush);
                            }

                            blockStart = x;

                            currentForegroundColor = cellForegroundColor;
                        }
                        currentCellUCSWIDE = cell.Character == CjkWidth.UCSWIDE;
                    }
                }
            }

            context2D.EndDraw();
        }

        /// <summary>
        /// Transforms a <see cref="ScreenColor"/> to the actual <see cref="Color"/> through the theme.
        /// </summary>
        /// <param name="screenColor">The screen color.</param>
        /// <returns>The actual <see cref="Color"/> to use.</returns>
        private Color GetColor(ScreenColor screenColor)
        {
            var color = this.screenDisplay.ColorTheme.ColorTable[screenColor];
            return new Color(color.R, color.G, color.B, color.A);
        }

        /// <summary>
        /// Gets a brush for the specified render target and color.
        /// </summary>
        /// <param name="renderTarget">The render target.</param>
        /// <param name="color">The color.</param>
        /// <returns>The brush (either from the cache or newly created).</returns>
        /// <remarks>
        /// Brushes are cached because their creation is expensive.
        /// </remarks>
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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
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
