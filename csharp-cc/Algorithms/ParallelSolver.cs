using System.Collections.Concurrent;

namespace ConnectedComponents.Algorithms;

public sealed class ParallelSolver : ISolver
{
    public string Name => "parallel";

    public int Workers { get; }

    public ParallelSolver() : this(0) { }

    public ParallelSolver(int workers)
    {
        Workers = workers;
    }

    public int[][] FindComponents(Graph.Graph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        int n = graph.NodeCount;
        if (n == 0)
            return Array.Empty<int[]>();

        int workers = Workers > 0 ? Workers : Environment.ProcessorCount;

        var uf = new LockFreeUnionFind(n);

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = workers,
        };

        int chunkSize = Math.Max(1, (n + workers - 1) / workers);
        var partitioner = Partitioner.Create(0, n, chunkSize);

        Parallel.ForEach(partitioner, parallelOptions, range =>
        {
            for (int u = range.Item1; u < range.Item2; u++)
            {
                var neighbors = graph.Edges[u];
                for (int i = 0; i < neighbors.Count; i++)
                {
                    int v = neighbors[i];
                    uf.Union(u, v);
                }
            }
        });

        return CollectComponents(uf, n);
    }

    private static int[][] CollectComponents(LockFreeUnionFind uf, int n)
    {
        var rootToIdx = new Dictionary<int, int>(capacity: Math.Max(16, n / 4));
        var components = new List<List<int>>();

        for (int i = 0; i < n; i++)
        {
            int root = uf.Find(i);
            if (!rootToIdx.TryGetValue(root, out int idx))
            {
                idx = components.Count;
                rootToIdx[root] = idx;
                components.Add(new List<int>());
            }
            components[idx].Add(i);
        }

        var result = new int[components.Count][];
        for (int i = 0; i < components.Count; i++)
        {
            result[i] = components[i].ToArray();
        }
        return result;
    }
}
