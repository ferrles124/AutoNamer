using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework.Input;

namespace AutoNamer
{
    public class ModEntry : Mod
    {
        private static IMonitor? _monitor;
        private static IModHelper? _helper;
        private static int _counter = 0;

        private static object? _pendingMenu = null;
        private static int _ticksWaited = 0;
        private const int TicksToWait = 120; // 60 tick/saniye * 2 saniye

        public override void Entry(IModHelper helper)
        {
            _monitor = this.Monitor;
            _helper = helper;
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

            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

            _monitor.Log("AutoNamer hazir.", LogLevel.Info);
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

                if (field?.GetValue(__instance) is StringBuilder sb)
                {
                    sb.Clear();
                    sb.Append(newName);
                    _monitor?.Log($"Otomatik isim atandi: {newName}", LogLevel.Info);

                    // 2 saniye sonra Enter'i tetiklemek icin bekleme listesine ekle
                    _pendingMenu = __instance;
                    _ticksWaited = 0;
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

        private static void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (_pendingMenu == null) return;

            _ticksWaited++;
            if (_ticksWaited < TicksToWait) return;

            try
            {
                var type = _pendingMenu.GetType();
                var method = type.GetMethod("RecieveSpecialInput",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (method != null)
                {
                    method.Invoke(_pendingMenu, new object[] { Keys.Enter });
                    _monitor?.Log("Enter otomatik tetiklendi.", LogLevel.Info);
                }
                else
                {
                    _monitor?.Log("RecieveSpecialInput metodu bulunamadi.", LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                _monitor?.Log($"Enter tetikleme hatasi: {ex}", LogLevel.Error);
            }
            finally
            {
                _pendingMenu = null;
            }
        }
    }
}
