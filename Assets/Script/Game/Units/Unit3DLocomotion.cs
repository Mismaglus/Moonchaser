using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Unit3DLocomotion : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private float speedScale = 1f;       // 视需要调到合适的数值
    [SerializeField] private float minMoveSpeed = 0.05f;  // 小于此速度视为静止
    [SerializeField] private Game.Units.Unit unit;
    [SerializeField] private float refSecondsPerTile = 1f;   // 以“1秒/格”为基准
    //[SerializeField] private float animSpeedLerp = 10f;
    [Header("Rotation")]
    [SerializeField] private float rotateLerp = 12f; // 转身平滑
    [Tooltip("仅在移动时才转向")]
    [SerializeField] private bool rotateOnlyWhenMoving = true;

    [Header("Highlight (optional)")]
    //[SerializeField] private Color hoverTint = new Color(0.9f, 0.9f, 1.1f, 1f);
    //[SerializeField] private Color selectedTint = new Color(1.1f, 1.05f, 0.9f, 1f);
    //[SerializeField] private float tintStrength = 0.15f; // 0~0.5 之间比较安全

    private Transform _parent;
    private Vector3 _lastParentPos;
    private int _speedHash;
    private float _speed;
    // 替换：字段区（多材质槽缓存）
    private Renderer[] _renderers;
    private MaterialPropertyBlock _mpb;

    // 原来是 List<int>/List<Color>，现在变成“每个 Renderer 一组数组”
    private readonly List<int[]> _colorPropIdsPerRenderer = new(); // 每材质槽的可写属性ID（-1=无）
    private readonly List<Color[]> _origColorsPerRenderer = new(); // 每材质槽的原始色

    // 常见颜色属性ID
    private static readonly int PropBaseColor = Shader.PropertyToID("_BaseColor"); // URP Lit/SimpleLit/Unlit
    private static readonly int PropColor = Shader.PropertyToID("_Color");     // Standard/自定义
    private static readonly int PropTint = Shader.PropertyToID("_Tint");      // 一些Unlit变体

    private readonly List<Color> _origBaseColors = new();
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
    }
    // 替换：Awake() —— 为每个 Renderer 的每个材质槽建缓存
    private void Awake()
    {
        if (!unit) unit = GetComponentInParent<Game.Units.Unit>();

        _parent = transform.parent != null ? transform.parent : transform;
        _lastParentPos = _parent.position;
        _speedHash = Animator.StringToHash(speedParam);

        _renderers = GetComponentsInChildren<Renderer>(true);
        _mpb = new MaterialPropertyBlock();

        _colorPropIdsPerRenderer.Clear();
        _origColorsPerRenderer.Clear();

        foreach (var r in _renderers)
        {
            var mats = r.sharedMaterials; // 注意：是“数组”，一个 Renderer 可能多个材质
            if (mats == null || mats.Length == 0)
            {
                _colorPropIdsPerRenderer.Add(System.Array.Empty<int>());
                _origColorsPerRenderer.Add(System.Array.Empty<Color>());
                continue;
            }

            var propIds = new int[mats.Length];
            var baseCols = new Color[mats.Length];

            for (int i = 0; i < mats.Length; i++)
            {
                var mat = mats[i];
                int prop = -1;
                if (mat != null)
                {
                    if (mat.HasProperty(PropBaseColor)) prop = PropBaseColor;
                    else if (mat.HasProperty(PropColor)) prop = PropColor;
                    else if (mat.HasProperty(PropTint)) prop = PropTint;
                }
                propIds[i] = prop;
                baseCols[i] = (prop != -1 && mat != null) ? mat.GetColor(prop) : Color.white;
            }

            _colorPropIdsPerRenderer.Add(propIds);
            _origColorsPerRenderer.Add(baseCols);
        }
    }


    private void Update()
    {
        // 计算父物体（BattleUnit）的平面速度（XZ）
        Vector3 parentPos = _parent.position;
        Vector3 delta = parentPos - _lastParentPos;
        _lastParentPos = parentPos;

        Vector3 planar = new Vector3(delta.x, 0f, delta.z);
        animator.SetFloat(Animator.StringToHash("AnimRate"),
             refSecondsPerTile / Mathf.Max(0.01f, unit.secondsPerTile));
        float rawSpeed = planar.magnitude / Mathf.Max(Time.deltaTime, 1e-5f);
        _speed = rawSpeed * speedScale;

        if (animator)
            animator.SetFloat(_speedHash, _speed);

        // 转向（面向移动方向）
        if (!rotateOnlyWhenMoving || _speed > minMoveSpeed)
        {
            if (planar.sqrMagnitude > 1e-6f)
            {
                Quaternion target = Quaternion.LookRotation(planar.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, 1f - Mathf.Exp(-rotateLerp * Time.deltaTime));
            }
        }
    }

    // 供外部强制朝向（例如移动完成后面向最近一次点击的方向）
    public void FaceWorldDirection(Vector3 worldDir)
    {
        worldDir.y = 0f;
        if (worldDir.sqrMagnitude < 1e-6f) return;
        Quaternion target = Quaternion.LookRotation(worldDir.normalized, Vector3.up);
        transform.rotation = target;
    }

    // 这两个给 SelectionManager 调用（可选）
    //public void SetHover(bool on) => ApplyTint(on ? hoverTint : (Color?)null);
    //public void SetSelected(bool on) => ApplyTint(on ? selectedTint : (Color?)null);

    //// 替换：ApplyTint —— 对“每个材质槽”调用 SetPropertyBlock(..., index)
    //private void ApplyTint(Color? tint)
    //{
    //    if (_renderers == null || _renderers.Length == 0) return;

    //    for (int ri = 0; ri < _renderers.Length; ri++)
    //    {
    //        var r = _renderers[ri];
    //        var propIds = _colorPropIdsPerRenderer[ri];
    //        var baseCols = _origColorsPerRenderer[ri];

    //        // 这个 Renderer 没有材质，跳过
    //        if (propIds == null || baseCols == null || propIds.Length == 0) continue;

    //        for (int mi = 0; mi < propIds.Length; mi++)
    //        {
    //            int prop = propIds[mi];
    //            if (prop == -1) continue; // 该材质槽没有可写颜色属性

    //            Color baseC = baseCols[mi];

    //            _mpb.Clear();
    //            Color mixed = new Color(-1, -1, -1);
    //            if (tint.HasValue)
    //            {
    //                // 放大点效果，确保肉眼可见
    //                mixed = Color.Lerp(baseC,
    //                                         new Color(baseC.r * tint.Value.r, baseC.g * tint.Value.g, baseC.b * tint.Value.b, baseC.a),
    //                                         tintStrength);
    //                _mpb.SetColor(prop, mixed);
    //            }
    //            else
    //            {
    //                _mpb.SetColor(prop, baseC);
    //            }

    //            // 关键：对“指定材质槽”应用 MPB
    //            r.SetPropertyBlock(_mpb, mi);
    //        }
    //    }
    //}

}
