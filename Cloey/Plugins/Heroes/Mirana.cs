using System;
using System.Collections.Generic;
using System.Linq;
using Cloey.Extensions;
using Cloey.Helpers;

using Ensage;
using Ensage.Common;
using Ensage.Common.Enums;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;

using SharpDX;

namespace Cloey.Plugins.Heroes
{
    internal class Mirana : Plugin
    {
        public override string PluginName => "Mirana";
        public override string TextureName => "npc_dota_hero_mirana";
        public override ClassID Id => ClassID.CDOTA_Unit_Hero_Mirana;

        internal static Ability MiranaArrow;
        internal static Ability MiranaLeap;
        internal static Ability MiranaStars;

        internal static Dictionary<float, float> Inputs = new Dictionary<float, float>();

        internal static string[] MixedSpells = { "mirana_arrow", "mirana_starfall" };
        internal static string[] ComboSpells = { "mirana_arrow", "mirana_starfall", /*"mirana_leap"*/ };
        internal static string[] FleeSpells = { "mirana_leap" };

        public bool MixedKeyDown => Menu.Parent?.Item("mixedkey").GetValue<KeyBind>().Active == true;
        public bool ComboKeyDown => Menu.Parent?.Item("combokey").GetValue<KeyBind>().Active == true;
        public bool FleeKeyDown => Menu?.Item("fleekey").GetValue<KeyBind>().Active == true;
        public bool JumpKeyDown => Menu?.Item("walljumpkey").GetValue<KeyBind>().Active == true;

        public override void OnLoadPlugin()
        {
            var cmenu = new Menu("Combo", "cmenu");
            cmenu.AddItem(new MenuItem("togglercombo", "Supported Spells: ")).SetFontColor(Color.LimeGreen)
                .SetValue(new AbilityToggler(ComboSpells.ToDictionary(x => x, x => true)));
            cmenu.AddItem(new MenuItem("chaincc", "Auto Chain Crowd Control")).SetValue(true).SetFontColor(Color.Orange);
            cmenu.AddItem(new MenuItem("kss", "Auto Q if Lethal")).SetValue(true).SetFontColor(Color.Orange);
            cmenu.AddItem(new MenuItem("blockks", "Block Auto Q in Combo")).SetValue(false);
            cmenu.AddItem(new MenuItem("twostars", "Starstorm Double Hit Only")).SetValue(false);
            cmenu.AddItem(new MenuItem("combomana", "Minimum Mana % to Use Combo")).SetValue(new Slider(15));
            Menu.AddSubMenu(cmenu);

            var mmenu = new Menu("Mixed", "mmenu");
            mmenu.AddItem(new MenuItem("togglermixed", "Supported Spells: ")).SetFontColor(Color.LimeGreen)
                .SetValue(new AbilityToggler(MixedSpells.ToDictionary(x => x, x => true)));
            mmenu.AddItem(new MenuItem("mixedmana", "Minimum Mana % to Use Harass")).SetValue(new Slider(55));
            Menu.AddSubMenu(mmenu);

            var fmenu = new Menu("Flee", "fmenu");
            fmenu.AddItem(new MenuItem("togglerflee", "Supported Beta Spells: ")).SetFontColor(Color.LimeGreen)
                .SetValue(new AbilityToggler(FleeSpells.ToDictionary(x => x, x => true)));

            fmenu.AddItem(new MenuItem("orbwalkwj", "Orbwalk with Walljump")).SetValue(true);
            fmenu.AddItem(new MenuItem("orbwalkmn", "Orbwalk with Leap")).SetValue(true);
            fmenu.AddItem(new MenuItem("fleekey", "Flee [active]")).SetValue(new KeyBind('G', KeyBindType.Press));
            fmenu.AddItem(new MenuItem("walljumpkey", "Walljump [active]")).SetValue(new KeyBind('T', KeyBindType.Press));
            Menu.AddSubMenu(fmenu);
        }

        public override void SetupSpells()
        {
            MiranaArrow = Me.GetAbilityById(AbilityId.mirana_arrow);
            MiranaLeap = Me.GetAbilityById(AbilityId.mirana_leap);
            MiranaStars = Me.GetAbilityById(AbilityId.mirana_starfall);
        }

        public override void OnUpdate()
        {
            if (!Utils.SleepCheck("LimitHeroUpdate"))
            {
                return;
            }

            if (FleeKeyDown)
            {
                TryLeap("togglerflee");
                return;
            }

            if (JumpKeyDown)
            {
                TryWallJump();
                return;
            }

            if (MixedKeyDown)
            {
                if (Me.Mana / Me.MaximumMana * 100 >= Menu.Item("mixedmana").GetValue<Slider>().Value)
                {
                    DoArrow("togglermixed");
                    DoStarfall("togglermixed");
                }
            }

            if (ComboKeyDown)
            {
                if (Me.Mana / Me.MaximumMana * 100 >= Menu.Item("combomana").GetValue<Slider>().Value)
                {
                    DoArrow("togglercombo");
                    ComboLeap();
                    DoStarfall("togglercombo");
                }

                if (Menu.Item("blockks").GetValue<bool>())
                {
                    return;
                }
            }

            if (Me.IsInvisible() == false)
            {
                ChainArrow();
                Killsteal();
            }
        }

        #region Tidy: DoLeap

        internal void TryLeap(string toggler)
        {
            if (Me.CanMove())
            {
                if (Menu.Item(toggler).GetValue<AbilityToggler>().IsEnabled("mirana_leap"))
                {
                    var jumpRange = new[] { 600, 700, 800, 900 } [Math.Min(0, MiranaLeap.Level - 1)];

                    var to = Me.InFront(jumpRange);

                    var proj = to.To2D().ProjectOn(Me.Position.To2D(), Game.MousePosition.To2D());
                    if (proj.IsOnSegment && Inputs.Count > 2)
                    {
                        if (MiranaLeap.CanBeCasted() && Inputs.Values.Last() - Me.Rotation < 25f)
                        {
                            if (Utils.SleepCheck("DoLeap"))
                            {
                                Me.Hold();
                                Inputs.Clear();
                                MiranaLeap.UseAbility();
                                Utils.Sleep(250, "DoLeap");
                            }
                        }
                    }

                    if (Menu.Item("orbwalkmn").GetValue<bool>() && Utils.SleepCheck("Move4"))
                    {
                        Me.Move(Game.MousePosition);
                        Inputs[Game.GameTime] = Me.Rotation;
                        Utils.Sleep(100, "Move4");
                    }
                }
            }
        }

        internal void ComboLeap()
        {

        }

        #endregion

        #region Tidy: DoArrow

        internal void DoArrow(string toggler)
        {
            var target = Me.GetTarget(1000 + Root.Item("predictionrange").GetValue<Slider>().Value / 2, Root);
            if (MiranaArrow.CanBeCasted() && Menu.Item(toggler).GetValue<AbilityToggler>().IsEnabled("mirana_arrow"))
            {   
                if (target.IsValidUnit(Root.Item("predictionrange").GetValue<Slider>().Value))
                {
                    var speed = MiranaArrow.GetAbilityData("arrow_speed");
                    var radius = MiranaArrow.GetAbilityData("arrow_width") + 35;

                    var distToHero = Me.NetworkPosition.To2D().Dist(target.NetworkPosition.To2D());
                    var distTime = (500 + Game.Ping) + (1000 * (distToHero / speed));

                    if (Root.Item("prediction").GetValue<StringList>().SelectedValue == "Zynox")
                    {
                        var zpredPosition = ZPrediction.PredictPosition(target, (int) distTime,
                            Root.Item("predictionallowturning2", true).GetValue<bool>());

                        if (zpredPosition != default(Vector3))
                        {
                            List<Unit> units;
                            if (MathUtils.CountInPath(Me.NetworkPosition, zpredPosition, radius, distToHero, out units, false) <= 1)
                            {
                                if (Utils.SleepCheck("MiranaW"))
                                {
                                    MiranaArrow.UseAbility(zpredPosition);
                                    Utils.Sleep(distTime + 250, "eul" + target.Handle);
                                    Utils.Sleep(distTime + Game.Ping, "MiranaW");
                                }
                            }
                            else
                            {
                                if (MiranaArrow.IsInAbilityPhase)
                                {
                                    Me.Hold();
                                    Utils.Sleep(125, "LimitHeroUpdate");
                                }
                            }
                        }

                        else
                        {
                            if (MiranaArrow.IsInAbilityPhase)
                            {
                                Me.Hold();
                                Utils.Sleep(125, "LimitHeroUpdate");
                            }
                        }
                    }
                    else
                    {
                        if (Utils.SleepCheck("MiranaW"))
                        {
                            MiranaArrow.CastSkillShot(target);
                            Utils.Sleep(distTime + 250, "eul" + target.Handle);
                            Utils.Sleep(distTime + Game.Ping, "MiranaW");
                        }
                    }
                }
            }
        }

        #endregion

        #region Tidy: DoStarfall

        internal void DoStarfall(string toggler)
        {
            var target = Me.GetTarget(950, Root);
            if (MiranaStars.CanBeCasted() && Menu.Item(toggler).GetValue<AbilityToggler>().IsEnabled("mirana_starfall"))
            {
                if (target.IsValidUnitFull(Menu.Item("twostars").GetValue<bool>() ? 450 : 660) && Utils.SleepCheck("StarfallCombo"))
                {
                    MiranaStars.UseAbility();
                    Utils.Sleep(350, "StarfallCombo");
                }
            }
        }

        #endregion

        #region Tidy: DoWalljump
        internal void TryWallJump()
        {
            // todo: improve & use rotation
            if (Me.CanMove())
            {
                var cursorPos = Game.MousePosition;

                var wallPoint = MathUtils.GetFirstWallPoint(Me.Position, cursorPos);
                if (wallPoint != default(Vector2))
                {
                    wallPoint = MathUtils.GetFirstWallPoint(wallPoint.To3D(), cursorPos, 5);  // more precision
                }

                var moveSpot = wallPoint != default(Vector2) ? wallPoint.To3D() : Game.MousePosition;
                if (wallPoint != default(Vector2))
                {
                    int minimumInputs = 2;
                    bool leapTriggered = false;

                    var wallPosition = moveSpot;
                    var dir2D = (cursorPos.To2D() - wallPosition.To2D()).Normalized();

                    var angle = 80f;
                    var step = angle / 20;
                    var curStep = 0f;
                    var curAngle = 0f;
                    var minWallWidth = 175f;
                    var jumpRange = new[] { 600, 700, 800, 900 } [Math.Min(0, MiranaLeap.Level - 1)];

                    while (true)
                    {
                        if (curStep > angle && curAngle < 0)
                        {
                            break;
                        }

                        if (curAngle == 0 || curAngle < 0)
                        {
                            if (curStep != 0)
                            {
                                curAngle = (curStep) * (float) Math.PI / 180;
                                curStep += step;
                            }
                            else if (curAngle > 0)
                            {
                                curAngle = -curAngle;
                            }
                        }
                        else if (curAngle > 0)
                        {
                            curAngle = -curAngle;
                        }

                        Vector3 checkPoint;

                        if (curStep == 0)
                        {
                            curStep = step;
                            checkPoint = wallPosition + dir2D.To3D() * jumpRange;
                        }
                        else
                        {
                            checkPoint = wallPosition + dir2D.Rotated(curAngle).To3D() * jumpRange;
                        }

                        if (NavMesh.GetCellFlags(checkPoint).HasFlag(NavMeshCellFlags.Walkable))
                        {
                            wallPoint = MathUtils.GetFirstWallPoint(checkPoint, wallPosition, 5);

                            if (wallPoint == default(Vector2))
                            {
                                continue;
                            }

                            var wallPointOpposite = MathUtils.GetFirstWallPoint(wallPoint.To3D(), wallPosition, 5).To3D();

                            var predictedRoute = ZPrediction.PredictMyRoute(Me, 1000, wallPointOpposite).ToList().To2D();
                            if (predictedRoute.PathLength() - Me.Position.Dist(wallPointOpposite) > minWallWidth + Me.HullRadius)
                            {
                                if (Me.Position.Dist(wallPointOpposite) < jumpRange - Me.HullRadius / 2)
                                {
                                    Me.Move(wallPosition);
                                    Inputs[Game.GameTime] = Me.Rotation;

                                    var to = Me.InFront(200);

                                    var proj = wallPointOpposite.To2D().ProjectOn(to.To2D(), cursorPos.To2D());
                                    if (proj.IsOnSegment && to.Dist(cursorPos) < Me.Position.Dist(cursorPos))
                                    {
                                        if (MiranaLeap.CanBeCasted())
                                        {
                                            Inputs.Clear();
                                            MiranaLeap.UseAbility();
                                            leapTriggered = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (!leapTriggered && Menu.Item("orbwalkwj").GetValue<bool>())
                    {
                        if (Utils.SleepCheck("Move2"))
                        {
                            Me.Move(Game.MousePosition);
                            Inputs[Game.GameTime] = Me.Rotation;
                            Utils.Sleep(100, "Move2");
                        }
                    }
                }
                else if (Menu.Item("orbwalkwj").GetValue<bool>())
                {
                    if (Utils.SleepCheck("Move3"))
                    {
                        Me.Move(Game.MousePosition);
                        Inputs[Game.GameTime] = Me.Rotation;
                        Utils.Sleep(100, "Move3");
                    }
                }
            }
        }

        #endregion

        #region Tidy: Misc Methods

        internal void ChainArrow()
        {
            if (MiranaArrow.CanBeCasted() && Menu.Item("chaincc").GetValue<bool>() && Utils.SleepCheck("MiranaAutoW"))
            {
                foreach (var hero in ObjectManager.GetEntities<Hero>().Where(x => x.IsValidUnit(Root.Item("predictionrange").GetValue<Slider>().Value)))
                {
                    Modifier modifier;

                    if (hero.IsValidUnit() && hero.IsDisabled(out modifier))
                    {
                        var speed = (int) MiranaArrow.GetAbilityData("arrow_speed");
                        var radius = (int) MiranaArrow.GetAbilityData("arrow_width") + 35;

                        var distToHero = Me.NetworkPosition.To2D().Dist(hero.NetworkPosition.To2D());
                        var distTime = (int) (500 + Game.Ping + (1000 * (distToHero / speed)));

                        var zpredPosition = ZPrediction.PredictDisabledPosition(hero, distTime);
                        if (zpredPosition != default(Vector3))
                        {
                            List<Unit> units;
                            if (MathUtils.CountInPath(Me.NetworkPosition, zpredPosition, radius, distToHero, out units, false) <= 1)
                            {
                                if (Utils.SleepCheck("MiranaAutoW"))
                                {
                                    MiranaArrow.UseAbility(zpredPosition);
                                    Utils.Sleep(distTime + 250, "eul" + hero.Handle);
                                    Utils.Sleep(distTime + Game.Ping, "MiranaAutoW");
                                }
                            }
                            else
                            {
                                if (MiranaArrow.IsInAbilityPhase)
                                {
                                    Me.Hold();
                                    Utils.Sleep(125, "LimitHeroUpdate");
                                }
                            }
                        }
                        else
                        {
                            if (MiranaArrow.IsInAbilityPhase)
                            {
                                Me.Hold();
                                Utils.Sleep(125, "LimitHeroUpdate");
                            }
                        }
                    }
                }
            }
        }

        internal void Killsteal()
        {
            return;
            if (MiranaStars.CanBeCasted() && Menu.Item("kss").GetValue<bool>() && Utils.SleepCheck("MiranaAutoKS"))
            {
                foreach (var hero in ObjectManager.GetEntities<Hero>().Where(x => x.IsValidUnitFull(650)))
                {
                    float damage;

                    damage = MiranaStars.GetDamage(Math.Min(0, MiranaStars.Level - 1));
                    damage *= Me.GetSpellAmp();

                    if (hero.Dist(Me.NetworkPosition) <= 450)
                    {
                        damage = damage * 2;
                    }

                    if (hero.IsValidUnitFull() && damage >= hero.Health)
                    {
                        MiranaStars.UseAbility();
                        Utils.Sleep(200, "MiranaAutoKS");
                    }
                }

                Utils.Sleep(125, "MiranaAutoKS");
            }
        }

        #endregion
    }
}


