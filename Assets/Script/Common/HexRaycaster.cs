using UnityEngine;

namespace Game.Common
{
    public static class HexRaycaster
    {
        static readonly RaycastHit[] _hits = new RaycastHit[8];

        /// <summary>�������Ļ���귢�����ߣ����д� TileTag ����Ƭ��</summary>
        public static bool TryPick(Camera cam, Vector2 screenPos, out GameObject tileGO, out TileTag tag, int layerMask = ~0, float maxDist = 2000f)
        {
            tileGO = null; tag = null;
            if (!cam) return false;

            var ray = cam.ScreenPointToRay(screenPos);
            int n = Physics.RaycastNonAlloc(ray, _hits, maxDist, layerMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var h = _hits[i];
                if (!h.collider) continue;
                var t = h.collider.GetComponent<TileTag>();
                if (!t) t = h.collider.GetComponentInParent<TileTag>();
                if (t)
                {
                    tileGO = t.gameObject;
                    tag = t;
                    return true;
                }
            }
            return false;
        }
    }
}
