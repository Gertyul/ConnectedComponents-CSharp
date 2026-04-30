namespace ConnectedComponents.Algorithms;

public interface ISolver
{
    string Name { get; }
    int[][] FindComponents(Graph.Graph graph);
}

public static class SolverRegistry
{
    public static IReadOnlyList<ISolver> All() =>
    [
        new SequentialSolver(),
        new ParallelSolver(),
    ];
}
