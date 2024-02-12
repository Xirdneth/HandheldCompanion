namespace HandheldCompanion.UI
{
    public interface IUISounds
    {
        string Expanded { get; }
        string Collapse { get; }
        void PlayOggFile(string fileName);
    }
}