using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Microsoft.Xna.Framework.Input;

namespace AutoNamer
{
    public class ModEntry : Mod
    {
        private static IMonitor? _monitor;
        private static int _counter = 0;

        private static object? _pendingMenu = null;
        private static int _ticksWaited = 0;
        private const int TicksToWait = 120;

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

            // --- 1) Isim ekranini otomatik doldurma + Enter (oncekiyle ayni) ---
            var menuType = waystonesAssembly.GetType("StardewWaystones.Code.WaystoneNameMenu");
            if (menuType != null)
            {
                var ctor = menuType.GetConstructors(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).First();
                harmony.Patch(ctor, postfix: new HarmonyMethod(typeof(ModEntry), nameof(WaystoneNameMenu_Postfix)));
                _monitor.Log("AutoNamer: isim ekrani yamasi uygulandi.", LogLevel.Info);
            }

            // --- 2) MouseRight (1001) -> MouseLeft (1000) yamasi ---
            var modEntryType = waystonesAssembly.GetType("StardewWaystones.ModEntry");
            var onButtonPressed = modEntryType?.GetMethod("OnButtonPressed",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (onButtonPressed != null)
            {
                harmony.Patch(onButtonPressed,
                    transpiler: new HarmonyMethod(typeof(ModEntry), nameof(OnButtonPressed_Transpiler)));
                _monitor.Log("AutoNamer: sag-tik -> sol-tik yamasi uygulandi.", LogLevel.Info);
            }
            else
            {
                _monitor.Log("OnButtonPressed metodu bulunamadi, tiklama yamasi uygulanamadi.", LogLevel.Warn);
            }

            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        // Waystones'in kendi OnButtonPressed kodundaki "1001" (MouseRight) sabitini
        // "1000" (MouseLeft) ile degistirir. Mantigin tamamini birebir korur.
        private static IEnumerable<CodeInstruction> OnButtonPressed_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_I4 && instruction.operand is int value && value == 1001)
                {
                    _monitor?.Log("Transpiler: 1001 (MouseRight) -> 1000 (MouseLeft) degistirildi.", LogLevel.Info);
                    yield return new CodeInstruction(OpCodes.Ldc_I4, 1000);
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        private static void WaystoneNameMenu_Postfix(object __instance)
        {
            try
            {
                _counter++;
                string newName = $"Isim{_counter}";
                var type = __instance.GetType();
                var field = type.GetField("inputText", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (field?.GetValue(__instance) is StringBuilder sb)
                {
                    sb.Clear();
                    sb.Append(newName);
                    _pendingMenu = __instance;
                    _ticksWaited = 0;
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
                var method = type.GetMethod("RecieveSpecialInput", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                method?.Invoke(_pendingMenu, new object[] { Keys.Enter });
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
