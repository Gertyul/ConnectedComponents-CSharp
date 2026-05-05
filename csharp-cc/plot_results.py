import csv
import os
import sys

try:
    import matplotlib.pyplot as plt
except ImportError:
    print("Помилка: не встановлено matplotlib.")
    print("Встановіть: pip install matplotlib")
    sys.exit(1)


def find_csv():
    candidates = [
        "benchmark_results.csv",
        "../benchmark_results.csv",
        "../../benchmark_results.csv",
        os.path.join(os.path.dirname(__file__), "benchmark_results.csv"),
        os.path.join(os.path.dirname(__file__), "..", "benchmark_results.csv"),
    ]
    for c in candidates:
        if os.path.exists(c):
            return os.path.abspath(c)
    return None


def read_data(csv_path):
    """Парсить CSV у словник {dataset_name: {workers: speedup, ..., 'sequential_ms': float, 'time': {workers: ms}}}"""
    data = {}
    with open(csv_path, "r", encoding="utf-8") as f:
        reader = csv.DictReader(f)
        for row in reader:
            ds = row["dataset"]
            data[ds] = {
                "nodes": int(row["nodes"]),
                "edges": int(row["edges"]),
                "sequential_ms": float(row["sequential_ms"]),
                "times": {},
                "speedups": {},
            }
            # Знаходимо всі парні колонки parallel_X_ms / speedup_X
            for key, value in row.items():
                if key.startswith("parallel_") and key.endswith("_ms"):
                    workers = int(key[len("parallel_"):-len("_ms")])
                    data[ds]["times"][workers] = float(value)
                elif key.startswith("speedup_"):
                    workers = int(key[len("speedup_"):])
                    data[ds]["speedups"][workers] = float(value)
    return data


def plot_time(data, out_path):
    """Графік 1: час виконання vs кількість потоків (логарифмічна шкала по Y)"""
    fig, ax = plt.subplots(figsize=(10, 6))

    styles = {
        "sparse": ("o-", "#1f77b4"),
        "medium": ("s-", "#ff7f0e"),
        "dense":  ("^-", "#d62728"),
        "large":  ("D-", "#2ca02c"),
    }

    for ds_name, ds_data in data.items():
        if ds_name not in styles:
            continue
        marker, color = styles[ds_name]
        workers = sorted(ds_data["times"].keys())
        times = [ds_data["times"][w] for w in workers]
        label = f"{ds_name} (N={ds_data['nodes']:,}, M={ds_data['edges']:,})"
        ax.plot(workers, times, marker, label=label, color=color, linewidth=2, markersize=8)

        # Горизонтальна лінія - час послідовного
        ax.axhline(y=ds_data["sequential_ms"], color=color, linestyle=":", alpha=0.4)

    ax.set_xlabel("Кількість робочих потоків", fontsize=12)
    ax.set_ylabel("Час виконання, мс (логарифмічна шкала)", fontsize=12)
    ax.set_title("Час виконання паралельного алгоритму\nна графах різного розміру та щільності", fontsize=13)
    ax.set_xscale("log", base=2)
    ax.set_yscale("log")
    ax.set_xticks([1, 2, 4, 8, 16, 32])
    ax.set_xticklabels([1, 2, 4, 8, 16, 32])
    ax.grid(True, which="both", linestyle="--", alpha=0.5)
    ax.legend(loc="best", fontsize=10)
    plt.tight_layout()
    plt.savefig(out_path, dpi=300, bbox_inches="tight")
    plt.close()
    print(f"  Збережено: {out_path}")


def plot_speedup(data, out_path):
    """Графік 2: коефіцієнт прискорення vs кількість потоків (БЕЗ ідеальної лінії)"""
    fig, ax = plt.subplots(figsize=(10, 6))

    styles = {
        "sparse": ("o-", "#1f77b4"),
        "medium": ("s-", "#ff7f0e"),
        "dense":  ("^-", "#d62728"),
        "large":  ("D-", "#2ca02c"),
    }

    for ds_name, ds_data in data.items():
        if ds_name not in styles:
            continue
        marker, color = styles[ds_name]
        workers = sorted(ds_data["speedups"].keys())
        speedups = [ds_data["speedups"][w] for w in workers]
        label = f"{ds_name}"
        ax.plot(workers, speedups, marker, label=label, color=color, linewidth=2, markersize=8)

    # Цільова лінія (мінімальна планка)
    ax.axhline(y=1.2, color="red", linestyle=":", alpha=0.6, label="Ціль ТЗ: 1,2x")

    ax.set_xlabel("Кількість робочих потоків", fontsize=12)
    ax.set_ylabel("Коефіцієнт прискорення (S = T_seq / T_par)", fontsize=12)
    ax.set_title("Залежність коефіцієнта прискорення\nвід кількості потоків", fontsize=13)
    ax.set_xscale("log", base=2)
    ax.set_xticks([1, 2, 4, 8, 16, 32])
    ax.set_xticklabels([1, 2, 4, 8, 16, 32])
    ax.grid(True, which="both", linestyle="--", alpha=0.5)
    ax.legend(loc="best", fontsize=10)
    plt.tight_layout()
    plt.savefig(out_path, dpi=300, bbox_inches="tight")
    plt.close()
    print(f"  Збережено: {out_path}")


def plot_density(data, out_path):
    """Графік 3: порівняння трьох рівнів щільності"""
    targets = ["sparse", "medium", "dense"]
    available = [t for t in targets if t in data]
    if len(available) < 2:
        print(f"  Пропущено plot_density: потрібно >= 2 з {targets}, наявні: {available}")
        return

    fig, ax = plt.subplots(figsize=(10, 6))

    workers_set = sorted(data[available[0]]["speedups"].keys())
    width = 0.25
    x_positions = list(range(len(workers_set)))

    colors = {"sparse": "#1f77b4", "medium": "#ff7f0e", "dense": "#d62728"}

    for i, ds_name in enumerate(available):
        offset = (i - 1) * width
        speedups = [data[ds_name]["speedups"][w] for w in workers_set]
        bars = ax.bar(
            [x + offset for x in x_positions],
            speedups,
            width,
            label=f"{ds_name} (M={data[ds_name]['edges']:,})",
            color=colors.get(ds_name, "gray"),
        )

        for bar, val in zip(bars, speedups):
            ax.text(bar.get_x() + bar.get_width() / 2, bar.get_height() + 0.05,
                    f"{val:.1f}x", ha="center", fontsize=8)

    ax.set_xlabel("Кількість робочих потоків", fontsize=12)
    ax.set_ylabel("Коефіцієнт прискорення", fontsize=12)
    ax.set_title("Вплив щільності графа на ефективність паралелізації\n(N = 100 000 вершин у всіх трьох випадках)", fontsize=13)
    ax.set_xticks(x_positions)
    ax.set_xticklabels(workers_set)
    ax.axhline(y=1.0, color="gray", linestyle="-", alpha=0.4)
    ax.grid(True, axis="y", linestyle="--", alpha=0.5)
    ax.legend(loc="best", fontsize=10)
    plt.tight_layout()
    plt.savefig(out_path, dpi=300, bbox_inches="tight")
    plt.close()
    print(f"  Збережено: {out_path}")


def main():
    print("─" * 50)
    print("  Побудова графіків для курсової")
    print("─" * 50)

    csv_path = find_csv()
    if not csv_path:
        print()
        print("ПОМИЛКА: не знайдено benchmark_results.csv.")
        sys.exit(1)

    data = read_data(csv_path)
    out_dir = os.path.dirname(csv_path)

    plot_time(data, os.path.join(out_dir, "plot_time.png"))
    plot_speedup(data, os.path.join(out_dir, "plot_speedup.png"))
    plot_density(data, os.path.join(out_dir, "plot_density.png"))

    print()
    print("  Готово! Графіки збережено.")


if __name__ == "__main__":
    main()