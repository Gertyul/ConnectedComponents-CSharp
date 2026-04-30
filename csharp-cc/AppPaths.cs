namespace ConnectedComponents;

/// <summary>
/// Утиліти для роботи з шляхами файлів програми.
/// </summary>
internal static class AppPaths
{
    public static string FindOrCreateTestdataDir()
    {
        var candidates = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory,
        };

        foreach (var startDir in candidates)
        {
            var dir = new DirectoryInfo(startDir);
            for (int i = 0; i < 8 && dir is not null; i++, dir = dir.Parent)
            {
                string candidate = Path.Combine(dir.FullName, "testdata");
                if (Directory.Exists(candidate))
                    return candidate;
            }
        }

        string newPath = Path.Combine(Directory.GetCurrentDirectory(), "testdata");
        Directory.CreateDirectory(newPath);
        return newPath;
    }
}
