using ConnectedComponents.Algorithms;

namespace ConnectedComponents.Tests;

public static class TestRunner
{
    public static int RunAll()
    {
        var tests = new (string Name, Action Body)[]
        {
            ("Triangle + IsolatedNode", TestTrianglePlusIsolated),
            ("Empty graph", TestEmptyGraph),
            ("Single node", TestSingleNode),
            ("All disconnected", TestAllDisconnected),
            ("Fully connected (K4)", TestFullyConnected),
            ("Chain of 5", TestChain),
            ("Three triangles", TestThreeTriangles),
            ("Self-loop", TestSelfLoop),
            ("Stress: 5K nodes / 10K edges (Sequential vs Parallel agree)", TestStressLarge),
        };

        Console.WriteLine();
        Console.WriteLine("─────────── ТЕСТИ КОРЕКТНОСТІ ───────────");
        Console.WriteLine();

        int passed = 0, failed = 0;
        foreach (var (name, body) in tests)
        {
            try
            {
                body();
                Console.WriteLine($"  [PASS]  {name}");
                passed++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [FAIL]  {name}");
                Console.WriteLine($"          → {ex.Message}");
                failed++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"  Підсумок: {passed} passed, {failed} failed");
        Console.WriteLine();

        return failed;
    }

    private static IEnumerable<ISolver> AllSolvers()
    {
        yield return new SequentialSolver();
        yield return new ParallelSolver(workers: 1);
        yield return new ParallelSolver(workers: 2);
        yield return new ParallelSolver(workers: 4);
        yield return new ParallelSolver(workers: 8);
    }

    private static int[][] Normalize(int[][] components)
    {
        var copy = components.Select(c =>
        {
            var arr = c.ToArray();
            Array.Sort(arr);
            return arr;
        }).ToArray();

        Array.Sort(copy, (a, b) =>
        {
            if (a.Length == 0 && b.Length == 0) return 0;
            if (a.Length == 0) return -1;
            if (b.Length == 0) return 1;
            return a[0].CompareTo(b[0]);
        });
        return copy;
    }

    private static bool ComponentsEqual(int[][] a, int[][] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i].Length != b[i].Length) return false;
            for (int j = 0; j < a[i].Length; j++)
            {
                if (a[i][j] != b[i][j]) return false;
            }
        }
        return true;
    }

    private static void AssertCorrect(Graph.Graph g, int[][] expectedRaw)
    {
        var expected = Normalize(expectedRaw);
        foreach (var solver in AllSolvers())
        {
            var actual = Normalize(solver.FindComponents(g));
            if (!ComponentsEqual(actual, expected))
            {
                throw new InvalidOperationException(
                    $"Алгоритм {solver.Name} повернув некоректний результат " +
                    $"(очікувалося {expected.Length} компонент, отримано {actual.Length}).");
            }
        }
    }

    private static void TestTrianglePlusIsolated()
    {
        var g = new Graph.Graph(4);
        g.AddEdge(0, 1);
        g.AddEdge(1, 2);
        g.AddEdge(0, 2);
        AssertCorrect(g, new[]
        {
            new[] { 0, 1, 2 },
            new[] { 3 },
        });
    }

    private static void TestEmptyGraph()
    {
        var g = new Graph.Graph(0);
        foreach (var solver in AllSolvers())
        {
            var actual = solver.FindComponents(g);
            if (actual.Length != 0)
                throw new InvalidOperationException($"{solver.Name}: очікувався порожній результат.");
        }
    }

    private static void TestSingleNode()
    {
        var g = new Graph.Graph(1);
        AssertCorrect(g, new[] { new[] { 0 } });
    }

    private static void TestAllDisconnected()
    {
        var g = new Graph.Graph(5);
        AssertCorrect(g, new[]
        {
            new[] { 0 }, new[] { 1 }, new[] { 2 }, new[] { 3 }, new[] { 4 },
        });
    }

    private static void TestFullyConnected()
    {
        var g = new Graph.Graph(4);
        for (int i = 0; i < 4; i++)
        {
            for (int j = i + 1; j < 4; j++)
            {
                g.AddEdge(i, j);
            }
        }
        AssertCorrect(g, new[] { new[] { 0, 1, 2, 3 } });
    }

    private static void TestChain()
    {
        var g = new Graph.Graph(5);
        g.AddEdge(0, 1);
        g.AddEdge(1, 2);
        g.AddEdge(2, 3);
        g.AddEdge(3, 4);
        AssertCorrect(g, new[] { new[] { 0, 1, 2, 3, 4 } });
    }

    private static void TestThreeTriangles()
    {
        var g = new Graph.Graph(9);
        g.AddEdge(0, 1); g.AddEdge(1, 2); g.AddEdge(0, 2);
        g.AddEdge(3, 4); g.AddEdge(4, 5); g.AddEdge(3, 5);
        g.AddEdge(6, 7); g.AddEdge(7, 8); g.AddEdge(6, 8);
        AssertCorrect(g, new[]
        {
            new[] { 0, 1, 2 },
            new[] { 3, 4, 5 },
            new[] { 6, 7, 8 },
        });
    }

    private static void TestSelfLoop()
    {
        var g = new Graph.Graph(3);
        g.AddEdge(0, 0);
        g.AddEdge(1, 2);
        AssertCorrect(g, new[]
        {
            new[] { 0 },
            new[] { 1, 2 },
        });
    }

    private static void TestStressLarge()
    {
        var rng = new Random(42);
        const int n = 5_000;
        const int edges = 10_000;

        var g = new Graph.Graph(n);
        for (int i = 0; i < edges; i++)
        {
            int u = rng.Next(n);
            int v = rng.Next(n);
            g.AddEdge(u, v);
        }

        var seq = new SequentialSolver();
        var expected = Normalize(seq.FindComponents(g));

        foreach (int workers in new[] { 1, 2, 4, 8, 16 })
        {
            var par = new ParallelSolver(workers);
            var actual = Normalize(par.FindComponents(g));
            if (!ComponentsEqual(actual, expected))
            {
                throw new InvalidOperationException(
                    $"ParallelSolver(workers={workers}) розійшовся з SequentialSolver.");
            }
        }
    }
}
