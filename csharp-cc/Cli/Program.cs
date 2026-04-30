using System.Diagnostics;
using ConnectedComponents.Algorithms;
using ConnectedComponents.Benchmarks;
using ConnectedComponents.Tests;

namespace ConnectedComponents.Cli;

internal static class Program
{
    private static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        while (true)
        {
            try
            {
                if (!ShowMainMenu())
                    return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"Помилка: {ex.Message}");
                Console.WriteLine();
                Pause();
            }
        }
    }

    private static bool ShowMainMenu()
    {
        Console.Clear();

        Console.WriteLine("Головне меню:");
        Console.WriteLine("  1) Запустити алгоритм на існуючому датасеті");
        Console.WriteLine("  2) Згенерувати новий тестовий граф");
        Console.WriteLine("  3) Запустити тести коректності");
        Console.WriteLine("  4) Запустити бенчмарк (з таблицями для курсової)");
        Console.WriteLine("  5) Згенерувати стандартний набір датасетів (small + medium + large)");
        Console.WriteLine("  0) Вийти");
        Console.WriteLine();
        Console.Write("Ваш вибір: ");

        string? choice = Console.ReadLine()?.Trim();
        Console.WriteLine();

        switch (choice)
        {
            case "0":
            case "":
            case null:
                return false;
            case "1":
                RunAlgorithm();
                Pause();
                return true;
            case "2":
                GenerateGraph();
                Pause();
                return true;
            case "3":
                TestRunner.RunAll();
                Pause();
                return true;
            case "4":
                QuickBenchmark.Run();
                Pause();
                return true;
            case "5":
                GenerateStandardDatasets();
                Pause();
                return true;
            default:
                Console.WriteLine("Невідомий вибір. Спробуйте ще раз.");
                Pause();
                return true;
        }
    }

    private static void RunAlgorithm()
    {
        Console.WriteLine("─────────── ЗАПУСК АЛГОРИТМУ ───────────");
        Console.WriteLine();

        string testdataDir = AppPaths.FindOrCreateTestdataDir();
        var files = Directory.GetFiles(testdataDir, "*.txt").OrderBy(f => new FileInfo(f).Length).ToArray();
        if (files.Length == 0)
        {
            Console.WriteLine("У директорії testdata/ немає жодного графа.");
            Console.WriteLine("Спочатку згенеруйте граф (опція 2 або 5 з головного меню).");
            return;
        }

        Console.WriteLine("Доступні датасети:");
        for (int i = 0; i < files.Length; i++)
        {
            string name = Path.GetFileNameWithoutExtension(files[i]);
            string info = ReadHeader(files[i]);
            Console.WriteLine($"  {i + 1}) {name}  {info}");
        }
        Console.Write("Виберіть датасет (Enter для першого): ");
        string? input = Console.ReadLine()?.Trim();
        int dsIdx = 0;
        if (!string.IsNullOrEmpty(input))
        {
            if (!int.TryParse(input, out dsIdx) || dsIdx < 1 || dsIdx > files.Length)
            {
                Console.WriteLine("Невірний вибір. Використано перший датасет.");
                dsIdx = 1;
            }
            dsIdx--;
        }
        string datasetPath = files[dsIdx];

        Console.WriteLine();
        Console.WriteLine("Виберіть алгоритм:");
        Console.WriteLine("  1) sequential (DFS)");
        Console.WriteLine("  2) parallel (Lock-free Union-Find)");
        Console.WriteLine("  3) обидва (для порівняння)");
        Console.Write("Ваш вибір (Enter для 'обидва'): ");
        string? algoChoice = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(algoChoice))
            algoChoice = "3";

        int workers = 0;
        if (algoChoice == "2" || algoChoice == "3")
        {
            int defaultWorkers = Environment.ProcessorCount;
            Console.Write($"Кількість воркерів [Enter для авто = {defaultWorkers}]: ");
            string? workersInput = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(workersInput) && int.TryParse(workersInput, out int w) && w > 0)
            {
                workers = w;
            }
        }

        Console.WriteLine();
        Console.Write("Завантаження графу... ");
        var graph = Graph.Graph.LoadFromFile(datasetPath);
        Console.WriteLine($"готово. Вершин: {graph.NodeCount}");
        Console.WriteLine();

        if (algoChoice == "1" || algoChoice == "3")
        {
            RunAndReport(new SequentialSolver(), graph);
        }
        if (algoChoice == "2" || algoChoice == "3")
        {
            var solver = workers > 0 ? new ParallelSolver(workers) : new ParallelSolver();
            RunAndReport(solver, graph);
        }
    }

    private static void RunAndReport(ISolver solver, Graph.Graph graph)
    {
        _ = solver.FindComponents(graph);

        var sw = Stopwatch.StartNew();
        var components = solver.FindComponents(graph);
        sw.Stop();

        Console.WriteLine($"  [{solver.Name}]");
        Console.WriteLine($"     Компонент:  {components.Length}");
        Console.WriteLine($"     Час:        {sw.Elapsed.TotalMilliseconds:F3} мс");
        if (solver is ParallelSolver ps)
        {
            int actualWorkers = ps.Workers > 0 ? ps.Workers : Environment.ProcessorCount;
            Console.WriteLine($"     Воркерів:   {actualWorkers}");
        }
        Console.WriteLine();
    }

    private static void GenerateGraph()
    {
        Console.WriteLine("─────────── ГЕНЕРАЦІЯ ГРАФУ ───────────");
        Console.WriteLine();

        Console.Write("Кількість вершин (Enter для 10000): ");
        string? input = Console.ReadLine()?.Trim();
        int nodes = string.IsNullOrEmpty(input) ? 10_000 : int.Parse(input);

        Console.Write("Кількість ребер (Enter для 50000): ");
        input = Console.ReadLine()?.Trim();
        int edges = string.IsNullOrEmpty(input) ? 50_000 : int.Parse(input);

        Console.Write("Назва файлу (без розширення, Enter для 'custom'): ");
        input = Console.ReadLine()?.Trim();
        string name = string.IsNullOrEmpty(input) ? "custom" : input;

        Console.Write("Зерно ГВЧ (Enter для 42): ");
        input = Console.ReadLine()?.Trim();
        int seed = string.IsNullOrEmpty(input) ? 42 : int.Parse(input);

        string testdataDir = AppPaths.FindOrCreateTestdataDir();
        string outputPath = Path.Combine(testdataDir, $"{name}.txt");

        Console.WriteLine();
        Console.Write($"Генерую граф ({nodes} вершин, {edges} ребер)... ");
        Graph.Graph.GenerateAndSave(nodes, edges, outputPath, seed);
        Console.WriteLine("готово.");
        Console.WriteLine($"Збережено: {outputPath}");
    }

    private static void GenerateStandardDatasets()
    {
        Console.WriteLine("─────────── ГЕНЕРАЦІЯ СТАНДАРТНОГО НАБОРУ ───────────");
        Console.WriteLine();

        var datasets = new (string Name, int Nodes, int Edges, string Density)[]
        {
            ("sparse", 100_000,   200_000,    "мала щільність, степ. ~4"),
            ("medium", 100_000,   2_000_000,  "середня щільність, степ. ~40"),
            ("dense",  100_000,   10_000_000, "велика щільність, степ. ~200"),
            ("large",  1_000_000, 5_000_000,  "масштаб, степ. ~10"),
        };

        string testdataDir = AppPaths.FindOrCreateTestdataDir();

        foreach (var (name, nodes, edges, density) in datasets)
        {
            string path = Path.Combine(testdataDir, $"{name}.txt");
            Console.Write($"  {name,-7} ({nodes,8} вершин, {edges,8} ребер, {density})... ");
            Graph.Graph.GenerateAndSave(nodes, edges, path);
            var fi = new FileInfo(path);
            Console.WriteLine($"готово. Розмір файлу: {FormatBytes(fi.Length)}");
        }

        Console.WriteLine();
        Console.WriteLine($"Усі датасети збережено в: {testdataDir}");
    }

    private static string ReadHeader(string path)
    {
        try
        {
            using var reader = new StreamReader(path);
            string? line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
                return string.Empty;
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return string.Empty;
            return $"({parts[0]} вершин, {parts[1]} ребер)";
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }

    private static void Pause()
    {
        Console.WriteLine();
        Console.WriteLine("Натисніть Enter для повернення до меню...");
        Console.ReadLine();
    }
}
