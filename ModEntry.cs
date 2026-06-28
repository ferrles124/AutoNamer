using HarmonyLib;
using StardewModdingAPI;
using System;
using System.Linq;
using System.Reflection;

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

            // Waystones DLL'i yüklendiyse, içindeki WaystoneNameMenu tipini bul
            var waystonesAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "StardewWaystones");

            if (waystonesAssembly == null)
            {
                _monitor.Log("Stardew Waystones bulunamadı, AutoNamer bu oturumda pasif.", LogLevel.Warn);
                return;
            }

            var menuType = waystonesAssembly.GetTypes()
                .FirstOrDefault(t => t.Name == "WaystoneNameMenu");

            if (menuType == null)
            {
                _monitor.Log("WaystoneNameMenu tipi bulunamadı, AutoNamer bu oturumda pasif.", LogLevel.Warn);
                return;
            }

            // Tüm public/non-public constructor'ları yakala
            foreach (var ctor in menuType.GetConstructors(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                harmony.Patch(
                    original: ctor,
                    postfix: new HarmonyMethod(typeof(ModEntry), nameof(WaystoneNameMenu_Postfix))
                );
            }

            _monitor.Log("AutoNamer, WaystoneNameMenu'ye basariyla baglandi.", LogLevel.Info);
        }

        private static void WaystoneNameMenu_Postfix(object __instance)
        {
            try
            {
                _counter++;
                string newName = $"Isim{_counter}";

                var type = __instance.GetType();
                var field = type.GetField("inputText",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (field != null)
                {
                    field.SetValue(__instance, newName);
                    _monitor?.Log($"Otomatik isim atandi: {newName}", LogLevel.Info);
                }
                else
                {
                    _monitor?.Log("inputText alani bulunamadi.", LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                _monitor?.Log($"WaystoneNameMenu_Postfix hata: {ex}", LogLevel.Error);
            }
        }
    }
}
