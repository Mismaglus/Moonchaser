using UnityEngine;

namespace Core.Hex
{
    public enum HexOrientation { PointyTop, FlatTop }        // ��֧�� PointyTop
    public enum BorderMode { None, OuterOnly, AllUnique }  // ������ / ȫ��(ȥ��)

    public static class HexMetrics
    {
        public const float SQRT3 = 1.7320508075688772f;

        // �� i �ķ����������������ε�˳��һ�£��Ƕ� = 60*i - 30 �ȣ�
        public static readonly Vector2[] CORNER_DIRS = new Vector2[6] {
            new(Mathf.Cos(-30f * Mathf.Deg2Rad), Mathf.Sin(-30f * Mathf.Deg2Rad)), // 0
            new(Mathf.Cos( 30f * Mathf.Deg2Rad), Mathf.Sin( 30f * Mathf.Deg2Rad)), // 1
            new(Mathf.Cos( 90f * Mathf.Deg2Rad), Mathf.Sin( 90f * Mathf.Deg2Rad)), // 2
            new(Mathf.Cos(150f * Mathf.Deg2Rad), Mathf.Sin(150f * Mathf.Deg2Rad)), // 3
            new(Mathf.Cos(210f * Mathf.Deg2Rad), Mathf.Sin(210f * Mathf.Deg2Rad)), // 4
            new(Mathf.Cos(270f * Mathf.Deg2Rad), Mathf.Sin(270f * Mathf.Deg2Rad)), // 5
        };

        // odd-r���ⶥ���ھ�λ�ƣ��������������Ӧ
        // ����0 E, 1 NE, 2 NW, 3 W, 4 SW, 5 SE
        public static readonly Vector2Int[] NEI_EVENR = {
            new(+1, 0), new( 0,+1), new(-1,+1),
            new(-1, 0), new(-1,-1), new( 0,-1)
        };
        public static readonly Vector2Int[] NEI_ODDR = {
            new(+1, 0), new(+1,+1), new( 0,+1),
            new(-1, 0), new( 0,-1), new(+1,-1)
        };

        // odd-r �������� -> �������꣨XZ��
        public static Vector3 GridToWorld(int q, int r, float outerRadius, bool useOddROffset)
        {
            float inner = outerRadius * (0.5f * SQRT3);
            float x = q * (2f * inner);
            if (useOddROffset && (r & 1) == 1) x += inner; // ���������ư��
            float z = r * (1.5f * outerRadius);
            return new Vector3(x, 0f, z);
        }

        // odd-r���ⶥ����������(XZ) -> ����(q,r)
        // Լ����r �����򱱵�����useOddROffset=true����ǰ��Ŀ�������֣�
        // ʵ�֣�world -> axial ���� -> cube round -> odd-r
        public static (int q, int r) WorldToGrid(Vector3 worldXZ, float outerRadius, bool useOddROffset)
        {
            // 1) world -> axial(float)
            float qf = (SQRT3 / 3f * worldXZ.x - 1f / 3f * worldXZ.z) / outerRadius;
            float rf = (2f / 3f * worldXZ.z) / outerRadius;

            // 2) axial -> cube(float)
            float xf = qf;
            float zf = rf;
            float yf = -xf - zf;

            // 3) cube round
            int rx = Mathf.RoundToInt(xf);
            int ry = Mathf.RoundToInt(yf);
            int rz = Mathf.RoundToInt(zf);

            float dx = Mathf.Abs(rx - xf);
            float dy = Mathf.Abs(ry - yf);
            float dz = Mathf.Abs(rz - zf);

            if (dx > dy && dx > dz) rx = -ry - rz;
            else if (dy > dz) ry = -rx - rz;
            else rz = -rx - ry;

            // 4) cube -> odd-r��r ������
            int r = rz;
            int q = rx + ((r - (r & 1)) >> 1);
            return (q, r);
        }

    }
}
