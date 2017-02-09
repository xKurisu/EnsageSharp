using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;

using Cloey.Extensions;
using Cloey.Interfaces;

using Ensage;


namespace Cloey.Plugins.Heroes.Beta
{
    class Squad
    {
        public int Id;
        public float Stamp;
        public Unit Unit;
        public Vector2 FormationPosition;
    }

    class NagaSiren : Plugin
    {
        internal static int Index;
        public override string PluginName => "Naga Siren";
        public override string TextureName => "npc_dota_hero_naga_siren";
        public override ClassID ClassId => ClassID.CDOTA_Unit_Hero_Naga_Siren;
        public override bool IsHeroPlugin => true;

        public static Ability MirrorImage;
        public static Dictionary<uint, Squad> NagaSquad = new Dictionary<uint, Squad>();

        public override void OnLoadPlugin()
        {
            ObjectManager.OnAddEntity += ObjectManager_OnAddEntity;
            ObjectManager.OnRemoveEntity += ObjectManager_OnRemoveEntity;
            Unit.OnModifierRemoved += Hero_OnModifierRemoved;
            Player.OnExecuteOrder += Player_OnExecuteOrder;
        }

        private void Player_OnExecuteOrder(Player sender, ExecuteOrderEventArgs args)
        {
            if (sender.Handle == Player.Handle)
            {
                HandleFormation(args.TargetPosition, 200 + Me.HullRadius);
            }
        }

        private void Hero_OnModifierRemoved(Unit sender, ModifierChangedEventArgs args)
        {
            foreach (var entry in NagaSquad)
            {
                var unit = entry.Value.Unit;
                if (unit.NetworkName == Me.NetworkName && unit.Handle == sender.Handle)
                {
                    if (unit.Handle != Me.Handle)
                    {
                        if (args.Modifier.Name == "modifier_illusion")
                        {
                            RemoveUnit(unit.Handle); // ?
                            break;
                        }
                    }
                }
            }
        }

        private void ObjectManager_OnRemoveEntity(EntityEventArgs args)
        {
            var unit = args.Entity as Unit;
            if (unit != null)
            {
                if (unit.NetworkName == Me.NetworkName && unit.Handle != Me.Handle)
                {
                    RemoveUnit(unit.Handle);
                }
            }
        }

        private void ObjectManager_OnAddEntity(EntityEventArgs args)
        {
            var unit = args.Entity as Unit;
            if (unit != null)
            {
                if (unit.NetworkName == Me.NetworkName && unit.Handle != Me.Handle)
                {
                    AddUnit(unit);
                }
            }
        }

        public void AddUnit(Unit unit)
        {
            if (!NagaSquad.ContainsKey(unit.Handle))
            {
                Index++;
                NagaSquad[unit.Handle] = new Squad { Id = Index, Unit = unit, Stamp = Game.GameTime };
            }
        }

        public void RemoveUnit(uint handle)
        {
            if (NagaSquad.ContainsKey(handle))
            {
                Index--;
                NagaSquad.Remove(handle); 
            }
        }

        public override void OnUpdate()
        {
            HandleInvalidIllusions();
        }

        public void HandleFormation(Vector3 origin, float radius)
        {
            if (!NagaSquad.Any())
            {
                return;
            }

            var count = NagaSquad.Count;
            var constant = Math.PI / 2 - Math.PI / count;

            foreach (var entry in NagaSquad)
            {
                var squad = entry.Value;
                var unit = entry.Value.Unit;

                var xpos = (float) (origin.X + radius * Math.Cos(squad.Id * 2 * Math.PI / count + constant));
                var ypos = (float) (origin.Y + radius * Math.Sin(squad.Id * 2 * Math.PI / count + constant));

                squad.FormationPosition = new Vector2(xpos, ypos);

                if (squad.FormationPosition != default(Vector2))
                {
                    if (!unit.IsControlledByMe())
                    {
                        // test
                        unit.Move(squad.FormationPosition.To3D());
                    }
                }
            }
        }

        public void HandleInvalidIllusions()
        {
            foreach (var entry in NagaSquad)
            {
                var unit = entry.Value.Unit;
                if (unit.IsValid == false || unit.IsAlive == false || unit.IsVisible == false)
                {
                    RemoveUnit(entry.Key);
                    break;
                }

                var timestamp = entry.Value.Stamp;
                if (Game.GameTime - timestamp > 25f)
                {
                    RemoveUnit(entry.Key);
                    break;
                }
            }
        }
    }
}
