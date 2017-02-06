using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cloey.Interfaces;
using Ensage;

namespace Cloey.Plugins.Items.Boots
{
    class item_phase_boots : Plugin
    {
        public override string PluginName => "Phase Boots";
        public override string TextureName => "item_phase_boots";
        public override ClassID ClassId => ObjectManager.LocalHero.ClassID;

        public item_phase_boots()
        {
            
        }
    }
}
