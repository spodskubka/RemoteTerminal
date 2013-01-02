using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Windows.UI;
using Windows.UI.Xaml.Controls;

namespace RemoteTerminal.Screens
{
    public interface IRenderableScreenCopy
    {
        /// <summary>
        /// Gets the cells that the terminal screen is composed of.
        /// </summary>
        IRenderableScreenCell[][] Cells { get; }

        /// <summary>
        /// Gets the row position of the cursor (zero-based).
        /// </summary>
        int CursorRow { get; }

        /// <summary>
        /// Gets the column position of the cursor (zero-based).
        /// </summary>
        int CursorColumn { get; }

        /// <summary>
        /// Gets a value indicating whether the cursor should be hidden.
        /// </summary>
        bool CursorHidden { get; }

        /// <summary>
        /// Gets a value indicating whether the screen has focus.
        /// </summary>
        bool HasFocus { get; }
    }
}
