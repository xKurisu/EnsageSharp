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
        internal static Menu Origin;
        internal static Hero Me => ObjectManager.LocalHero;
        internal static List<Plugin> LoadedPlugins = new List<Plugin>();

        internal static MenuItem targetingItem;
        internal static MenuItem orbwalkerItem;
        internal static MenuItem predictionItem;
        internal static MenuItem predictionRangeItem;
        internal static MenuItem predictionAllowCancelItem;
        internal static MenuItem predictionAllowTurningItem;
        internal static MenuItem switcherItem;

        static void Main(string[] args)
        {
            Events.OnLoad += OnLoad;
            Events.OnClose += Events_OnClose;
        }

        private static void OnLoad(object sender, EventArgs e)
        {
            Orbwalking.Load();

            Origin = new Menu(" Cloey#", "cloey", true);

            var amenu = new Menu("Main", "main");
            switcherItem = new MenuItem("switcher", "AIO Mode: ");
            amenu.AddItem(switcherItem)
                .SetValue(new StringList(new[] { "Utility + Hero", "Utility Only" })).ValueChanged += (o, args) =>
                {
                    orbwalkerItem.Show(args.GetNewValue<StringList>().SelectedIndex != 1);
                    predictionItem.Show(args.GetNewValue<StringList>().SelectedIndex != 1);
                    predictionRangeItem.Show(args.GetNewValue<StringList>().SelectedIndex != 1);
                    predictionAllowTurningItem.Show(args.GetNewValue<StringList>().SelectedIndex != 1);

                    var mLoaded = args.GetNewValue<StringList>().SelectedIndex == 1 &&
                                  Origin.Children.Any(x => x.Name == "moonesroot" || x.Name == "zynoxroot" || x.Name ==  x.TextureName + "main");
                    var kLoaded = args.GetNewValue<StringList>().SelectedIndex == 0 && 
                                 !Origin.Children.Any(x => x.Name == "moonesroot" || x.Name == "zynoxroot");

                    Origin.Item("f5check").Show(mLoaded || kLoaded);
                };


            targetingItem = new MenuItem("targeting", "Targeting:");
            orbwalkerItem = new MenuItem("orbwalkmode", "Orbwalker: ");
            predictionItem = new MenuItem("prediction", "Prediction: ");
            predictionRangeItem = new MenuItem("predictionrange", " -> Max Range: ");
            predictionAllowTurningItem = new MenuItem("predictionallowturning", " -> Ignore Turning/Rotating", true);
            predictionAllowCancelItem = new MenuItem("predictionallowcancel", "Allow Self Cast Interrupt");
            predictionAllowCancelItem.SetTooltip("Soon").Show(false);

            amenu.AddItem(targetingItem).SetValue(new StringList(new[] { "Mouse", "Quickest Kill" }));

            if (switcherItem.GetValue<StringList>().SelectedIndex != 1)
            {
                amenu.AddItem(orbwalkerItem).SetValue(new StringList(new[] { "Moones", "None" }));

                orbwalkerItem.ValueChanged += (o, args) =>
                {
                    var mLoaded = args.GetNewValue<StringList>().SelectedIndex == 1 &&
                                  Origin.Children.Any(x => x.Name == "moonesroot" || x.Name == "zynoxroot");
                    var kLoaded = args.GetNewValue<StringList>().SelectedIndex == 0 &&
                                  !Origin.Children.Any(x => x.Name == "moonesroot" || x.Name == "zynoxroot");

                    Origin.Item("f5check").Show(mLoaded || kLoaded);
                };

                amenu.AddItem(predictionItem).SetValue(new StringList(new[] { "Common", "Zynox" }, 1));
                amenu.AddItem(predictionAllowTurningItem).SetValue(false);
                amenu.AddItem(predictionRangeItem).SetValue(new Slider(1800, 1000, 3000));
                amenu.AddItem(predictionAllowCancelItem).SetValue(false).ValueChanged += (o, args) => args.Process = false;
            }

            amenu.AddItem(new MenuItem("f5check", "Reload Required Please F5!")).SetFontColor(Color.Fuchsia).Show(false);
            Origin.AddSubMenu(amenu);

            var hkmenu = new Menu("Hotkeys", "hkmenu");
            hkmenu.AddItem(new MenuItem("combokey", "Combo [active]")).SetValue(new KeyBind(32, KeyBindType.Press));
            hkmenu.AddItem(new MenuItem("mixedkey", "Mixed [active]")).SetValue(new KeyBind('G', KeyBindType.Press));
            Origin.AddSubMenu(hkmenu);

            if (switcherItem.GetValue<StringList>().SelectedIndex != 1)
            {
                GetTypesByGroup("Plugins.Heroes").ForEach(x => { NewPlugin((Plugin) NewInstance(x), Origin); });

                if (orbwalkerItem.GetValue<StringList>().SelectedIndex != 1)
                    GetTypesByGroup("Plugins.Orbwalkers").ForEach(x => { NewPlugin((Plugin) NewInstance(x), Origin); });
            }

            var imenu = new Menu("Utility", "utility");

            var umenu = new Menu("Items", "items");
            GetTypesByGroup("Plugins.Items.Offense").ForEach(x => { NewPlugin((Plugin) NewInstance(x), umenu); });
            imenu.AddSubMenu(umenu);

            Origin.AddSubMenu(imenu);
            Origin.AddToMainMenu();

            var color = System.Drawing.Color.FromArgb(255, 255, 135, 0);
            var hexargb = $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";
            DelayAction.Add(100, () => Game.PrintMessage("<b><font color=\"" + hexargb + "\">Cloey#</font></b> - Loaded!"));
        }

        private static void Events_OnClose(object sender, EventArgs e)
        {
            LoadedPlugins.Clear();
            Origin?.RemoveFromMainMenu();
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
                    LoadedPlugins.Add(plugin.Init(parent, Origin));
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
