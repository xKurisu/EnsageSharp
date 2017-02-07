using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cloey.Extensions;
using Cloey.Interfaces;
using Ensage;
using Ensage.Common;

namespace Cloey.Plugins.Items.Offense
{
    class item_shivas_guard : Plugin
    {
        public override string PluginName => "Shiva's Guard";
        public override string TextureName => "item_shivas_guard";
        public override ClassID ClassId => ObjectManager.LocalHero.ClassID;

        public override void OnUpdate()
        {
            var target = Me.GetTarget(1000, Root);
            if (target.IsValidUnit())
            {
                if (Utils.SleepCheck("Use" + TextureName))
                {
                    Item.UseAbility();
                    Utils.Sleep(300, "Use" + TextureName);
                }
            }
        }
    }
}
