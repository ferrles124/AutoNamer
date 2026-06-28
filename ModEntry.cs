using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Menus;
using System.Reflection;

namespace AutoNamer
{
    public class ModEntry : Mod
    {
        private static IModHelper? _helper;
        private static int _counter = 0;

        public override void Entry(IModHelper helper)
        {
            _helper = helper;
            var harmony = new Harmony("Mehmet.AutoNamer");

            foreach (var ctor in typeof(NamingMenu).GetConstructors(
                BindingFlags.Public | BindingFlags.Instance))
            {
                harmony.Patch(
                    original: ctor,
                    postfix: new HarmonyMethod(typeof(ModEntry), nameof(NamingMenu_Postfix))
                );
            }
        }

        private static void NamingMenu_Postfix(NamingMenu __instance)
        {
            _counter++;
            __instance.textBox.Text = $"Isim{_counter}";
        }
    }
}
