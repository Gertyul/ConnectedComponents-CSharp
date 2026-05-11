namespace ConnectedComponents.Algorithms;

public sealed class SequentialUnionFind
{
    private readonly int[] _parent;
    private readonly int[] _rank;

    public SequentialUnionFind(int n)
    {
        _parent = new int[n];
        _rank = new int[n];
        for (int i = 0; i < n; i++) _parent[i] = i;
    }

    public int Find(int x)
    {
        while (true)
        {
            int p = _parent[x];
            int gp = _parent[p];
            if (p == gp) return p;
            _parent[x] = gp;
            x = gp;
        }
    }

    public bool Union(int a, int b)
    {
        int ra = Find(a);
        int rb = Find(b);
        if (ra == rb) return false;

        int rankA = _rank[ra];
        int rankB = _rank[rb];

        if (rankA < rankB || (rankA == rankB && ra > rb))
        {
            (ra, rb) = (rb, ra);
            (rankA, rankB) = (rankB, rankA);
        }

        _parent[rb] = ra;
        if (rankA == rankB) _rank[ra]++;
        return true;
    }
}