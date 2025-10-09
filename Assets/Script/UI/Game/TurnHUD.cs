using UnityEngine;
using UnityEngine.UIElements;
using Game.Battle;
using Game.Localization;
public class TurnHUD_UITK : MonoBehaviour
{
    public BattleStateMachine battle;
    public BattleUnit playerUnit;

    [SerializeField]
    UIDocument doc;
    Label turnText, apText;
    Button endTurnBtn;

    void Awake()
    {
        if (doc == null)
            doc = GetComponent<UIDocument>() ?? GetComponentInChildren<UIDocument>();

        if (doc == null)
        {
            Debug.LogError($"{nameof(TurnHUD_UITK)} requires a {nameof(UIDocument)} on the same GameObject or its children.", this);
            return;
        }

        var root = doc.rootVisualElement;
        if (root == null)
        {
            Debug.LogError($"{nameof(UIDocument)} on {name} has no root visual element assigned.", this);
            return;
        }
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
