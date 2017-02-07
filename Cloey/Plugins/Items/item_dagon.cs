using System;
using Cloey.Extensions;
using Cloey.Interfaces;
using Ensage;
using Ensage.Common;

namespace Cloey.Plugins.Items.Offense
{
    class item_dagon : Plugin
    {
        public override string PluginName => "Dagon";
        public override string TextureName => "item_dagon";
        public override ClassID ClassId => ObjectManager.LocalHero.ClassID;

        public override void OnUpdate()
        {
            var target = Me.GetTarget(1000, Root);
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
