using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using System.Linq;
using System.Collections;

namespace RemoteTerminal.Screens
{
    public class ScreenLine : List<ScreenCell>
    {
        public ScreenLine(int columns)
            : base(columns)
        {
            for (int i = 0; i < columns; i++)
            {
                this.Add(new ScreenCell());
            }
        }
    }
}
