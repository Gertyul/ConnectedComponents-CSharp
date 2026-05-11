namespace ConnectedComponents.Algorithms;

public sealed class SequentialSolver : ISolver
{
    public string Name => "sequential";

    public int[][] FindComponents(Graph.Graph graph)
    {
        int n = graph.NodeCount;
        if (n == 0) return Array.Empty<int[]>();

        var uf = new SequentialUnionFind(n);
        
        for (int u = 0; u < n; u++)
        {
            var neighbors = graph.Edges[u];
            for (int i = 0; i < neighbors.Count; i++)
                uf.Union(u, neighbors[i]);
        }

        return CollectComponents(uf, n);
    }

    private static int[][] CollectComponents(SequentialUnionFind uf, int n)
    {
        var rootToIdx = new Dictionary<int, int>();
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
            result[i] = components[i].ToArray();
        return result;
    }
}