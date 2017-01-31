using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Ensage;
using Ensage.Common;
using Ensage.Common.Menu;
using Cloey.Interfaces;
using SharpDX;

namespace Cloey
{
    internal class Program
    {
        internal static Menu RootMenu;
        internal static Hero Me => ObjectManager.LocalHero;
        internal static List<Plugin> LoadedPlugins = new List<Plugin>();

        static void Main(string[] args)
        {
            Events.OnLoad += OnLoad;
            Events.OnClose += Events_OnClose;
        }

        private static void OnLoad(object sender, EventArgs e)
        {
            Orbwalking.Load();

            RootMenu = new Menu("Cloey", "cloey", true);

            var amenu = new Menu("Main", "utils");
            amenu.AddItem(new MenuItem("ticklimiter", "Tick Limiter")).SetValue(new Slider(250, 0, 1000)).SetTooltip("Limit OnUpdate");
            amenu.AddItem(new MenuItem("orbwalkmode", "Orbwalker: "))
                    .SetValue(new StringList(new[] {"Moones"}))
                    .ValueChanged +=
                (o, args) =>
                {
                    RootMenu.Item("f5check")
                        .Show(args.GetNewValue<StringList>().SelectedIndex == 0 &&
                              RootMenu.Children.All(x => x.Name != "Orbwalkerroot"));
                };

            amenu.AddItem(new MenuItem("f5check", "Orbwalker not Loaded Please F5!"))
                .SetFontColor(Color.Fuchsia)
                .Show(false);

            RootMenu.AddSubMenu(amenu);

            GetTypesByGroup("Plugins.Heroes").ForEach(x => { NewPlugin((Plugin) NewInstance(x), RootMenu); });
            GetTypesByGroup("Plugins.Orbwalkers").ForEach(x => { NewPlugin((Plugin) NewInstance(x), RootMenu); });
            RootMenu.AddToMainMenu();

            var color = System.Drawing.Color.FromArgb(255, 255, 135, 0);
            var hexargb = $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";
            DelayAction.Add(100, () => Game.PrintMessage("<b><font color=\"" + hexargb + "\">Cloey#</font></b> - Loaded!"));
        }

        private static void Events_OnClose(object sender, EventArgs e)
        {
            LoadedPlugins.Clear();
            RootMenu?.RemoveFromMainMenu();
        }

        private static void NewPlugin(Plugin plugin, Menu parent)
        {
            try
            {
                if (Me.Player?.Hero.ClassID != plugin.ClassId)
                {
                    return;
                }

                if (LoadedPlugins.Contains(plugin) == false)
                    LoadedPlugins.Add(plugin.Init(parent));
            }

            catch (Exception e)
            {
                Console.WriteLine(e);
                Game.PrintMessage("Exception thrown at <font color=\"#FFF280\">Cloey.NewPlugin</font>");
                throw;
            }
        }

        private static List<Type> GetTypesByGroup(string nspace)
        {
            try
            {
                Type[] allowedTypes = new[] { typeof(Plugin) };

                return
                    Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .Where(
                            t =>
                                t.IsClass && t.Namespace == "Cloey." + nspace &&
                                allowedTypes.Any(x => x.IsAssignableFrom(t)))
                        .ToList();
            }

            catch (Exception e)
            {
                Console.WriteLine(e);
                Game.PrintMessage("Exception thrown at <font color=\"#FFF280\">Cloey.GetTypesByGroup</font>");
                return null;
            }
        }

        private static object NewInstance(Type type)
        {
            try
            {
                ConstructorInfo target = type.GetConstructor(Type.EmptyTypes);
                DynamicMethod dynamic = new DynamicMethod(string.Empty, type, new Type[0], target.DeclaringType);
                ILGenerator il = dynamic.GetILGenerator();

                il.DeclareLocal(target.DeclaringType);
                il.Emit(OpCodes.Newobj, target);
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ret);

                Func<object> method = (Func<object>)dynamic.CreateDelegate(typeof(Func<object>));
                return method();
            }

            catch (Exception e)
            {
                Console.WriteLine(e);
                Game.PrintMessage("Exception thrown at <font color=\"#FFF280\">Cloey.NewInstance</font>");
                return null;
            }
        }
    }
}
