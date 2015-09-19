namespace RemoteTerminal.Screens
{
    public interface IRenderableScreen
    {
        bool Changed { get; }
        IRenderableScreenCopy GetScreenCopy();
        int RowCount { get; }
        int ColumnCount { get; }
        int ScrollbackRowCount { get; }
        int ScrollbackPosition { get; set; }
    }
}
