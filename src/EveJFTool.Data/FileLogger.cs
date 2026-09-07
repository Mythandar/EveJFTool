using EveJFTool.Core.Interfaces;

namespace EveJFTool.Data;

public sealed class FileLogger : IAppLogger
{
    private readonly string _path;
    private readonly object _sync = new();

    public FileLogger(string path)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    }

    public void Info(string message) => Write("INFO", message, null);

    public void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    private void Write(string level, string message, Exception? exception)
    {
        var line = $"{DateTimeOffset.Now:O} [{level}] {message}";
        if (exception is not null)
        {
            line += Environment.NewLine + exception;
        }

        lock (_sync)
        {
            File.AppendAllText(_path, line + Environment.NewLine);
        }
    }
}
