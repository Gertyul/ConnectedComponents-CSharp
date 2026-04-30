using System.Globalization;

namespace ConnectedComponents.Graph;

public sealed class Graph
{
    public int NodeCount { get; }
    public List<int>[] Edges { get; }

    public Graph(int nodeCount)
    {
        if (nodeCount < 0)
            throw new ArgumentOutOfRangeException(nameof(nodeCount), "Кількість вершин не може бути від'ємною.");

        NodeCount = nodeCount;
        Edges = new List<int>[nodeCount];
        for (int i = 0; i < nodeCount; i++)
        {
            Edges[i] = new List<int>();
        }
    }

    public void AddEdge(int u, int v)
    {
        if ((uint)u >= (uint)NodeCount)
            throw new ArgumentOutOfRangeException(nameof(u));
        if ((uint)v >= (uint)NodeCount)
            throw new ArgumentOutOfRangeException(nameof(v));

        Edges[u].Add(v);
        if (u != v)
        {
            Edges[v].Add(u);
        }
    }

    public static Graph LoadFromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Шлях до файлу не може бути порожнім.", nameof(path));
        if (!File.Exists(path))
            throw new FileNotFoundException($"Файл графу не знайдено: {path}", path);

        using var reader = new StreamReader(path);

        string? header = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(header))
            throw new InvalidDataException("Файл графу порожній або не містить заголовку.");

        var headerParts = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (headerParts.Length < 2)
            throw new InvalidDataException("Невірний формат заголовку. Очікується: 'N M'.");

        if (!int.TryParse(headerParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ||
            !int.TryParse(headerParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            throw new InvalidDataException("Заголовок повинен містити два цілих числа.");
        }

        var graph = new Graph(n);

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                continue;

            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int u) ||
                !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
            {
                continue;
            }

            if ((uint)u >= (uint)n || (uint)v >= (uint)n)
                continue;

            graph.AddEdge(u, v);
        }

        return graph;
    }

    public static void GenerateAndSave(int nodes, int edges, string outputPath, int seed = 42)
    {
        var rng = new Random(seed);
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var writer = new StreamWriter(outputPath);
        writer.WriteLine($"{nodes} {edges}");
        for (int i = 0; i < edges; i++)
        {
            int u = rng.Next(nodes);
            int v = rng.Next(nodes);
            writer.WriteLine($"{u} {v}");
        }
    }
}
