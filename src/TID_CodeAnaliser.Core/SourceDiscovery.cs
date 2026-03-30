namespace TID_CodeAnaliser.Core;

public static class SourceDiscovery
{
    public static IReadOnlyList<SourceFile> Discover(string rootPath, AnalysisOptions options)
    {
        var files = new List<SourceFile>();

        foreach (var file in Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories))
        {
            if (!options.IncludedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (IsExcluded(file, rootPath, options))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            files.Add(new SourceFile
            {
                FilePath = file,
                RelativePath = Path.GetRelativePath(rootPath, file),
                Content = content,
                Lines = File.ReadAllLines(file)
            });
        }

        return files;
    }

    private static bool IsExcluded(string filePath, string rootPath, AnalysisOptions options)
    {
        var relative = Path.GetRelativePath(rootPath, filePath);
        var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(segment => options.ExcludedDirectories.Contains(segment, StringComparer.OrdinalIgnoreCase));
    }
}
