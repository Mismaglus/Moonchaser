// Script/Game/Battle/Abilities/AbilityRunner.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Battle.Abilities
{
    public class AbilityRunner : MonoBehaviour
    {
        public IEnumerator PerformEffects(BattleUnit caster, Ability ability, AbilityContext ctx, IList<AbilityEffect> effects)
        {
            // Hook: play cast animation/VFX here if you like.
            foreach (var ef in effects)
                if (ef != null)
                    yield return ef.Apply(caster, ability, ctx);
        }
    }
}
