using Ensage;
using Ensage.Common.Menu;
using Ensage.Common.Enums;
using Ensage.Common.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using Cloey.Helpers;
using Ensage.Common.Objects;
using SharpDX;

namespace Cloey.Extensions
{
    internal static class UnitUtils
    {
        #region Tidy: Aura Mechanics

        internal static bool ModifierActive(this Unit unit, string modifier)
        {
            return unit.GetModifier(modifier) != null;
        }

        internal static bool HasAnyModifiers(this Unit unit, HashSet<string> modifiers)
        {
            return unit.GetModifier(modifiers) != null;
        }

        internal static Modifier GetModifier(this Unit unit, string modifierName)
        {
            return unit.HasModifier(modifierName) ? unit.Modifiers.FirstOrDefault(x => modifierName == x.Name) : null;
        }

        internal static Modifier GetModifier(this Unit unit, HashSet<string> modifiers)
        {
            return modifiers.Where(unit.HasModifier).Select(unit.GetModifier).FirstOrDefault();
        }

        internal static bool IsDisabled(this Unit unit, out Modifier modifier, bool includeInvulnerable = true)
        {
            Modifier invulnerableModifier = null;

            var rootModifier = unit.GetModifier(ModifierData.RootModifiers);
            var vulnerableModifier = unit.GetModifier(ModifierData.VulnerableStunModifiers);

            if (includeInvulnerable)
            {
                invulnerableModifier = unit.GetModifier(ModifierData.InvulnerableStunModifiers);
            }

            if (vulnerableModifier != null)
            {
                modifier = vulnerableModifier;
                return true;
            }

            if (invulnerableModifier != null)
            {
                modifier = invulnerableModifier;
                return true;
            }

            if (rootModifier != null)
            {
                modifier = rootModifier;
                return true;
            }

            modifier = null;
            return false;
        }

        #endregion

        #region Tidy: ValidUnit

        internal static bool IsValidUnit(this Unit u, float range = float.MaxValue, bool checkTeam = true, Vector3 from = default(Vector3))
        {
            return u != null && u.IsVisible && u.IsValid && !u.IsIllusion && u.IsAlive && u.IsSelectable &&
                   u.Position.Dist(from != default(Vector3) ? from : ObjectManager.LocalHero.NetworkPosition, true) <=
                   range * range && (u.Team != ObjectManager.LocalHero.Team || !checkTeam);
        }

        internal static bool IsValidUnitFull(this Unit u, float range = float.MaxValue, bool checkTeam = true, Vector3 from = default(Vector3))
        {
            return u != null && u.IsVisible && u.IsValid && !u.IsIllusion && u.IsAlive && u.IsSelectable &&
                   u.HasAnyModifiers(ModifierData.InvulnerableStunModifiers) == false &&
                   u.HasAnyModifiers(ModifierData.CantExecuteModifiers) == false &&
                   u.HasAnyModifiers(ModifierData.CantAttackModifiers) == false &&
                   u.Position.Dist(from != default(Vector3) ? from : ObjectManager.LocalHero.NetworkPosition, true) <=
                   range * range && (u.Team != ObjectManager.LocalHero.Team || !checkTeam);
        }


        internal static bool IsControlledByMe(this Unit unit)
        {
            var source = ObjectManager.LocalPlayer;
            return source != null && source.Selection.Where(x => (x as Unit).IsValidUnit(float.MaxValue, false)).Contains(unit);
        }

        internal static bool IsControlledByPlayer(this Unit unit, Player player)
        {
            var source = player;
            return source != null && source.Selection.Where(x => (x as Unit).IsValidUnit(float.MaxValue, false)).Contains(unit);
        }

        #endregion

        #region Tidy: Misc

        internal static Hero GetTarget(this Hero me, float range, Menu menu)
        {
            // todo: finish/new target selector currently using a common edit
            var num1 = me.MinimumDamage + me.BonusDamage;
            var num2 = 0.0f;

            Hero target = null;

            if (menu.Item("targeting").GetValue<StringList>().SelectedIndex == 0)
                return Heroes.All.Where(x => x.IsValidUnit(range)).OrderBy(x => x.Dist(me.Position)).FirstOrDefault();

            foreach (Hero hero2 in Heroes.All.Where(x => x.IsValidUnit(range)))
                foreach (var hero in ObjectManager.GetEntitiesFast<Hero>().Where(x => IsValidUnit(x, range)))
                {
                    var num3 = hero2.DamageTaken(num1, DamageType.Physical, me);
                    var num4 = hero2.Health / num3;

                    if (target == null || num2 > num4)
                    {
                        target = hero2;
                        num2 = num4;
                    }

                }

            return target;
        }

        public static string GetHeroName(this string n)
        {
            return n.Substring(n.LastIndexOf("o_", StringComparison.Ordinal) + 2);
        }

        public static float GetSpellAmp(this Hero hero)
        {

            var spellAmp = (100.0f + hero.TotalIntelligence / 16.0f) / 100.0f;

            var aether = hero.GetItemById(ItemId.item_aether_lens);
            if (aether != null)
            {
                spellAmp += aether.AbilitySpecialData.First(x => x.Name == "spell_amp").Value / 100.0f;
            }

            var talent = hero.Spellbook.Spells.FirstOrDefault(x => x.Level > 0 && x.Name.StartsWith("special_bonus_spell_amplify_"));
            if (talent != null)
            {
                spellAmp += talent.AbilitySpecialData.First(x => x.Name == "value").Value / 100.0f;
            }

            return spellAmp;
        }

        #endregion
    }
}
