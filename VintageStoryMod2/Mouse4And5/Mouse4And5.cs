using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace VintageStoryMod2
{
    [HarmonyPatch]
    public class Mouse4And5 : ModSystem
    {
        private static readonly MethodInfo OrMouse4MethodInfo =
            AccessTools.Method(typeof(Mouse4And5), nameof(OrMouse4));

        private static readonly MethodInfo OrMouse5MethodInfo =
            AccessTools.Method(typeof(Mouse4And5), nameof(OrMouse5));

        private static readonly MethodInfo XorMouse4AndSprintMethodInfo =
            AccessTools.Method(typeof(Mouse4And5), nameof(XorMouse4AndSprint));

        private static readonly MethodInfo XorMouse5AndSneakMethodInfo =
            AccessTools.Method(typeof(Mouse4And5), nameof(XorMouse5AndSneak));
        
        public static readonly string ModId = "mouse4and5";

        public static event Action Mouse4DownChanged;
        public static event Action Mouse5DownChanged;

        public static bool Mouse4Down { get; private set; }
        public static bool Mouse5Down { get; private set; }

        public static ModConfig ModConfig { get; private set; }
        public Harmony Harmony { get; private set; }

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);

            ModConfig = api.LoadModConfig<ModConfig>($"{ModId}.json");
            if (ModConfig == null)
            {
                api.StoreModConfig(new ModConfig(), $"{ModId}.json");
                ModConfig = api.LoadModConfig<ModConfig>($"{ModId}.json");
            }
        }

        public override void StartClientSide(ICoreClientAPI capi)
        {
            base.StartClientSide(capi);

            Harmony = new Harmony(ModId);
            Harmony.PatchAll();

            RegisterHotKeyHelper(capi, "mouse4", I18N("mouse4"), EnumMouseButton.Button4);
            RegisterHotKeyHelper(capi, "mouse5", I18N("mouse5"), EnumMouseButton.Button5);
            capi.Input.SetHotKeyHandler("mouse4", (keyComb) =>
            {
                if (ModConfig.ToggleHold)
                {
                    Mouse4Down = !Mouse4Down;
                }
                else
                {
                    Mouse4Down = !keyComb.OnKeyUp;
                }

                Mouse4DownChanged?.Invoke();
                return true;
            });

            capi.Input.SetHotKeyHandler("mouse5", (keyComb) =>
            {
                if (ModConfig.ToggleHold)
                {
                    Mouse5Down = !Mouse5Down;
                }
                else
                {
                    Mouse5Down = !keyComb.OnKeyUp;
                }

                Mouse5DownChanged?.Invoke();
                return true;
            });

            if (ModConfig.NoHud == false)
            {
                var hud = new MyHUD(capi);
                hud.TryOpen();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            Harmony.UnpatchAll(ModId);
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(SystemPlayerControl), "OnGameTick")]
        public static IEnumerable<CodeInstruction> OnTickTranspiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            var setCtrlKey = AccessTools.Method(typeof(EntityControls), "set_CtrlKey");
            var setShiftKey = AccessTools.Method(typeof(EntityControls), "set_ShiftKey");
            var setSprintKey = AccessTools.Method(typeof(EntityControls), "set_Sprint");
            var setSneakKey = AccessTools.Method(typeof(EntityControls), "set_Sneak");

            var foundCtrl = false;
            var foundShift = false;
            var foundSprint = false;
            var foundSneak = false;

            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Callvirt)
                {
                    if (instruction.operand == setShiftKey)
                    {
                        yield return new CodeInstruction(OpCodes.Call, OrMouse4MethodInfo);
                        foundShift = true;
                    }
                    else if (instruction.operand == setCtrlKey)
                    {
                        yield return new CodeInstruction(OpCodes.Call, OrMouse5MethodInfo);
                        foundCtrl = true;
                    }
                    else if (instruction.operand == setSprintKey)
                    {
                        yield return new CodeInstruction(OpCodes.Call, XorMouse4AndSprintMethodInfo);
                        foundSprint = true;
                    }
                    else if (instruction.operand == setSneakKey)
                    {
                        yield return new CodeInstruction(OpCodes.Call, XorMouse5AndSneakMethodInfo);
                        foundSneak = true;
                    }
                }

                yield return instruction;
            }

            if (!foundCtrl)
            {
                throw new Exception("Could not find set_CtrlKey");
            }

            if (!foundShift)
            {
                throw new Exception("Could not find set_ShiftKey");
            }

            if (!foundSprint)
            {
                throw new Exception("Could not find set_Sprint");
            }

            if (!foundSneak)
            {
                throw new Exception("Could not find set_Sneak");
            }
        }

        public static bool OrMouse4(bool ogInput)
        {
            return ogInput || Mouse4Down;
        }

        public static bool OrMouse5(bool ogInput)
        {
            return ogInput || Mouse5Down;
        }

        public static bool XorMouse4AndSprint(bool ogInput)
        {
            return ogInput ^ (Mouse4Down && ModConfig.Mouse4Sprint);
        }

        public static bool XorMouse5AndSneak(bool ogInput)
        {
            return ogInput ^ (Mouse5Down && ModConfig.Mouse5Sneak);
        }

        public static string I18N(string key)
        {
            return Lang.Get($"{ModId}:{key}");
        }

        private static void RegisterHotKeyHelper(
            ICoreClientAPI capi,
            string key,
            string description,
            EnumMouseButton button)
        {
            /*
             * KF 5/20/2024: I did some digging and found that if you add 240 to EnumMouseButton, you get a value that
             * can be used as a keycode for the hotkey system. This is useful because the API only allows a GlKeys enum
             * parameter to be passed to the RegisterHotKey method. This is a workaround to add a mouse 4 and mouse 5
             * hotkey - the developers likely are not ready to add this feature to the API yet, since it is not possible
             * to rebind mouse 4 and 5 in the settings ONLY because the mouse control settings gui cuts off the option
             * to lol.
             */
            var keyCode = (int)button + 240;
            capi.Input.RegisterHotKey(key, description, (GlKeys)keyCode, HotkeyType.MouseControls);
            capi.Input.GetHotKeyByCode(key).TriggerOnUpAlso = true;
        }
    }
}