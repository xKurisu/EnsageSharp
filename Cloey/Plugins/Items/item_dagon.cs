using System;
using Cloey.Extensions;
using Cloey.Interfaces;
using Ensage.Common;
using Ensage.Common.Extensions;

namespace Cloey.Plugins.Items.Offense
{
    class item_dagon : Plugin
    {
        public override string PluginName => "Dagon";
        public override string TextureName => "item_dagon";

        public override void OnUpdate()
        {
            if (Item != null)
            {
                var target = Me.GetTarget(1000, Root);
                if (target.IsValidUnit(new[] { 600, 650, 700, 750, 800 } [Math.Min(0, Item.Level - 1)]))
                {
                    var eth = Me.FindItem("item_ethereal_blade");
                    if (eth != null && eth.CanBeCasted())
                    {
                        if (Menu.Parent.Item("item_ethereal_bladeenabled").GetValue<bool>())
                        {
                            return;
                        }
                    }

                    if (Utils.SleepCheck("Use" + TextureName))
                    {
                        Item.UseAbility(target);
                        Utils.Sleep(300, "Use" + TextureName);
                    }
                }
            }
        }
    }
}
