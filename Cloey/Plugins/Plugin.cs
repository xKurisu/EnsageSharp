using Cloey.Helpers;

using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;

using System;
using System.Collections.Generic;
using System.Linq;


namespace Cloey.Plugins
{
    internal class Plugin
    {
        public virtual string PluginName { get; set; }
        public virtual string TextureName { get; set; }
        public virtual ClassID Id { get; set; } = ObjectManager.LocalHero.ClassID;

        public Menu Root { get; private set; }
        public Menu Menu { get; private set; }
        public Hero Me => ObjectManager.LocalHero;
        public Player Player => ObjectManager.LocalPlayer;
        public Item Item { get; private set; }

        private int pNow;
        private PluginType pType;
        private Dictionary<string, bool> pDict = new Dictionary<string, bool>();

        // todo: redo initializer
        public Plugin Init(Menu root, Menu origin)
        {
            try
            {
                if (TextureName.Contains("npc_dota_hero"))
                {
                    Root = origin;
                    Menu = new Menu(PluginName, TextureName + "main");

                    pType = PluginType.Hero;
                    SetupSpells();
                    OnLoadPlugin();
                    root.AddSubMenu(Menu);

                    if (!pDict.ContainsKey("Init"))
                    {
                        Events.OnClose += Events_OnClose;
                        Game.OnIngameUpdate += Game_OnUpdate;
                        pDict["Init"] = true;
                    }
                }

                // orbwalkers
                var orbwalker = root.Item("orbwalkmode");
                if (orbwalker != null)
                {
                    var selected = orbwalker.GetValue<StringList>().SelectedValue;
                    if (selected.ToLower() == TextureName.ToLower())
                    {
                        Root = origin;
                        Menu = new Menu(PluginName, TextureName + "root");

                        pType = PluginType.Orbwalker;
                        SetupSpells();
                        OnLoadPlugin();
                        root.AddSubMenu(Menu);

                        if (!pDict.ContainsKey("Init"))
                        {
                            Events.OnClose += Events_OnClose;
                            Game.OnIngameUpdate += Game_OnUpdate;
                            pDict["Init"] = true;
                        }
                    }
                }

                // items
                if (TextureName.Contains("item"))
                {
                    Root = origin;
                    var uniqueTexture = TextureName == "item_dagon" ? "item_dagon_5" : TextureName;

                    Menu = new Menu("  " + PluginName, TextureName + "root", false, uniqueTexture, true);
                    Menu.AddItem(new MenuItem(TextureName + "enabled", "Enabled: ")).SetValue(true);
                    Menu.AddItem(new MenuItem(TextureName + "mode", "Mode: ")).SetValue(new StringList(new[] { "Combo", "Always" }));

                    pType = PluginType.Item;
                    SetupSpells();
                    OnLoadPlugin();
                    root.AddSubMenu(Menu);

                    if (!pDict.ContainsKey("Init"))
                    {
                        Events.OnClose += Events_OnClose;
                        Game.OnIngameUpdate += Game_OnUpdate;
                        pDict["Init"] = true;
                    }
                }
            }

            catch (Exception e)
            {
                Console.WriteLine(e);
                Game.PrintMessage("<font color=\"#FFF280\">Exception thrown at Plugin.Init: </font>: " + e.Message);
            }

            return this;
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (Game.IsPaused || !Game.IsInGame || Game.IsChatOpen)
            {
                return;
            }

            if (!Me.IsAlive || Me.IsChanneling())
            {
                return;
            }

            if (Environment.TickCount - pNow >= 0) 
            {
                switch (pType)
                {
                    case PluginType.Item:
                        var combo = Root != null && Root.Item("combokey").GetValue<KeyBind>().Active;
                        if (combo || Menu.Item(TextureName + "mode").GetValue<StringList>().SelectedIndex != 0)
                        {
                            var myItems = Me.Inventory.Items;
                            foreach (var item in myItems.Where(x => x.TextureName.Contains(TextureName)))
                            {
                                if (Menu.Item(TextureName + "enabled").GetValue<bool>() && item.CanBeCasted())
                                {
                                    Item = item;
                                    OnUpdate();
                                    pNow = Environment.TickCount;
                                }
                            }
                        }
                        break;

                    case PluginType.Hero:
                    case PluginType.Orbwalker:
                        OnUpdate();
                        pNow = Environment.TickCount;
                        break;
                }
            }
        }

        private void Events_OnClose(object sender, EventArgs e)
        {
            OnClose();
        }

        #region Virtual Voids
        public virtual void OnUpdate()
        {
            
        }

        public virtual void SetupSpells()
        {
            
        }

        public virtual void OnClose()
        {
            
        }

        public virtual void OnLoadPlugin()
        {
            
        }

        #endregion
    }
}
