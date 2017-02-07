using System;
using Cloey.Extensions;
using Cloey.Interfaces;
using Ensage.Common;
using Ensage.Common.Extensions;

namespace Cloey.Plugins.Items.Offense
{
    class item_shivas_guard : Plugin
    {
        public override string PluginName => "Shiva's Guard";
        public override string TextureName => "item_shivas_guard";

        public override void OnUpdate()
        {
            var target = Me.GetTarget(900, Root);
            if (target.IsValidUnit())
            {
                var eth = Me.FindItem("item_ethereal_blade");
                if (eth != null && eth.CanBeCasted())
                {
                    return;
                }

                if (Utils.SleepCheck("Use" + TextureName))
                {
                    Item.UseAbility();
                    Utils.Sleep(300, "Use" + TextureName);
                }
            }
        }
    }
}
