using UnityEngine;
using Core.Hex;

namespace Game.Common
{
    /// <summary>�ҵ�ÿ�� Hex_r?_c? �������ϣ��ṩ (q,r) ���ꡣ</summary>
    [DisallowMultipleComponent]
    public class TileTag : MonoBehaviour
    {
        [SerializeField] int _q;
        [SerializeField] int _r;
        public HexCoords Coords => new HexCoords(_q, _r);
        public void Set(int q, int r) { _q = q; _r = r; }
    }
}
