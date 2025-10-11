// Script/Game/Battle/Abilities/Ability.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Battle.Abilities
{
    public enum TargetShape { Self, Single, Disk, Ring, Line } // extend later
    public enum TargetFaction { Any, Ally, Enemy, SelfOnly }

    public abstract class Ability : ScriptableObject
    {
        [Header("Identity")]
        public string abilityId;
        public string displayName;

        [Header("Costs & Cooldown")]
        public int apCost = 1;
        public int cooldownTurns = 0;

        [Header("Targeting")]
        public TargetShape shape = TargetShape.Single;
        public TargetFaction targetFaction = TargetFaction.Enemy;
        public int minRange = 1;
        public int maxRange = 1;
        public bool requiresLoS = false;

        [Header("Effects")]
        public List<AbilityEffect> effects = new();

        public virtual bool CanUse(BattleUnit caster) => caster != null && caster.CurAP >= apCost;

        public virtual bool IsValidTarget(BattleUnit caster, AbilityContext ctx) => ctx != null && ctx.HasAnyTarget;

        public virtual IEnumerator Execute(BattleUnit caster, AbilityContext ctx, AbilityRunner runner)
        {
            if (!CanUse(caster) || !IsValidTarget(caster, ctx)) yield break;
            caster.TrySpendAP(apCost);

            // VFX/SFX hooks could be placed here or inside runner.PerformEffects
            yield return runner.PerformEffects(caster, this, ctx, effects);
        }
    }
}
