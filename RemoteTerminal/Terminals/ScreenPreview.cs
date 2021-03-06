﻿// Remote Terminal, an SSH/Telnet terminal emulator for Microsoft Windows
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
using RemoteTerminal.Model;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace RemoteTerminal.Terminals
{
    /// <summary>
    /// This is a XAML user control for displaying a small terminal preview (in the terminal switcher AppBar).
    /// </summary>
    /// <remarks>
    /// This class is meant to be similar to the <see cref="ScreenDisplay"/> class.
    /// It should display a small representation of a terminal screen.
    /// However, due to a non-working implementation it isn't used at the moment.
    /// </remarks>
    public sealed class ScreenPreview : UserControl, IDisposable
    {
        private ITerminal terminal = null;

        private readonly Rectangle rectangle;
        private readonly Border border;

        // DirectX stuff
        //private readonly DeviceManager deviceManager;
        //private ScreenPreviewRenderer terminalRenderer = null;
        //private SurfaceImageSourceTarget d2dTarget = null;
        //private bool firstRender = true;

        public ScreenPreview()
        {
            this.rectangle = new Rectangle();
            this.border = new Border()
            {
                Child = this.rectangle
            };

            this.border.BorderBrush = new SolidColorBrush(Colors.Gray);
            this.border.BorderThickness = new Thickness(2d);
            this.border.Background = new SolidColorBrush(Colors.Black);

            this.Content = border;

            this.IsTabStop = true;
            this.IsTapEnabled = true;

            //this.deviceManager = new DeviceManager();
        }

        public static double TerminalCellFontSize { get { return 17d; } }
        public static double TerminalCellWidth { get { return 9d; } }
        public static double TerminalCellHeight { get { return 20d; } }

        public ColorThemeData ColorTheme { get; set; }

        public ITerminal Terminal
        {
            get
            {
                return this.terminal;
            }

            set
            {
                if (this.terminal != null)
                {
                    this.DetachRenderer();
                    this.terminal.Disconnected -= terminal_Disconnected;
                    this.terminal = null;
                }

                this.terminal = value;

                if (this.terminal == null)
                {
                    return;
                }

                this.terminal.Disconnected += terminal_Disconnected;

                // Preview terminals have always focus.
                this.terminal.ScreenHasFocus = true;

                this.border.BorderBrush = new SolidColorBrush(this.terminal.IsConnected ? Colors.Gray : Colors.Red);

                // This will result in ArrangeOverride being called, where the new renderer is attached.
                this.InvalidateArrange();
            }
        }

        async void terminal_Disconnected(object sender, EventArgs e)
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                this.border.BorderBrush = new SolidColorBrush(Colors.Red);
            });
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (this.terminal == null)
            {
                return availableSize;// new Size(180d, 150d);
            }

            Size desiredSize = new Size();
            desiredSize.Width = (this.terminal.RenderableScreen.ColumnCount * TerminalCellWidth) + this.border.BorderThickness.Left + this.border.BorderThickness.Right;
            desiredSize.Width = (this.terminal.RenderableScreen.RowCount * TerminalCellHeight) + this.border.BorderThickness.Top + this.border.BorderThickness.Bottom;
            return desiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            //if (this.terminal == null)
            //{
            //    return base.ArrangeOverride(finalSize);
            //}

            //double terminalRectangleWidth = finalSize.Width - this.border.BorderThickness.Left - this.border.BorderThickness.Right;
            //double terminalRectangleHeight = finalSize.Height - this.border.BorderThickness.Top - this.border.BorderThickness.Bottom;

            //this.DetachRenderer();

            //int rows = (int)(terminalRectangleHeight / ScreenPreview.TerminalCellHeight);
            //int columns = (int)(terminalRectangleWidth / ScreenPreview.TerminalCellWidth);
            //this.terminal.ResizeScreen(rows, columns);

            //int pixelWidth = (int)(terminalRectangleWidth * DisplayProperties.LogicalDpi / 96.0);
            //int pixelHeight = (int)(terminalRectangleHeight * DisplayProperties.LogicalDpi / 96.0);
            //this.AttachRenderer(pixelWidth, pixelHeight);

            return base.ArrangeOverride(finalSize);
        }

        void CompositionTarget_Rendering(object sender, object e)
        {
            //if (!this.terminal.RenderableScreen.Changed && !this.firstRender)
            //{
            //    return;
            //}

            //lock (this.deviceManager)
            //{
            //    if (this.d2dTarget == null)
            //    {
            //        return;
            //    }

            //    this.d2dTarget.RenderAll();
            //    this.firstRender = false;
            //}
        }

        private void AttachRenderer(int pixelWidth, int pixelHeight)
        {
            //lock (this.deviceManager)
            //{
            //    if (this.terminalRenderer != null)
            //    {
            //        throw new InvalidOperationException("Renderer already attached.");
            //    }

            //    this.terminalRenderer = new ScreenPreviewRenderer(this, this.terminal.RenderableScreen);
            //    this.d2dTarget = new SurfaceImageSourceTarget(pixelWidth, pixelHeight);
            //    this.firstRender = true;

            //    this.deviceManager.OnInitialize += this.d2dTarget.Initialize;
            //    this.deviceManager.OnInitialize += this.terminalRenderer.Initialize;
            //    this.deviceManager.Initialize(DisplayProperties.LogicalDpi);

            //    this.rectangle.Fill = new ImageBrush() { ImageSource = this.d2dTarget.ImageSource };
            //    this.d2dTarget.OnRender += terminalRenderer.Render;
            //    CompositionTarget.Rendering += CompositionTarget_Rendering;
            //}
        }

        private void DetachRenderer()
        {
            //lock (this.deviceManager)
            //{
            //    if (this.terminalRenderer == null)
            //    {
            //        return;
            //    }

            //    CompositionTarget.Rendering -= CompositionTarget_Rendering;
            //    this.d2dTarget.OnRender -= terminalRenderer.Render;
            //    this.rectangle.Fill = null;

            //    this.deviceManager.OnInitialize -= this.d2dTarget.Initialize;
            //    this.deviceManager.OnInitialize -= this.terminalRenderer.Initialize;

            //    this.d2dTarget.Dispose();
            //    this.d2dTarget = null;

            //    this.terminalRenderer.Dispose();
            //    this.terminalRenderer = null;
            //}
        }

        public void Dispose()
        {
            this.DetachRenderer();
            if (this.terminal != null)
            {
                this.terminal.Disconnected -= terminal_Disconnected;
            }

            //this.deviceManager.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
