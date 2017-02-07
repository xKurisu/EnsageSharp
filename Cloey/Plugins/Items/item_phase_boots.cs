using System;
using Cloey.Interfaces;
using Ensage;

namespace Cloey.Plugins.Items.Boots
{
    class item_phase_boots : Plugin
    {
        public override string PluginName => "Phase Boots";
        public override string TextureName => "item_phase_boots";
        public override ClassID ClassId => ObjectManager.LocalHero.ClassID;

        public override void OnUpdate()
        {

        }
    }
}
