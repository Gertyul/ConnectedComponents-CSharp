namespace ConnectedComponents.Algorithms;

public sealed class SequentialSolver : ISolver
{
    public string Name => "sequential";

    public int[][] FindComponents(Graph.Graph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        int n = graph.NodeCount;
        if (n == 0)
            return Array.Empty<int[]>();

        var visited = new bool[n];
        var components = new List<int[]>();

        var stack = new Stack<int>(capacity: 64);
        var componentBuffer = new List<int>(capacity: 64);

        for (int start = 0; start < n; start++)
        {
            if (visited[start])
                continue;

            componentBuffer.Clear();
            stack.Clear();

            stack.Push(start);
            visited[start] = true;

            while (stack.Count > 0)
            {
                int node = stack.Pop();
                componentBuffer.Add(node);

                var neighbors = graph.Edges[node];
                for (int i = 0; i < neighbors.Count; i++)
                {
                    int neighbor = neighbors[i];
                    if (!visited[neighbor])
                    {
                        visited[neighbor] = true;
                        stack.Push(neighbor);
                    }
                }
            }

            components.Add(componentBuffer.ToArray());
        }

        return components.ToArray();
    }
}
