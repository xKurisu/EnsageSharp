using System;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;

namespace Cloey.Interfaces
{
    internal class Plugin
    {
        #region Virtual Properties

        public virtual string PluginName { get; set; }
        public virtual string TextureName { get; set; }
        public virtual ClassID ClassId { get; set; }

        #endregion

        #region Properties

        public Menu Root { get; private set; }
        public Menu Menu { get; private set; }

        public Menu Parent => Menu.Parent;
        public Hero Me => ObjectManager.LocalHero;
        public Player Player => ObjectManager.LocalPlayer;

        #endregion

        private int _limiter;

        public Plugin Init(Menu root)
        {
            try
            {
                Root = root;
                Menu = new Menu(PluginName, PluginName + "root");

                // todo:

                SetupSpells();
                OnLoadPlugin();
                root.AddSubMenu(Menu);

                Events.OnClose += Events_OnClose;
                Game.OnIngameUpdate += Game_OnUpdate;
                Drawing.OnDraw += Drawing_OnDraw;
            }

            catch (Exception e)
            {
                Console.WriteLine(e);
                Game.PrintMessage("<font color=\"#FFF280\">Exception thrown at ComboHero.Init: </font>: " + e.Message);
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
            if (Game.IsPaused || !Game.IsInGame || Game.IsChatOpen)
            {
                return;
            }

            if (Me == null)
            {
                return;
            }

            if (!Me.IsAlive || Me.IsChanneling())
            {
                return;
            }

            if (Environment.TickCount - _limiter >= Menu.Parent.Item("ticklimiter").GetValue<Slider>().Value)
            {
                OnUpdate();
                _limiter = Environment.TickCount;
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
