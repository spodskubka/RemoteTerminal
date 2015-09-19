namespace RemoteTerminal.Screens
{
    public interface IWritableScreen : IRenderableScreen
    {
        IScreenModifier GetModifier();
        int CursorRow { get; }
        int CursorColumn { get; }
    }
}
