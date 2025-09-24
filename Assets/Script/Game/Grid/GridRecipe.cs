using UnityEngine;
using Core.Hex;

namespace Game.Battle
{
    [CreateAssetMenu(fileName = "BattleGridRecipe", menuName = "Game/Battle/Grid Recipe")]
    public class GridRecipe : ScriptableObject
    {
        [Header("����")]
        [Min(1)] public int width = 6;
        [Min(1)] public int height = 6;
        public HexOrientation orientation = HexOrientation.PointyTop;
        public bool useOddROffset = true;

        [Header("�����γߴ������")]
        [Min(0.1f)] public float outerRadius = 1f;
        [Range(0f, 0.5f)] public float thickness = 0f;
        public Material tileMaterial;

        [Header("����")]
        public BorderMode borderMode = BorderMode.AllUnique;       // None/OuterOnly/AllUnique
        [Min(0.001f)] public float borderWidth = 0.05f;
        [Min(0f)] public float borderYOffset = 0.001f;
        public Color borderColor = new(1f, 1f, 1f, 0.65f);
        public Material borderMaterial;

        [Header("����ѡ���ո�/���������½ڵ�ͼ�����")]
        public int[] emptyColumns;                  // ָ������Ϊ�գ��� [3,5]��
        public bool enableRandomHoles = false;      // ����ڶ�����ʾ/�ؿ�ԭ�ͣ�
        [Range(0f, 0.9f)] public float holeChance = 0.0f;
        public int randomSeed = 12345;
    }
}
