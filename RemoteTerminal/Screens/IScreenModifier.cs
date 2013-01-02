using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTerminal.Screens
{
    public interface IScreenModifier : IDisposable
    {
        int CursorRow { get; set; }
        int CursorColumn { get; set; }
        char CursorCharacter { get; set; }
        bool HasFocus { get; set; }

        void ApplyFormatToCursor(ScreenCellFormat format);
        void Erase(int startRow, int startColumn, int endRow, int endColumn, ScreenCellFormat format);
        void CursorRowIncreaseWithScroll(int? scrollTop, int? scrollBottom);
        void CursorRowDecreaseWithScroll(int? scrollTop, int? scrollBottom);
        void ScrollDown(int lines, int? scrollTop, int? scrollBottom);
        void ScrollUp(int lines, int? scrollTop, int? scrollBottom);
        void InsertLines(int lines, int? scrollTop, int? scrollBottom);
        void DeleteLines(int lines, int? scrollTop, int? scrollBottom);
        void InsertCells(int cells);
        void DeleteCells(int cells);
        void Resize(int rows, int columns);
    }
}
