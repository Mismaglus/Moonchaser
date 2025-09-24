using System.Collections.Generic;
using UnityEngine;

namespace Core.Hex
{
    /// <summary>
    /// ��������ֵ���ͣ�odd-r���ⶥ����r �����򱱵���
    /// </summary>
    public readonly struct HexCoords : System.IEquatable<HexCoords>
    {
        public readonly int q; // �У���������
        public readonly int r; // �У��ϡ�����

        public HexCoords(int q, int r) { this.q = q; this.r = r; }
        public override string ToString() => $"({q},{r})";

        public bool Equals(HexCoords other) => q == other.q && r == other.r;
        public override bool Equals(object obj) => obj is HexCoords h && Equals(h);
        public override int GetHashCode() => (q * 397) ^ r;

        // ���� ���껻�� ����
        public Vector3 ToWorld(float outerRadius, bool useOddROffset) =>
            HexMetrics.GridToWorld(q, r, outerRadius, useOddROffset);

        public static HexCoords FromWorld(Vector3 worldXZ, float outerRadius, bool useOddROffset)
        {
            var (Q, R) = HexMetrics.WorldToGrid(worldXZ, outerRadius, useOddROffset);
            return new HexCoords(Q, R);
        }

        // ���� �ھ� ����
        /// <summary>dir: 0:E,1:NE,2:NW,3:W,4:SW,5:SE</summary>
        public HexCoords Neighbor(int dir)
        {
            var nei = ((r & 1) == 1) ? HexMetrics.NEI_ODDR : HexMetrics.NEI_EVENR;
            var d = nei[dir % 6];
            return new HexCoords(q + d.x, r + d.y);
        }

        public IEnumerable<HexCoords> Neighbors()
        {
            var nei = ((r & 1) == 1) ? HexMetrics.NEI_ODDR : HexMetrics.NEI_EVENR;
            for (int i = 0; i < 6; i++) yield return new HexCoords(q + nei[i].x, r + nei[i].y);
        }

        // ���� ���루ͨ�� cube������
        public int DistanceTo(HexCoords other)
        {
            Cube(this, out int x1, out int y1, out int z1);
            Cube(other, out int x2, out int y2, out int z2);
            return Mathf.Max(Mathf.Abs(x1 - x2), Mathf.Abs(y1 - y2), Mathf.Abs(z1 - z2));
        }

        // ���� ��״���ɣ����ڷ�Χ����/Ѱ·���ӻ�������
        /// <summary>���̣������뾶��radius �����и񣨺����ģ�</summary>
        public IEnumerable<HexCoords> Disk(int radius)
        {
            // �� BFS���뾶С���㹻��
            var visited = new HashSet<HexCoords>();
            var q = new Queue<(HexCoords c, int d)>();
            visited.Add(this);
            q.Enqueue((this, 0));
            while (q.Count > 0)
            {
                var (c, d) = q.Dequeue();
                if (d > radius) continue;
                yield return c;
                if (d == radius) continue;
                foreach (var n in c.Neighbors())
                {
                    if (!visited.Contains(n))
                    {
                        visited.Add(n);
                        q.Enqueue((n, d + 1));
                    }
                }
            }
        }

        /// <summary>����ǡ�þ��� == radius �ĸ�</summary>
        public IEnumerable<HexCoords> Ring(int radius)
        {
            if (radius <= 0) { yield break; }
            // �Ӷ��ϱ߿�ʼ�� 6 ������Ѳ��
            Cube(this, out int cx, out int cy, out int cz);
            // 6 �� cube ���򣨼ⶥ��
            (int x, int y, int z)[] dirs = {
                ( +1,-1, 0), ( +1, 0,-1), ( 0, +1,-1),
                ( -1,+1, 0), ( -1, 0,+1), ( 0, -1,+1)
            };
            // ��㣺���� + dir[4]*radius��Լ���� SW ����
            int x = cx + dirs[4].x * radius;
            int y = cy + dirs[4].y * radius;
            int z = cz + dirs[4].z * radius;

            for (int edge = 0; edge < 6; edge++)
            {
                for (int step = 0; step < radius; step++)
                {
                    var h = FromCube(x, y, z);
                    yield return h;
                    // �ص�ǰ�߷���ǰ��
                    x += dirs[edge].x;
                    y += dirs[edge].y;
                    z += dirs[edge].z;
                }
            }
        }

        // ���� �ڲ���odd-r <-> cube ������r �򱱵�����
        static void Cube(HexCoords h, out int x, out int y, out int z)
        {
            // odd-r �� cube��x = q - (r - (r&1))/2; z = r; y = -x - z
            x = h.q - ((h.r - (h.r & 1)) >> 1);
            z = h.r;
            y = -x - z;
        }
        static HexCoords FromCube(int x, int y, int z)
        {
            int r = z;
            int q = x + ((r - (r & 1)) >> 1);
            return new HexCoords(q, r);
        }
    }
}
