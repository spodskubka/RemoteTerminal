namespace RemoteTerminal.Screens
{
    public interface IRenderableScreenCell
    {
        char Character { get; }
        ScreenCellModifications Modifications { get; }
        ScreenColor ForegroundColor { get; }
        ScreenColor BackgroundColor { get; }
    }
}
