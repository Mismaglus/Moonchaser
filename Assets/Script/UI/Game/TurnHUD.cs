using UnityEngine;
using UnityEngine.UIElements;
using Game.Battle;
using Game.Localization;
public class TurnHUD_UITK : MonoBehaviour
{
    public BattleStateMachine battle;
    public BattleUnit playerUnit;
    public UIDocument doc;

    [SerializeField]
    Label turnText, apText;
    Button endTurnBtn;

    void Awake()
    {
        var root = doc.rootVisualElement;
        turnText = root.Q<Label>("TurnText");
        apText = root.Q<Label>("APText");
        endTurnBtn = root.Q<Button>("EndTurnBtn");
        endTurnBtn.clicked += OnEndTurn;
    }

    void OnEnable()
    {
        if (battle != null) battle.OnTurnChanged += UpdateTurnUI;
        UpdateTurnUI(battle != null ? battle.CurrentTurn : TurnSide.Player);
        UpdateAP();
    }

    void OnDisable()
    {
        if (battle != null) battle.OnTurnChanged -= UpdateTurnUI;
        if (endTurnBtn != null) endTurnBtn.clicked -= OnEndTurn;
    }

    void Update()
    {
        UpdateAP();
        endTurnBtn?.SetEnabled(battle != null && battle.CurrentTurn == TurnSide.Player);
    }

    void UpdateTurnUI(TurnSide side)
    {
        if (turnText != null)
        {
            var key = side == TurnSide.Player ? "turn.player" : "turn.enemy";
            turnText.text = LocalizationManager.Get(key);
        }
    }

    void UpdateAP()
    {
        if (playerUnit != null && apText != null)
            apText.text = $"AP {playerUnit.CurAP}/{playerUnit.MaxAP}";
    }

    void OnEndTurn()
    {
        if (battle != null && battle.CurrentTurn == TurnSide.Player)
            battle.EndTurnRequest();
    }
}
