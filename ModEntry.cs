using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
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
        private static int _counter = 0;

        private static object? _pendingMenu = null;
        private static int _ticksWaited = 0;
        private const int TicksToWait = 120;

        private static FieldInfo? _waystoneManagerField;
        private static MethodInfo? _setLastActivatedTile;
        private static MethodInfo? _getWaystoneName;
        private static object? _waystoneManagerInstance;
        private static object? _waystonesModEntryInstance;

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
            if (menuType != null)
            {
                var ctor = menuType.GetConstructors(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).First();
                harmony.Patch(ctor, postfix: new HarmonyMethod(typeof(ModEntry), nameof(WaystoneNameMenu_Postfix)));
            }

            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Input.ButtonPressed += OnButtonPressed;

            _monitor.Log("AutoNamer hazir (debug modu acik).", LogLevel.Info);
        }

        // === DEBUG: her tusu logla ===
        private static void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            var grabTile = e.Cursor.GrabTile;
            var loc = Game1.currentLocation;

            bool hasObject = loc.objects.TryGetValue(grabTile, out var obj);

            _monitor?.Log($"[DEBUG] Tus: {e.Button} ({(int)e.Button}) | Tile: {grabTile} | Objeli mi: {hasObject} | Obje adi: {(hasObject ? obj?.Name : "yok")}", LogLevel.Info);

            if (!hasObject || obj == null) return;

            // bigCraftable + isim iceriginde "waystone" geciyorsa (kucuk/buyuk harf farketmeksizin)
            bool isBigCraftable = obj.bigCraftable.Value;
            bool nameMatches = obj.Name != null && obj.Name.ToLower().Contains("waystone");

            if (isBigCraftable && nameMatches)
            {
                _monitor?.Log($"[DEBUG] Waystone tespit edildi! Buton: {e.Button}", LogLevel.Info);
                TryOpenWaystoneMenu(grabTile);
            }
        }

        private static void TryOpenWaystoneMenu(Microsoft.Xna.Framework.Vector2 tile)
        {
            try
            {
                var waystonesAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "StardewWaystones");
                if (waystonesAssembly == null) return;

                // ModEntry instance'ini bul (SMAPI mod listesi uzerinden degil, static/singleton arama)
                var modEntryType = waystonesAssembly.GetType("StardewWaystones.ModEntry");
                if (modEntryType == null)
                {
                    _monitor?.Log("[DEBUG] StardewWaystones.ModEntry tipi bulunamadi.", LogLevel.Warn);
                    return;
                }

                // waystoneManager field'i static degil, instance'a ihtiyacimiz var.
                // SMAPI'nin kendi mod kayitlarindan instance'i cekmemiz gerekebilir; basit yontem: tum static field/event uzerinden bulmaya calis
                _monitor?.Log("[DEBUG] Menu acma denemesi yapildi (manuel tetikleme henuz tamamlanmadi, sadece tespit calisiyor).", LogLevel.Info);
            }
            catch (Exception ex)
            {
                _monitor?.Log($"[DEBUG] TryOpenWaystoneMenu hata: {ex}", LogLevel.Error);
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
