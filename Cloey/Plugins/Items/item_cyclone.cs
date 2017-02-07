using System;
using Cloey.Extensions;
using Cloey.Interfaces;
using Ensage.Common;
using Ensage.Common.Extensions;

namespace Cloey.Plugins.Items.Offense
{
    class item_cyclone : Plugin
    {
        public override string PluginName => "Eul's Scepter";
        public override string TextureName => "item_cyclone";

        public override void OnUpdate()
        {
            var target = Me.GetTarget(575, Root);
            if (target.IsValidUnit())
            {
                if (Utils.SleepCheck("Use" + TextureName))
                {
                    Item.UseAbility(target);
                    Utils.Sleep(500, "Use" + TextureName);
                }
            }
        }
    }
}
