using System;
using Cloey.Extensions;
using Cloey.Helpers;
using Cloey.Interfaces;
using Ensage;
using Ensage.Common;
using SharpDX;

namespace Cloey.Plugins.Items.Offense
{
    class item_cyclone : Plugin
    {
        public override string PluginName => "Eul's Scepter";
        public override string TextureName => "item_cyclone";

        public override void OnUpdate()
        {
            var target = Me.GetTarget(575, Root);
            if (target.IsValidUnit() && !target.ModifierActive("modifier_item_ethereal_blade_ethereal"))
            {
                if (Utils.SleepCheck("Use" + TextureName) && Utils.SleepCheck("eul" + target.Handle))
                {
                    Modifier m;
                    if (target.IsDisabled(out m))
                    {
                        var validPos = ZPrediction.PredictDisabledPosition(target, Game.Ping);
                        if (validPos != default(Vector3))
                        {
                            Item.UseAbility(target);
                            Utils.Sleep(500, "Use" + TextureName);
                            Utils.Sleep(550, "LimitHeroUpdate");
                        }
                    }
                    else
                    {
                        Item.UseAbility(target);
                        Utils.Sleep(500, "Use" + TextureName);
                        Utils.Sleep(550, "LimitHeroUpdate");
                    }
                }
            }
        }
    }
}
