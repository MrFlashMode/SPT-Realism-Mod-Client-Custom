﻿using System.Reflection;
using Aki.Reflection.Patching;
using EFT;
using EFT.Animations;
using HarmonyLib;
using UnityEngine;

namespace RealismMod
{
    public class SyncWithCharacterSkillsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("SyncWithCharacterSkills", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref Player.FirearmController __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(Player.FirearmController), "_player").GetValue(__instance);

            if (player.IsYourPlayer == true)
            {
                SkillsClass.GClass1680 skillsClass = (SkillsClass.GClass1680)AccessTools.Field(typeof(Player.FirearmController), "gclass1680_0").GetValue(__instance);
                PlayerProperties.StrengthSkillAimBuff = player.Skills.StrengthBuffAimFatigue.Value;
                PlayerProperties.ReloadSkillMulti = Mathf.Max(1, ((skillsClass.ReloadSpeed - 1f) * 0.5f) + 1f);
                PlayerProperties.FixSkillMulti = skillsClass.FixSpeed;
                PlayerProperties.WeaponSkillErgo = skillsClass.DeltaErgonomics;
                PlayerProperties.AimSkillADSBuff = skillsClass.AimSpeed;
                PlayerProperties.StressResistanceFactor = player.Skills.StressPain.Value;
            }
        }
    }

    public class PlayerInitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("Init", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {
            if (__instance.IsYourPlayer == true)
            {
                PlayerProperties.playerIsScav = __instance.Fraction == ETagStatus.Scav;
                Plugin.StanceBlender.Target = 0f;
                StatCalc.SetGearParamaters(__instance);
                StanceController.SelectedStance = 0;
                StanceController.IsLowReady = false;
                StanceController.IsHighReady = false;
                StanceController.IsActiveAiming = false;
                StanceController.WasHighReady = false;
                StanceController.WasLowReady = false;
                StanceController.IsShortStock = false;
            }
        }
    }

    public class OnItemAddedOrRemovedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("OnItemAddedOrRemoved", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {
            if (__instance.IsYourPlayer == true)
            {
                StatCalc.SetGearParamaters(__instance);
            }
        }
    }

    public class PlayerLateUpdatePatch : ModulePatch
    {

        private static float sprintCooldownTimer = 0f;
        private static bool doSwayReset = false;
        private static float sprintTimer = 0f;
        private static bool didSprintPenalties = false;
        private static bool resetSwayAfterFiring = false;

        private static void doSprintTimer(ProceduralWeaponAnimation pwa, Player.FirearmController fc)
        {
            sprintCooldownTimer += Time.deltaTime;

            if (!didSprintPenalties)
            {
                float sprintDurationModi = 1 + ((sprintTimer * 2) / 10f);

                float breathIntensity = Mathf.Min(pwa.Breath.Intensity * sprintDurationModi, 5f);
                float inputIntensitry = Mathf.Min(pwa.HandsContainer.HandsRotation.InputIntensity * sprintDurationModi, 1.15f);
                pwa.Breath.Intensity = breathIntensity;
                pwa.HandsContainer.HandsRotation.InputIntensity = inputIntensitry;
                PlayerProperties.SprintTotalBreathIntensity = breathIntensity;
                PlayerProperties.SprintTotalHandsIntensity = inputIntensitry;

                PlayerProperties.ADSSprintMulti = Mathf.Min(1f - (sprintTimer / 25f), 0.3f);

                didSprintPenalties = true;
                doSwayReset = false;
            }

            if (sprintCooldownTimer >= 0.35f)
            {
                PlayerProperties.SprintBlockADS = false;
                if (PlayerProperties.TriedToADSFromSprint)
                {
                    fc.ToggleAim();
                }
            }
            if (sprintCooldownTimer >= 3f)
            {
                PlayerProperties.WasSprinting = false;
                doSwayReset = true;
                sprintCooldownTimer = 0f;
                sprintTimer = 0f;
            }
        }

        private static void resetSwayParams(ProceduralWeaponAnimation pwa)
        {
            float resetSpeed = Time.deltaTime * 0.5f;
            float resetSpeedADS = Time.deltaTime;
            PlayerProperties.SprintTotalBreathIntensity = Mathf.Lerp(PlayerProperties.SprintTotalBreathIntensity, PlayerProperties.TotalBreathIntensity, resetSpeed);
            PlayerProperties.SprintTotalHandsIntensity = Mathf.Lerp(PlayerProperties.SprintTotalHandsIntensity, PlayerProperties.TotalHandsIntensity, resetSpeed);
            PlayerProperties.ADSSprintMulti = Mathf.Lerp(PlayerProperties.ADSSprintMulti, 1f, resetSpeedADS);

            pwa.Breath.Intensity = PlayerProperties.SprintTotalBreathIntensity;
            pwa.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.SprintTotalHandsIntensity;

            if (Utils.AreFloatsEqual(1f, PlayerProperties.ADSSprintMulti) && Utils.AreFloatsEqual(pwa.Breath.Intensity, PlayerProperties.TotalBreathIntensity) && Utils.AreFloatsEqual(pwa.HandsContainer.HandsRotation.InputIntensity, PlayerProperties.TotalHandsIntensity))
            {
                Logger.LogWarning("=============RESET=========");
                doSwayReset = false;
            }
        }

        private static void DoSprintPenalty(Player player, Player.FirearmController fc)
        {
            if (player.IsSprintEnabled)
            {
                sprintTimer += Time.deltaTime;
                if (sprintTimer >= 1f)
                {
                    PlayerProperties.SprintBlockADS = true;
                    PlayerProperties.WasSprinting = true;
                    didSprintPenalties = false;
                }
            }
            else
            {
                if (PlayerProperties.WasSprinting)
                {
                    doSprintTimer(player.ProceduralWeaponAnimation, fc);
                }
                if (doSwayReset)
                {
                    resetSwayParams(player.ProceduralWeaponAnimation);
                }
            }

            if (!doSwayReset && !PlayerProperties.WasSprinting)
            {
                PlayerProperties.HasFullyResetSprintADSPenalties = true;
            }
            else
            {
                PlayerProperties.HasFullyResetSprintADSPenalties = false;
            }

            if (Plugin.IsFiring)
            {
                doSwayReset = false;
                player.ProceduralWeaponAnimation.Breath.Intensity = 0.69f;
                player.ProceduralWeaponAnimation.HandsContainer.HandsRotation.InputIntensity = 0.71f;
                resetSwayAfterFiring = false;
            }
            else if (!resetSwayAfterFiring)
            {
                resetSwayAfterFiring = true;
                doSwayReset = true;
            }
        }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {
            if (Utils.IsReady && __instance.IsYourPlayer)
            {
                Player.FirearmController fc = __instance.HandsController as Player.FirearmController;

                PlayerProperties.IsSprinting = __instance.IsSprintEnabled;
                PlayerProperties.enviroType = __instance.Environment;
                Plugin.IsInInventory = __instance.IsInventoryOpened;

                if (Plugin.EnableSprintPenalty.Value)
                {
                    DoSprintPenalty(__instance, fc);
                }

                if (fc != null)
                {
                    ReloadController.ReloadStateCheck(__instance, fc, Logger);
                    AimController.ADSCheck(__instance, fc, Logger);

                    if (Plugin.EnableStanceStamChanges == true)
                    {
                        StanceController.SetStanceStamina(__instance, fc);
                    }

                    float remainStamPercent = __instance.Physical.HandsStamina.Current / __instance.Physical.HandsStamina.TotalCapacity;
                    PlayerProperties.RemainingArmStamPercentage = 1f - ((1f - remainStamPercent) / 3.5f);
                }
                else if (Plugin.EnableStanceStamChanges == true)
                {
                    StanceController.ResetStanceStamina(__instance);
                }

                __instance.Physical.HandsStamina.Current = Mathf.Max(__instance.Physical.HandsStamina.Current, 1f);
            }
        }
    }
}

