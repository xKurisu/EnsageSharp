﻿namespace Cloey.Plugins.Orbwalkers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Ensage;
    using Ensage.Common;
    using Ensage.Common.Extensions;
    using Ensage.Common.Extensions.SharpDX;
    using Ensage.Common.Menu;


    using orb = Ensage.Common.Objects.UtilityObjects.Orbwalker;

    /// <summary>
    ///     The orb walker.
    /// </summary>
    internal class Orbwalker : Plugin
    {
        #region Virtual Fields

        public override string PluginName => "Orbwalker";
        public override string TextureName => "moones";
        public override ClassID Id => ObjectManager.LocalHero.ClassID;

        #endregion

        #region Static Fields

        /// <summary>
        ///     The range display.
        /// </summary>
        private static readonly RangeDrawing RangeDisplay = new RangeDrawing();

        /// <summary>
        ///     The controllable units.
        /// </summary>
        private static ControllableUnits controllableUnits;

        /// <summary>
        ///     The creep target.
        /// </summary>
        private static Unit creepTarget;

        /// <summary>
        ///     The me.
        /// </summary>
        private static Hero me;

        /// <summary>
        ///     The orbwalker dictionary.
        /// </summary>
        private static Dictionary<float, orb> orbwalkerDictionary;

        /// <summary>
        ///     The target.
        /// </summary>
        private static Hero target;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The initialization
        /// </summary>
        public override void OnLoadPlugin()
        {
            Menu.AddItem(new MenuItem("chaseKey", "Chase Key").SetValue(new KeyBind(32, KeyBindType.Press)));
            Menu.AddItem( new MenuItem("allUnitsChaseKey", "All units Key").SetValue(new KeyBind('N', KeyBindType.Press)));
            Menu.AddItem(new MenuItem("kiteKey", "Kite Key").SetValue(new KeyBind('V', KeyBindType.Press)));
            Menu.AddItem(new MenuItem("farmKey", "Farm Key").SetValue(new KeyBind('B', KeyBindType.Press)));
            Menu.AddItem(new MenuItem("bonusWindup", "Bonus WindUp time on kitting").SetValue(new Slider(500, 100, 2000)).SetTooltip("Time between attacks in kitting mode"));

            OnLoad();
        }

        #endregion

        #region Methods

        /// <summary>
        ///     The events_ on close.
        /// </summary>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="e">
        ///     The e.
        /// </param>
        public override void OnClose()
        {
            Events.OnUpdate -= Game_OnUpdate;
            target = null;
            RangeDisplay.Dispose();
        }

        /// <summary>
        ///     The events_ on load.
        /// </summary>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="e">
        ///     The e.
        /// </param>
        private void OnLoad()
        {
            me = ObjectManager.LocalHero;
            controllableUnits = new ControllableUnits(me.Team);
            orbwalkerDictionary = new Dictionary<float, orb>();
            target = null;
            RangeDisplay.Dispose();
            Events.OnUpdate += Game_OnUpdate;
        }

        /// <summary>
        ///     The game_ on update.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        private  void Game_OnUpdate(EventArgs args)
        {
            RangeDisplay.Me();

            if (Game.IsPaused)
            {
                return;
            }

            if (RangeDisplay.IsDisposed())
            {
                if (me.IsAlive)
                {
                    RangeDisplay.Create();
                }
            }
            else
            {
                if (!me.IsAlive)
                {
                    RangeDisplay.Dispose();
                }
                else if (!RangeDisplay.IsUpdated())
                {
                    RangeDisplay.Update();
                }
            }

            if (target != null && (!target.IsValid || !target.IsVisible || !target.IsAlive || target.Health <= 0))
            {
                target = null;
            }

            var canCancel = Orbwalking.CanCancelAnimation();

            var isFarmKeyDown = Menu.Item("farmKey").GetValue<KeyBind>().Active;
            var allUnitsKeyDown = Menu.Item("allUnitsChaseKey").GetValue<KeyBind>().Active;

            if (canCancel)
            {
                if (Utils.SleepCheck("Orbwalker.Update.Target"))
                {
                    if (allUnitsKeyDown)
                    {
                        target = me.ClosestToMouseTarget(500);
                    }
                    else if (target != null && !target.IsVisible)
                    {
                        target = me.ClosestToMouseTarget(128);
                    }
                    else
                    {
                        var bestAa = me.BestAATarget();
                        if (bestAa != null)
                        {
                            target = me.BestAATarget();
                        }
                    }

                    Utils.Sleep(500, "Orbwalker.Update.Target");
                }
            }

            if (canCancel && creepTarget != null
                && (!creepTarget.IsValid || !creepTarget.IsSpawned || creepTarget.Health <= 0 || !creepTarget.IsAlive
                    || !creepTarget.IsVisible))
            {
                creepTarget = null;
            }

            if (Utils.SleepCheck("Orbwalker.Update.Creep") && !Game.IsChatOpen
                 && isFarmKeyDown
                 && (canCancel || creepTarget == null || !creepTarget.IsValid || !creepTarget.IsSpawned
                     || creepTarget.Health <= 0 || !creepTarget.IsAlive || !creepTarget.IsVisible))
            {
                creepTarget =
                    ObjectManager.GetEntitiesParallel<Creep>()
                        .Where(
                            x =>
                            x.IsSpawned && x.IsValid && x.IsAlive && x.Team != me.Team
                            && x.Distance2D(me) < me.GetAttackRange() + 150)
                        .MinOrDefault(x => x.Health + x.Distance2D(me) * 1.5);
                Utils.Sleep(230, "Orbwalker.Update.Creep");
            }

            if (Game.IsChatOpen)
            {
                return;
            }

            if (allUnitsKeyDown)
            {
                Orbwalking.Orbwalk(target, attackmodifiers: true);
                foreach (var unit in controllableUnits.Units.Where(x => !x.Equals(me) && x.IsValid && x.IsAlive))
                {
                    orb unitOrbwalker;
                    if (!orbwalkerDictionary.TryGetValue(unit.Handle, out unitOrbwalker))
                    {
                        unitOrbwalker = new orb(unit);
                        orbwalkerDictionary.Add(unit.Handle, unitOrbwalker);
                    }

                    var position = Game.MousePosition;
                    if (target != null)
                    {
                        position = Game.MousePosition.Extend(
                            target.Position,
                            (float)(Game.MousePosition.Distance2D(target) * 0.7));
                    }

                    unitOrbwalker.OrbwalkOn(target, position);
                }

                return;
            }

            if (isFarmKeyDown)
            {
                Orbwalking.Orbwalk(creepTarget);
                return;
            }

            if (Menu.Item("chaseKey").GetValue<KeyBind>().Active)
            {
                Orbwalking.Orbwalk(target, attackmodifiers: true);
                return;
            }

            if (Menu.Item("kiteKey").GetValue<KeyBind>().Active)
            {
                Orbwalking.Orbwalk(
                    target,
                    attackmodifiers: true,
                    bonusWindupMs: Menu.Item("bonusWindup").GetValue<Slider>().Value);
            }
        }

        #endregion
    }
}