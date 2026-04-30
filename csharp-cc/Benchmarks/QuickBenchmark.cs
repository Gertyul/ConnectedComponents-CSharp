using System.Diagnostics;
using ConnectedComponents.Algorithms;

namespace ConnectedComponents.Benchmarks;

public static class QuickBenchmark
{
    private static readonly (string Name, int Nodes, int Edges)[] Datasets =
    {
        ("sparse", 100_000,   200_000),
        ("medium", 100_000,   2_000_000),
        ("dense",  100_000,   10_000_000),
        ("large",  1_000_000, 5_000_000),
    };

    private static readonly int[] WorkerCounts = { 1, 2, 4, 8, 16, 32 };

    private const int WarmupIterations = 2;
    private const int MeasureIterations = 5;

    public static void Run()
    {
        Console.WriteLine();
        Console.WriteLine("─────────── ШВИДКИЙ БЕНЧМАРК ───────────");
        Console.WriteLine();
        Console.WriteLine($"  CPU cores:  {Environment.ProcessorCount}");
        Console.WriteLine($"  Warmup:     {WarmupIterations} ітерацій");
        Console.WriteLine($"  Measure:    {MeasureIterations} ітерацій (береться середнє)");
        Console.WriteLine();

        var sequentialTimes = new double[Datasets.Length];
        var parallelTimes = new double[Datasets.Length, WorkerCounts.Length];

        for (int di = 0; di < Datasets.Length; di++)
        {
            var (name, nodes, edges) = Datasets[di];

            Console.Write($"  Підготовка датасету {name} ({nodes} вершин, {edges} ребер)... ");
            var graph = GetOrCreateGraph(name, nodes, edges);
            Console.WriteLine("готово.");

            Console.Write($"    sequential... ");
            sequentialTimes[di] = Measure(() => new SequentialSolver().FindComponents(graph));
            Console.WriteLine($"{sequentialTimes[di]:F2} мс");

            for (int wi = 0; wi < WorkerCounts.Length; wi++)
            {
                int workers = WorkerCounts[wi];
                Console.Write($"    parallel (workers={workers,2})... ");
                parallelTimes[di, wi] = Measure(() => new ParallelSolver(workers).FindComponents(graph));
                double speedup = sequentialTimes[di] / parallelTimes[di, wi];
                Console.WriteLine($"{parallelTimes[di, wi]:F2} мс  (прискорення: {speedup:F2}x)");
            }

            Console.WriteLine();
        }

        PrintMarkdownTables(sequentialTimes, parallelTimes);

        SaveCsv(sequentialTimes, parallelTimes);
    }

    private static void SaveCsv(double[] seq, double[,] par)
    {
        string testdataDir = AppPaths.FindOrCreateTestdataDir();
        string outDir = Path.GetDirectoryName(testdataDir) ?? Directory.GetCurrentDirectory();
        string csvPath = Path.Combine(outDir, "benchmark_results.csv");

        var ci = System.Globalization.CultureInfo.InvariantCulture;

        using var writer = new StreamWriter(csvPath);

        writer.Write("dataset,nodes,edges,sequential_ms");
        foreach (int w in WorkerCounts)
            writer.Write($",parallel_{w}_ms,speedup_{w}");
        writer.WriteLine();

        for (int di = 0; di < Datasets.Length; di++)
        {
            var (name, nodes, edges) = Datasets[di];
            writer.Write($"{name},{nodes},{edges},{seq[di].ToString("F4", ci)}");
            for (int wi = 0; wi < WorkerCounts.Length; wi++)
            {
                double t = par[di, wi];
                double sp = seq[di] / t;
                writer.Write($",{t.ToString("F4", ci)},{sp.ToString("F4", ci)}");
            }
            writer.WriteLine();
        }

        Console.WriteLine();
        Console.WriteLine($"  CSV з результатами збережено в: {csvPath}");
        Console.WriteLine($"  (можна побудувати графіки через plot_results.py)");
    }

    private static Graph.Graph GetOrCreateGraph(string name, int nodes, int edges)
    {
        string testdataDir = AppPaths.FindOrCreateTestdataDir();
        string path = Path.Combine(testdataDir, $"{name}.txt");

        bool needRegenerate = true;
        if (File.Exists(path))
        {
            try
            {
                using var reader = new StreamReader(path);
                string? header = reader.ReadLine();
                if (!string.IsNullOrWhiteSpace(header))
                {
                    var parts = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 &&
                        int.TryParse(parts[0], out int existingN) &&
                        int.TryParse(parts[1], out int existingM) &&
                        existingN == nodes && existingM == edges)
                    {
                        needRegenerate = false;
                    }
                }
            }
            catch { }
        }

        if (needRegenerate)
        {
            Graph.Graph.GenerateAndSave(nodes, edges, path);
        }

        return Graph.Graph.LoadFromFile(path);
    }

    private static double Measure(Action action)
    {
        for (int i = 0; i < WarmupIterations; i++)
        {
            action();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < MeasureIterations; i++)
        {
            action();
        }
        sw.Stop();

        return sw.Elapsed.TotalMilliseconds / MeasureIterations;
    }

    private static void PrintMarkdownTables(double[] seq, double[,] par)
    {
        Console.WriteLine();
        Console.WriteLine("═══════════════ ТАБЛИЦІ ДЛЯ КУРСОВОЇ ═══════════════");
        Console.WriteLine();

        Console.WriteLine("Таблиця: Час виконання (мс)");
        Console.WriteLine();
        Console.Write("| Датасет | Sequential ");
        foreach (int w in WorkerCounts) Console.Write($"| Parallel ({w}) ");
        Console.WriteLine("|");

        Console.Write("|---|---");
        foreach (var _ in WorkerCounts) Console.Write("|---");
        Console.WriteLine("|");

        for (int di = 0; di < Datasets.Length; di++)
        {
            Console.Write($"| {Datasets[di].Name} | {seq[di]:F2} ");
            for (int wi = 0; wi < WorkerCounts.Length; wi++)
            {
                Console.Write($"| {par[di, wi]:F2} ");
            }
            Console.WriteLine("|");
        }
        Console.WriteLine();

        Console.WriteLine("Таблиця: Коефіцієнти прискорення (S = T_seq / T_par)");
        Console.WriteLine();
        Console.Write("| Датасет ");
        foreach (int w in WorkerCounts) Console.Write($"| {w} потоки ");
        Console.WriteLine("|");

        Console.Write("|---");
        foreach (var _ in WorkerCounts) Console.Write("|---");
        Console.WriteLine("|");

        for (int di = 0; di < Datasets.Length; di++)
        {
            Console.Write($"| {Datasets[di].Name} ");
            for (int wi = 0; wi < WorkerCounts.Length; wi++)
            {
                double speedup = seq[di] / par[di, wi];
                Console.Write($"| {speedup:F2}x ");
            }
            Console.WriteLine("|");
        }
        Console.WriteLine();
    }
}
