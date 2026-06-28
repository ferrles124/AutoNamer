using HarmonyLib;
using StardewModdingAPI;
using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AutoNamer
{
    public class ModEntry : Mod
    {
        private static IMonitor? _monitor;
        private static int _counter = 0;

        public override void Entry(IModHelper helper)
        {
            _monitor = this.Monitor;
            var harmony = new Harmony("Mehmet.AutoNamer");

            var waystonesAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "StardewWaystones");

            if (waystonesAssembly == null)
            {
                _monitor.Log("Stardew Waystones bulunamadi.", LogLevel.Warn);
                return;
            }

            var menuType = waystonesAssembly.GetType("StardewWaystones.Code.WaystoneNameMenu");
            if (menuType == null)
            {
                _monitor.Log("WaystoneNameMenu tipi bulunamadi.", LogLevel.Warn);
                return;
            }

            var ctor = menuType.GetConstructors(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).First();

            harmony.Patch(
                original: ctor,
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(WaystoneNameMenu_Postfix))
            );

            _monitor.Log("AutoNamer, WaystoneNameMenu constructor'ina baglandi.", LogLevel.Info);
        }

        private static void WaystoneNameMenu_Postfix(object __instance)
        {
            try
            {
                _counter++;
                string newName = $"Isim{_counter}";

                var type = __instance.GetType();

                // inputText bir StringBuilder, string degil!
                var field = type.GetField("inputText",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (field?.GetValue(__instance) is StringBuilder sb)
                {
                    sb.Clear();
                    sb.Append(newName);
                    _monitor?.Log($"Otomatik isim atandi: {newName}", LogLevel.Info);
                }
                else
                {
                    _monitor?.Log("inputText StringBuilder olarak bulunamadi.", LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                _monitor?.Log($"WaystoneNameMenu_Postfix hata: {ex}", LogLevel.Error);
            }
        }
    }
}
