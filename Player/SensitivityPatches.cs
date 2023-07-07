﻿using System.Reflection;
using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using HarmonyLib;
using UnityEngine;

namespace RealismMod
{
    public class SensPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass1603).GetMethod("ApplyExternalSense", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref Player.FirearmController __instance, Vector2 deltaRotation, ref Vector2 __result)
        {

            if (Plugin.IsFiring)
            {
                Player player = (Player)AccessTools.Field(typeof(GClass1603), "player_0").GetValue(__instance);
                float _mouseSensitivityModifier = (float)AccessTools.Field(typeof(Player), "_mouseSensitivityModifier").GetValue(player);
                float xLimit = Plugin.IsAiming ? Plugin.StartingAimSens : Plugin.StartingHipSens;
                Vector2 newSens = deltaRotation;
                newSens.y *= player.GetRotationMultiplier();
                newSens.x *= Mathf.Min(player.GetRotationMultiplier() * 1.5f, xLimit * (1f + _mouseSensitivityModifier));
                __result = newSens;
                return false;
            }
            return true;
        }
    }

    public class UpdateSensitivityPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("UpdateSensitivity", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        public static void PatchPostfix(ref Player.FirearmController __instance, ref float ____aimingSens)
        {
            Player player = (Player)AccessTools.Field(typeof(Player.FirearmController), "_player").GetValue(__instance);

            if (player.IsYourPlayer == true)
            {
                if (!Plugin.UniformAimIsPresent || !Plugin.BridgeIsPresent)
                {
                    Plugin.StartingAimSens = ____aimingSens;
                    Plugin.CurrentAimSens = ____aimingSens;
                }
                else
                {
                    Plugin.CurrentAimSens = Plugin.StartingAimSens;
                }
            }
        }
    }

    public class AimingSensitivityPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("get_AimingSensitivity", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        public static void PatchPostfix(ref Player.FirearmController __instance, ref float __result)
        {
            Player player = (Player)AccessTools.Field(typeof(Player.FirearmController), "_player").GetValue(__instance);

            if (player.IsYourPlayer == true)
            {
                __result = Plugin.CurrentAimSens;
            }
        }
    }

    public class GetRotationMultiplierPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("GetRotationMultiplier", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        public static void PatchPostfix(ref Player __instance, ref float __result)
        {
            if (__instance.IsYourPlayer == true)
            {
                if (!(__instance.HandsController != null) || !__instance.HandsController.IsAiming)
                {
                    float sens = Singleton<SharedGameSettingsClass>.Instance.Control.Settings.MouseSensitivity;
                    Plugin.StartingHipSens = sens;

                    if (!Plugin.CheckedForSens)
                    {
                        Plugin.CurrentHipSens = sens;
                        Plugin.CheckedForSens = true;
                    }
                    else
                    {
                        float _mouseSensitivityModifier = (float)AccessTools.Field(typeof(Player), "_mouseSensitivityModifier").GetValue(__instance);
                        __result = Plugin.CurrentHipSens * (1f + _mouseSensitivityModifier);
                    }
                }
            }
        }
    }
}
