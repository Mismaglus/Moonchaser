using Core.Hex;
using Game.Common;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Game.Grid;
namespace Game.Battle
{
    [ExecuteAlways]
    public class BattleHexGrid : MonoBehaviour, IHexGridProvider
    {
        [Header("�����䷽")]
        [SerializeField] private GridRecipe _recipe;
        [SerializeField, HideInInspector] private uint _version;

        public uint Version
        {
            get => _version;
            private set => _version = value;
        }

        public GridRecipe recipe
        {
            get => _recipe;
            private set => _recipe = value;
        }
        [SerializeField] private bool _useRecipeBorderMode = true; // ����=�����䷽���ص�=������ʱ����
        [SerializeField] private BorderMode _runtimeBorderMode = BorderMode.AllUnique;
        private BorderMode _lastRuntimeMode;                      // ����ʱ������

        [SerializeField] private int _lastRecipeHash;                      // �䷽��ϣ������ʱ����ã�

        const string CHILD_PREFIX_TILES = "Hex_r";
        const string CHILD_BORDERS = "GridBorders";

        Material _sharedMat;   // ��Ƭ����
        Material _borderMat;   // ���߲���
        bool _dirty;
        public IEnumerable<TileTag> EnumerateTiles()
        {
            return GetComponentsInChildren<TileTag>(true);
        }

        void OnEnable() { _dirty = true; }
#if UNITY_EDITOR
        void OnValidate() { _dirty = true; }    // ����ֻ����
#endif
        void Update()
        {
            if (!isActiveAndEnabled) return;

            bool need = _dirty;

            if (recipe != null)
            {
                int now = ComputeRecipeHash();
                if (now != _lastRecipeHash) { _lastRecipeHash = now; need = true; }
            }

            if (!_useRecipeBorderMode && _runtimeBorderMode != _lastRuntimeMode)
            {
                _lastRuntimeMode = _runtimeBorderMode;
                need = true;
            }

            if (need) { _dirty = false; Rebuild(); }
        }



        void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var ch = transform.GetChild(i).gameObject;
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(ch);
                else Destroy(ch);
#else
                Destroy(ch);
#endif
            }
        }

        public void Rebuild()
        {
            if (!isActiveAndEnabled) return;

            // ��ʱ�䷽��չ�������ܣ�
            if (recipe == null)
            {
                recipe = ScriptableObject.CreateInstance<GridRecipe>();
                recipe.width = 6; recipe.height = 6;
                recipe.outerRadius = 1f; recipe.thickness = 0f;
                recipe.useOddROffset = true;
                recipe.borderMode = BorderMode.AllUnique;
            }

            ClearChildren();

            // === ���� ===
            if (recipe.tileMaterial != null) _sharedMat = recipe.tileMaterial;
            else if (_sharedMat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                _sharedMat = new Material(shader) { color = new Color(0.18f, 0.2f, 0.25f, 1f) };
            }

            if (recipe.borderMaterial != null) _borderMat = recipe.borderMaterial;
            else if (_borderMat == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Unlit");
                _borderMat = new Material(sh);
                _borderMat.SetFloat("_Surface", 1f); // Transparent
                _borderMat.SetFloat("_ZWrite", 0f);  // Depth Off
                _borderMat.SetFloat("_Cull", 0f);    // Double-sided
                _borderMat.renderQueue = (int)RenderQueue.Transparent;
            }

            // === �������루������/�������===
            var mask = BuildMask(recipe);

            // === ����ƫ�ƣ�ֻ�����ڵĸ��ӣ�===
            var worldBounds = HexBorderMeshBuilder.ComputeWorldBounds(mask, recipe.outerRadius, recipe.useOddROffset);
            Vector3 centerOffset = new Vector3(worldBounds.center.x, 0f, worldBounds.center.z);

            // === ����Ƭ��ֻ�Ŵ��ڵĸ��ӣ�===
            for (int r = 0; r < recipe.height; r++)
            {
                for (int q = 0; q < recipe.width; q++)
                {
                    if (!mask[q, r]) continue;

                    Vector3 pos = HexMetrics.GridToWorld(q, r, recipe.outerRadius, recipe.useOddROffset) - centerOffset;

                    var go = new GameObject($"{CHILD_PREFIX_TILES}{r}_c{q}");
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = pos;

                    var tile = go.AddComponent<HexTile>();
                    tile.outerRadius = recipe.outerRadius;
                    tile.thickness = recipe.thickness;

                    var mr = go.GetComponent<MeshRenderer>();
                    mr.sharedMaterial = _sharedMat;

                    tile.BuildImmediate();

                    var tag = go.AddComponent<Game.Common.TileTag>();
                    tag.Set(q, r);

                }
            }

            // === ���ߣ�Ψһ����===
            var mode = _useRecipeBorderMode ? recipe.borderMode : _runtimeBorderMode;
            if (mode != BorderMode.None)
            {
                var bordersGO = new GameObject("GridBorders");
                bordersGO.transform.SetParent(transform, false);

                var mf = bordersGO.AddComponent<MeshFilter>();
                var mr = bordersGO.AddComponent<MeshRenderer>();
                mr.sharedMaterial = _borderMat;

                var mesh = HexBorderMeshBuilder.Build(
                    /* HexMask */ mask,
                    recipe.outerRadius, recipe.borderYOffset, recipe.thickness,
                    recipe.borderWidth, recipe.useOddROffset,
                    mode
                );
                mf.sharedMesh = mesh;

                var mpb = new MaterialPropertyBlock();
                mpb.SetColor("_BaseColor", recipe.borderColor);
                mpb.SetColor("_Color", recipe.borderColor);
                mr.SetPropertyBlock(mpb);
            }
        }

        // �������룺Ĭ��ȫ True�����޳����л�����ڶ��������½ڵ�ͼԭ�ͣ�
        HexMask BuildMask(GridRecipe rcp)
        {
            int w = rcp.width, h = rcp.height;
            var mask = HexMask.Filled(w, h);

            // �������
            if (rcp.emptyColumns != null)
            {
                foreach (var col in rcp.emptyColumns)
                {
                    if (col < 0 || col >= w) continue;
                    mask.ClearColumn(col);
                }
            }

            // ����ڶ�����ѡ��
            if (rcp.enableRandomHoles && rcp.holeChance > 0f)
                mask.RandomHoles(rcp.holeChance, rcp.randomSeed);
            Version++;
            return mask;
        }


        // �����ڽӿڣ��ⲿ�ű����л�����ģʽ
        public void SetBorderMode(BorderMode mode) { _runtimeBorderMode = mode; _dirty = true; }

        [ContextMenu("Rebuild Grid")]
        void RebuildFromMenu() { _dirty = true; }

        int ComputeRecipeHash()
        {
            if (recipe == null) return 0;
            unchecked
            {
                int h = 17;
                h = h * 31 + recipe.width;
                h = h * 31 + recipe.height;
                h = h * 31 + recipe.useOddROffset.GetHashCode();
                h = h * 31 + recipe.borderMode.GetHashCode();
                h = h * 31 + recipe.outerRadius.GetHashCode();
                h = h * 31 + recipe.thickness.GetHashCode();
                h = h * 31 + recipe.borderWidth.GetHashCode();
                h = h * 31 + recipe.borderYOffset.GetHashCode();
                if (recipe.emptyColumns != null)
                {
                    h = h * 31 + recipe.emptyColumns.Length;
                    for (int i = 0; i < recipe.emptyColumns.Length; i++) h = h * 31 + recipe.emptyColumns[i];
                }
                h = h * 31 + recipe.enableRandomHoles.GetHashCode();
                h = h * 31 + recipe.holeChance.GetHashCode();
                h = h * 31 + recipe.randomSeed;
                return h;
            }
        }


    }
}
