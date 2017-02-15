using Ensage;
using Ensage.Common;
using Cloey.Extensions;
using Cloey.Helpers;
using SharpDX;

namespace Cloey.Plugins.Items.Offense
{
    class item_ethereal_blade : Plugin
    {
        public override string PluginName => "Ethereal Blade";
        public override string TextureName => "item_ethereal_blade";

        public override void OnUpdate()
        {
            var target = Me.GetTarget(800, Root);
            if (target.IsValidUnit())
            {
                if (Utils.SleepCheck("Use" + TextureName))
                {
                    Item.UseAbility(target);
                    Utils.Sleep(300, "Use" + TextureName);
                }
            }
        }
    }
}
