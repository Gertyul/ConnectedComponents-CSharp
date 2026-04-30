namespace ConnectedComponents.Algorithms;
public sealed class LockFreeUnionFind
{
    private readonly int[] _parent;
    private readonly int[] _rank;

    public LockFreeUnionFind(int n)
    {
        if (n < 0)
            throw new ArgumentOutOfRangeException(nameof(n));

        _parent = new int[n];
        _rank = new int[n];

        for (int i = 0; i < n; i++)
        {
            _parent[i] = i;
        }
    }

    public int Count => _parent.Length;

    public int Find(int x)
    {
        while (true)
        {
            int p = Volatile.Read(ref _parent[x]);
            int gp = Volatile.Read(ref _parent[p]);

            if (p == gp)
            {
                return p;
            }

            Interlocked.CompareExchange(ref _parent[x], gp, p);
            x = gp;
        }
    }

    public bool Union(int a, int b)
    {
        while (true)
        {
            int ra = Find(a);
            int rb = Find(b);

            if (ra == rb)
                return false;

            int rankA = Volatile.Read(ref _rank[ra]);
            int rankB = Volatile.Read(ref _rank[rb]);

            if (rankA < rankB || (rankA == rankB && ra > rb))
            {
                (ra, rb) = (rb, ra);
                (rankA, rankB) = (rankB, rankA);
            }

            if (Interlocked.CompareExchange(ref _parent[rb], ra, rb) != rb)
                continue;

            if (rankA == rankB)
            {
                Interlocked.Increment(ref _rank[ra]);
            }

            return true;
        }
    }
}
