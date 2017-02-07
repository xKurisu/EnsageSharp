using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Cloey.Interfaces
{
    internal class Plugin
    {
        #region Virtual Properties

        public virtual string PluginName { get; set; }
        public virtual string TextureName { get; set; }
        public virtual bool IsHeroPlugin { get; set; } = false;
        public virtual ClassID ClassId { get; set; } = ObjectManager.LocalHero.ClassID;

        public Item Item;
        public Dictionary<string, bool> cDict = new Dictionary<string, bool>();

        #endregion

        #region Properties

        public Menu Root { get; private set; }
        public Menu Menu { get; private set; }

        public Menu Parent => Menu.Parent;
        public Hero Me => ObjectManager.LocalHero;
        public Player Player => ObjectManager.LocalPlayer;

        #endregion

        private int _limiter;

        // todo: redo initializer
        public Plugin Init(Menu root, Menu origin)
        {
            try
            {

                if (IsHeroPlugin)
                {
                    Root = origin;
                    Menu = new Menu(PluginName, TextureName + "main");

                    // todo:

                    SetupSpells();
                    OnLoadPlugin();
                    root.AddSubMenu(Menu);

                    if (!cDict.ContainsKey("Init"))
                    {
                        Events.OnClose += Events_OnClose;
                        Game.OnIngameUpdate += Game_OnUpdate;
                        Drawing.OnDraw += Drawing_OnDraw;
                        cDict["Init"] = true;
                    }

                    Root.TextureName = TextureName;
                    Root.ShowTextWithTexture = true;
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

                        // todo:

                        SetupSpells();
                        OnLoadPlugin();
                        root.AddSubMenu(Menu);

                        if (!cDict.ContainsKey("Init"))
                        {
                            Events.OnClose += Events_OnClose;
                            Game.OnIngameUpdate += Game_OnUpdate;
                            Drawing.OnDraw += Drawing_OnDraw;
                            cDict["Init"] = true;
                        }
                    }
                }

                // items
                if (TextureName.ToLower().Contains("item"))
                {
                    Root = origin;
                    var uniqueTexture = TextureName == "item_dagon" ? "item_dagon_5" : TextureName;

                    Menu = new Menu("  " + PluginName, TextureName + "root", false, uniqueTexture, true);
                    Menu.AddItem(new MenuItem(TextureName + "enabled", "Enabled: ")).SetValue(true);
                    Menu.AddItem(new MenuItem(TextureName + "mode", "Mode: ")).SetValue(new StringList(new[] { "Combo", "Always" }));

                    // todo:

                    SetupSpells();
                    OnLoadPlugin();
                    root.AddSubMenu(Menu);

                    if (!cDict.ContainsKey("Init"))
                    {
                        Events.OnClose += Events_OnClose;
                        Game.OnIngameUpdate += Game_OnUpdate;
                        Drawing.OnDraw += Drawing_OnDraw;
                        cDict["Init"] = true;
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

        private void Events_OnClose(object sender, EventArgs e)
        {
            OnClose();
        }

        private void Drawing_OnDraw(EventArgs args)
        {

        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (!cDict["Init"] || Me == null)
            {
                return;
            }

            if (Game.IsPaused || !Game.IsInGame || Game.IsChatOpen)
            {
                return;
            }

            if (!Me.IsAlive || Me.IsChanneling())
            {
                return;
            }

            if (Environment.TickCount - _limiter >= 250) 
            {
                if (TextureName.Contains("item"))
                {
                    var combo = Root != null && Root.Item("combokey").GetValue<KeyBind>().Active;
                    if (combo || Menu.Item(TextureName + "mode").GetValue<StringList>().SelectedIndex != 0)
                    {
                        var myItems = Me.Inventory.Items;
                        foreach (var item in myItems.Where(x => x.TextureName.Contains(TextureName)))
                        {
                            if (Menu.Item(TextureName + "enabled").GetValue<bool>() && item.CanBeCasted())
                            {
                                var slot = Me.FindItem(item.TextureName);
                                if (slot != null)
                                {
                                    Item = slot;
                                    OnUpdate();
                                    _limiter = Environment.TickCount;
                                }
                            }
                        }
                    }
                }
                else
                {
                    OnUpdate();
                    _limiter = Environment.TickCount;
                }
            }
        }

        public virtual void OnUpdate()
        {

        }

        public virtual void OnLoadPlugin()
        {

        }

        public virtual void SetupSpells()
        {
            
        }

        public virtual void OnClose()
        {
            
        }
    }
}
