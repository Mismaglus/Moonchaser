using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Unit3DLocomotion : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private float speedScale = 1f;       // ����Ҫ�������ʵ���ֵ
    [SerializeField] private float minMoveSpeed = 0.05f;  // С�ڴ��ٶ���Ϊ��ֹ
    [SerializeField] private Game.Units.Unit unit;
    [SerializeField] private float refSecondsPerTile = 1f;   // �ԡ�1��/��Ϊ��׼
    //[SerializeField] private float animSpeedLerp = 10f;
    [Header("Rotation")]
    [SerializeField] private float rotateLerp = 12f; // ת��ƽ��
    [Tooltip("�����ƶ�ʱ��ת��")]
    [SerializeField] private bool rotateOnlyWhenMoving = true;

    [Header("Highlight (optional)")]
    //[SerializeField] private Color hoverTint = new Color(0.9f, 0.9f, 1.1f, 1f);
    //[SerializeField] private Color selectedTint = new Color(1.1f, 1.05f, 0.9f, 1f);
    //[SerializeField] private float tintStrength = 0.15f; // 0~0.5 ֮��Ƚϰ�ȫ

    private Transform _parent;
    private Vector3 _lastParentPos;
    private int _speedHash;
    private float _speed;
    // �滻���ֶ���������ʲۻ��棩
    private Renderer[] _renderers;
    private MaterialPropertyBlock _mpb;

    // ԭ���� List<int>/List<Color>�����ڱ�ɡ�ÿ�� Renderer һ�����顱
    private readonly List<int[]> _colorPropIdsPerRenderer = new(); // ÿ���ʲ۵Ŀ�д����ID��-1=�ޣ�
    private readonly List<Color[]> _origColorsPerRenderer = new(); // ÿ���ʲ۵�ԭʼɫ

    // ������ɫ����ID
    private static readonly int PropBaseColor = Shader.PropertyToID("_BaseColor"); // URP Lit/SimpleLit/Unlit
    private static readonly int PropColor = Shader.PropertyToID("_Color");     // Standard/�Զ���
    private static readonly int PropTint = Shader.PropertyToID("_Tint");      // һЩUnlit����

    private readonly List<Color> _origBaseColors = new();
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
    }
    // �滻��Awake() ���� Ϊÿ�� Renderer ��ÿ�����ʲ۽�����
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
            var mats = r.sharedMaterials; // ע�⣺�ǡ����顱��һ�� Renderer ���ܶ������
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
        // ���㸸���壨BattleUnit����ƽ���ٶȣ�XZ��
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

        // ת�������ƶ�����
        if (!rotateOnlyWhenMoving || _speed > minMoveSpeed)
        {
            if (planar.sqrMagnitude > 1e-6f)
            {
                Quaternion target = Quaternion.LookRotation(planar.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, 1f - Mathf.Exp(-rotateLerp * Time.deltaTime));
            }
        }
    }

    // ���ⲿǿ�Ƴ��������ƶ���ɺ��������һ�ε���ķ���
    public void FaceWorldDirection(Vector3 worldDir)
    {
        worldDir.y = 0f;
        if (worldDir.sqrMagnitude < 1e-6f) return;
        Quaternion target = Quaternion.LookRotation(worldDir.normalized, Vector3.up);
        transform.rotation = target;
    }

    // �������� SelectionManager ���ã���ѡ��
    //public void SetHover(bool on) => ApplyTint(on ? hoverTint : (Color?)null);
    //public void SetSelected(bool on) => ApplyTint(on ? selectedTint : (Color?)null);

    //// �滻��ApplyTint ���� �ԡ�ÿ�����ʲۡ����� SetPropertyBlock(..., index)
    //private void ApplyTint(Color? tint)
    //{
    //    if (_renderers == null || _renderers.Length == 0) return;

    //    for (int ri = 0; ri < _renderers.Length; ri++)
    //    {
    //        var r = _renderers[ri];
    //        var propIds = _colorPropIdsPerRenderer[ri];
    //        var baseCols = _origColorsPerRenderer[ri];

    //        // ��� Renderer û�в��ʣ�����
    //        if (propIds == null || baseCols == null || propIds.Length == 0) continue;

    //        for (int mi = 0; mi < propIds.Length; mi++)
    //        {
    //            int prop = propIds[mi];
    //            if (prop == -1) continue; // �ò��ʲ�û�п�д��ɫ����

    //            Color baseC = baseCols[mi];

    //            _mpb.Clear();
    //            Color mixed = new Color(-1, -1, -1);
    //            if (tint.HasValue)
    //            {
    //                // �Ŵ��Ч����ȷ�����ۿɼ�
    //                mixed = Color.Lerp(baseC,
    //                                         new Color(baseC.r * tint.Value.r, baseC.g * tint.Value.g, baseC.b * tint.Value.b, baseC.a),
    //                                         tintStrength);
    //                _mpb.SetColor(prop, mixed);
    //            }
    //            else
    //            {
    //                _mpb.SetColor(prop, baseC);
    //            }

    //            // �ؼ����ԡ�ָ�����ʲۡ�Ӧ�� MPB
    //            r.SetPropertyBlock(_mpb, mi);
    //        }
    //    }
    //}

}
