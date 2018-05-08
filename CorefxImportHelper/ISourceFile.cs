namespace CorefxImportHelper
{
    public interface ISourceFile
    {
        string AbsolutePath { get; }
        string MonoPath { get; }
    }
}