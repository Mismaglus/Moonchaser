using Game.Core;
using UnityEngine;

[DisallowMultipleComponent]
public class UnitHighlighter : MonoBehaviour
{
    [Header("Assign the SG_OutlineShell material")]
    public Material outlineMaterialPrefab;

    [Range(0, 0.08f)] public float widthHover = 0.025f;
    [Range(0, 0.08f)] public float widthSelected = 0.035f;

    [Header("Colors (HDR)")]
    public Color colorPlayer = new Color(0.20f, 1.80f, 0.60f, 1f);     // ����
    public Color colorSelected = new Color(1.80f, 1.40f, 0.50f, 1f);  // ���
    public Color colorEnemy = new Color(2.00f, 0.60f, 0.40f, 1f);     // �з����

    public FactionMembership _faction;
    public bool IsPlayerControlled
    {
        get
        {
            if (_faction == null) TryGetComponent(out _faction);
            if (_faction != null) return _faction.IsPlayerControlled;
            return true; // �����ʱĬ�Ͽɿ�
        }
        set
        {
            if (_faction == null) TryGetComponent(out _faction);
            if (_faction != null) { _faction.playerControlled = value; return; }
        }
    }
    private SkinnedMeshRenderer _r;
    private Material _outlineMat;   // ʵ��
    private int _slot = -1;
    private bool _hoverOn, _selOn;
    private Color colorHover;
    static readonly int ColID = Shader.PropertyToID("_OutlineColor");
    static readonly int WidID = Shader.PropertyToID("_OutlineWidth");

    void Awake()
    {
        _r = GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (!_r || !outlineMaterialPrefab) return;

        _outlineMat = new Material(outlineMaterialPrefab); // ÿ����λһ��
        var mats = _r.sharedMaterials;
        System.Array.Resize(ref mats, mats.Length + 1);
        _slot = mats.Length - 1;
        mats[_slot] = _outlineMat;
        _r.sharedMaterials = mats;
        if (IsPlayerControlled)
        {
            colorHover = colorPlayer;
        }
        else
        {
            colorHover = colorEnemy;
        }

        Hide(); // Ĭ������
    }

    public void SetHover(bool on)
    {
        if (_slot < 0) return;
        _hoverOn = on;
        if (on)
        {
            _outlineMat.SetColor(ColID, colorHover);
            _outlineMat.SetFloat(WidID, widthHover);
        }
        else if (!_selOn) Hide();
    }

    public void SetSelected(bool on, bool enemy = false)
    {
        if (_slot < 0) return;
        _selOn = on;
        if (on)
        {
            _outlineMat.SetColor(ColID, enemy ? colorEnemy : colorSelected);
            _outlineMat.SetFloat(WidID, widthSelected);
        }
        else if (!_hoverOn) Hide();
    }

    public void Hide()
    {
        if (_slot < 0) return;
        _outlineMat.SetFloat(WidID, 0f);
    }
}
