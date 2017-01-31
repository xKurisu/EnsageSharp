using System;
using System.Collections.Generic;
using System.Linq;

using Cloey.Extensions;
using Cloey.Helpers;
using Cloey.Interfaces;

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
        public override string TextureName => "mirana";
        public override ClassID ClassId => ClassID.CDOTA_Unit_Hero_Mirana;

        internal static Ability MiranaArrow;
        internal static Ability MiranaLeap;
        internal static Ability MiranaStars;

        internal static Dictionary<float, float> Inputs = new Dictionary<float, float>();
        internal static string[] ComboSpells = { "mirana_arrow", "mirana_starfall" };
        internal static string[] FleeSpells = { "mirana_leap" };

        public bool ComboKeyDown => Menu != null && Menu.Item("combokey").GetValue<KeyBind>().Active;
        public bool FleeKeyDown => Menu != null && Menu.Item("fleekey").GetValue<KeyBind>().Active;
        public bool JumpKeyDown => Menu != null && Menu.Item("walljumpkey").GetValue<KeyBind>().Active;

        public override void OnLoadPlugin()
        {
            var tmenu = new Menu("Targeting", "tmenu");
            tmenu.AddItem(new MenuItem("targeting", "Mode:"))
                .SetValue(new StringList(new[] { "Mouse", "FastestKill" }, 1));
            Menu.AddSubMenu(tmenu);

            var pmenu = new Menu("Prediction", "prediction");
            pmenu.AddItem(new MenuItem("prediction", "Prediction: ")).SetValue(new StringList(new[] {"Common", "Zynox"}));
            pmenu.AddItem(new MenuItem("predictionrange", "Max Range: ")).SetValue(new Slider(1800, 1000, 3000));
            Menu.AddSubMenu(pmenu);

            var cmenu = new Menu("Combo", "cmenu");
            cmenu.AddItem(new MenuItem("toggler", "Supported Spells: "))
                .SetValue(new AbilityToggler(ComboSpells.ToDictionary(x => x, x => true)));
            cmenu.AddItem(new MenuItem("chaincc", "Auto Chain CC")).SetValue(true).SetFontColor(Color.Fuchsia);
            cmenu.AddItem(new MenuItem("killsteal", "Auto Killsteal")).SetValue(true).SetFontColor(Color.Fuchsia);
            cmenu.AddItem(new MenuItem("combokey", "Combo [active]")).SetValue(new KeyBind(32, KeyBindType.Press));
            Menu.AddSubMenu(cmenu);

            var fmenu = new Menu("Flee", "fmenu");
            fmenu.AddItem(new MenuItem("toggler2", "Leaping is still in Beta: ")).SetFontColor(Color.Fuchsia)
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
            if (FleeKeyDown)
            {
                TryLeap();
                return;
            }

            if (JumpKeyDown)
            {
                TryWallJump();
                return;
            }

            if (ComboKeyDown)
            {
                DoArrow();
                //DoStarfall();
                return;
            }

            if (Me.IsInvisible() == false)
            {
                ChainArrow();
                Killsteal();
            }
        }

        #region Tidy: DoWalljump
        internal void TryWallJump()
        {
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
                            wallPoint = MathUtils.GetFirstWallPoint(checkPoint, wallPosition, 15);

                            if (wallPoint == default(Vector2))
                            {
                                continue;
                            }

                            var wallPointOpposite =
                                MathUtils.GetFirstWallPoint(wallPoint.To3D(), wallPosition, 5).To3D();

                            var predictedRoute = Me.PredictRoute(1000, wallPointOpposite).ToList().To2D();
                            if (predictedRoute.PathLength() - Me.Position.Dist(wallPointOpposite) > minWallWidth + Me.HullRadius)
                            {
                                if (Me.Position.Dist(wallPointOpposite) < jumpRange - Me.HullRadius / 2)
                                {
                                    Me.Move(wallPosition);
                                    Inputs[Game.GameTime] = Me.Rotation;

                                    var to = Me.InFront(200);
                                    var proj = wallPointOpposite.To2D().Intersects(to.To2D(), cursorPos.To2D());

                                    if (proj.IsInside && to.Dist(cursorPos) < Me.Position.Dist(cursorPos))
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

        #region Tidy: DoLeap

        internal void TryLeap()
        {
            if (Me.CanMove())
            {
                if (Menu.Item("toggler2").GetValue<AbilityToggler>().IsEnabled("mirana_leap"))
                {
                    var jumpRange = new[] { 600, 700, 800, 900 } [Math.Min(0, MiranaLeap.Level - 1)];

                    var to = Me.InFront(jumpRange);

                    var proj = to.To2D().Intersects(Me.Position.To2D(), Game.MousePosition.To2D());
                    if (proj.IsInside && Inputs.Count > 2)
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

        #endregion

        #region Tidy: DoArrow

        internal void DoArrow()
        {
            var target = Me.GetTarget(1000 + Menu.Item("predictionrange").GetValue<Slider>().Value / 2, Menu);
            if (MiranaArrow.CanBeCasted() && Menu.Item("toggler").GetValue<AbilityToggler>().IsEnabled("mirana_arrow"))
            {   
                if (target.IsValidUnit(Menu.Item("predictionrange").GetValue<Slider>().Value))
                {
                    var speed = MiranaArrow.GetAbilityData("arrow_speed");
                    var radius = MiranaArrow.GetAbilityData("arrow_width") + 35;

                    var mhmk = Me.NetworkPosition.Dist(target.NetworkPosition);
                    var distTime = (550 + Game.Ping) + (1000 * (Me.NetworkPosition.Dist(target.NetworkPosition) / speed));

                    if (Menu.Item("prediction").GetValue<StringList>().SelectedValue == "Zynox")
                    {
                        var zpredPosition = ZPrediction.PredictPosition(target, (int)distTime, mhmk > 1800);
                        if (zpredPosition != default(Vector3))
                        {
                            List<Unit> units;
                            if (MathUtils.CountInPath(Me.NetworkPosition, zpredPosition, radius, mhmk, out units, false) <= 1)
                            {
                                if (Utils.SleepCheck("MiranaW"))
                                {
                                    MiranaArrow.UseAbility(zpredPosition);
                                    Utils.Sleep(distTime + Game.Ping, "MiranaW");
                                }
                            }
                        }
                    }
                    else
                    {
                        if (Utils.SleepCheck("MiranaW"))
                        {
                            MiranaArrow.CastSkillShot(target);
                            Utils.Sleep(distTime + Game.Ping, "MiranaW");
                        }
                    }
                }
            }
        }

        #endregion

        #region Tidy: DoStarfall

        internal void DoStarfall()
        {
            var target = Me.GetTarget(750, Menu);
            if (MiranaStars.CanBeCasted() && Menu.Item("toggler").GetValue<AbilityToggler>().IsEnabled("mirana_starfall"))
            {
                if (target.IsValidUnit(650) && Utils.SleepCheck("StarfallCombo"))
                {
                    MiranaStars.UseAbility();
                    Utils.Sleep(350, "StarfallCombo");
                }
            }
        }

        #endregion

        #region Tidy: Misc Methods

        internal void ChainArrow()
        {
            if (MiranaArrow.CanBeCasted() && Menu.Item("chaincc").GetValue<bool>() && Utils.SleepCheck("MiranaAutoW"))
            {
                foreach (var hero in ObjectManager.GetEntities<Hero>().Where(x => x.IsValidUnit(Menu.Item("predictionrange").GetValue<Slider>().Value)))
                {
                    if (hero.IsValidUnit() && hero.IsImmobile() && hero.Team != Me.Team)
                    {
                        if (MiranaArrow.CastStun(hero))
                        {
                            Utils.Sleep(350, "MiranaAutoW");
                            return;
                        }
                    }
                }

                Utils.Sleep(125, "MiranaAutoW");
            }
        }

        internal void Killsteal()
        {
            if (MiranaArrow.CanBeCasted() && Menu.Item("killsteal").GetValue<bool>() && Utils.SleepCheck("MiranaAutoKS"))
            {
                foreach (var hero in ObjectManager.GetEntities<Hero>().Where(x => x.IsValidUnit(Menu.Item("predictionrange").GetValue<Slider>().Value)))
                {
                    float damage;

                    damage = MiranaArrow.GetDamage(Math.Min(0, MiranaArrow.Level - 1));
                    damage *= Me.GetSpellAmp();

                    if (hero.IsValidUnit() && hero.Health <= damage)
                    {
                        var speed = MiranaArrow.GetAbilityData("arrow_speed");
                        var radius = MiranaArrow.GetAbilityData("arrow_width") + 35;

                        var distToHero = Me.NetworkPosition.Dist(hero.NetworkPosition);
                        var distTime = (550 + Game.Ping) + (1000 * (Me.NetworkPosition.Dist(hero.NetworkPosition) / speed));

                        if (Menu.Item("prediction").GetValue<StringList>().SelectedValue == "Zynox")
                        {
                            var zpredPosition = ZPrediction.PredictPosition(hero, (int) distTime, distToHero > 1800);
                            if (zpredPosition != default(Vector3))
                            {
                                List<Unit> units;
                                if (MathUtils.CountInPath(Me.NetworkPosition, zpredPosition, radius, distToHero, out units, false) <= 1)
                                {
                                    MiranaArrow.UseAbility(zpredPosition);
                                    Utils.Sleep(distTime + Game.Ping, "MiranaAutoKS");
                                    return;
                                }
                            }
                        }
                        else
                        {
                            MiranaArrow.CastSkillShot(hero);
                            Utils.Sleep(distTime + Game.Ping, "MiranaAutoKS");
                            return;
                        }
                    }
                }

                Utils.Sleep(125, "MiranaAutoKS");
            }
        }
        #endregion
    }
}


