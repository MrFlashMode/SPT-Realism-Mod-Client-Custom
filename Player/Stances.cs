﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;
using static EFT.Player;

namespace RealismMod
{
    public static class StanceController
    {

        public static string[] botsToUseTacticalStances = { "sptBear", "sptUsec", "exUsec", "pmcBot", "bossKnight", "followerBigPipe", "followerBirdEye", "bossGluhar", "followerGluharAssault", "followerGluharScout", "followerGluharSecurity", "followerGluharSnipe" };

        public static int SelectedStance = 0;

        public static bool IsActiveAiming = false;
        public static bool PistolIsCompressed = false;
        public static bool IsHighReady = false;
        public static bool IsLowReady = false;
        public static bool IsShortStock = false;
        public static bool WasHighReady = false;
        public static bool WasLowReady = false;
        public static bool WasShortStock = false;
        public static bool WasActiveAim = false;

        public static bool IsFiringFromStance = false;
        public static float StanceShotTime = 0.0f;
        public static float ManipTime = 0.0f;

        public static float HighReadyBlackedArmTime = 0.0f;
        public static bool DoHighReadyInjuredAnim = false;

        public static bool HaveSetAiming = false;
        public static bool SetActiveAiming = false;

        public static float HighReadyManipBuff = 1f;
        public static float ActiveAimManipBuff = 1f;
        public static float LowReadyManipBuff = 1f;

        public static bool CancelPistolStance = false;
        public static bool PistolIsColliding = false;
        public static bool CancelHighReady = false;
        public static bool CancelLowReady = false;
        public static bool CancelShortStock = false;
        public static bool CancelActiveAim = false;
        public static bool DoResetStances = false;

        private static bool setRunAnim = false;
        private static bool resetRunAnim = false;

        private static bool gotCurrentStam = false;
        private static float currentStam = 100f;

        public static Vector3 StanceTargetPosition = Vector3.zero;

        public static bool HasResetActiveAim = true;
        public static bool HasResetLowReady = true;
        public static bool HasResetHighReady = true;
        public static bool HasResetShortStock = true;
        public static bool HasResetPistolPos = true;

        public static Dictionary<string, bool> LightDictionary = new Dictionary<string, bool>();

        public static bool toggledLight = false;

        public static void SetStanceStamina(Player player, FirearmController fc)
        {
            if (!PlayerProperties.IsSprinting)
            {
                gotCurrentStam = false;

                if (fc.Item.WeapClass != "pistol")
                {
                    if (!IsHighReady && !IsLowReady && !Plugin.IsAiming && !IsActiveAiming && !IsShortStock && Plugin.EnableIdleStamDrain && !player.IsInPronePose)
                    {
                        player.Physical.Aim(!(player.MovementContext.StationaryWeapon == null) ? 0f : WeaponProperties.ErgoFactor * 0.8f * ((1f - PlayerProperties.ADSInjuryMulti) + 1f));
                    }
                    else if (IsActiveAiming)
                    {
                        player.Physical.Aim(!(player.MovementContext.StationaryWeapon == null) ? 0f : WeaponProperties.ErgoFactor * 0.4f * ((1f - PlayerProperties.ADSInjuryMulti) + 1f));
                    }
                    else if (!Plugin.IsAiming && !Plugin.EnableIdleStamDrain)
                    {
                        player.Physical.Aim(0f);
                    }
                    if (IsHighReady && !IsLowReady && !Plugin.IsAiming && !IsShortStock)
                    {
                        player.Physical.Aim(0f);
                        player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + ((((1f - (WeaponProperties.ErgoFactor / 100f)) * 0.01f) * PlayerProperties.ADSInjuryMulti)), player.Physical.HandsStamina.TotalCapacity);
                    }
                    if (IsLowReady && !IsHighReady && !Plugin.IsAiming && !IsShortStock)
                    {
                        player.Physical.Aim(0f);
                        player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + (((1f - (WeaponProperties.ErgoFactor / 100f)) * 0.03f) * PlayerProperties.ADSInjuryMulti), player.Physical.HandsStamina.TotalCapacity);
                    }
                    if (IsShortStock && !IsHighReady && !Plugin.IsAiming && !IsLowReady)
                    {
                        player.Physical.Aim(0f);
                        player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + (((1f - (WeaponProperties.ErgoFactor / 100f)) * 0.01f) * PlayerProperties.ADSInjuryMulti), player.Physical.HandsStamina.TotalCapacity);
                    }
                }
                else
                {
                    if (!Plugin.IsAiming)
                    {
                        player.Physical.Aim(0f);
                        player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + (((1f - (WeaponProperties.ErgoFactor / 100f)) * 0.025f) * PlayerProperties.ADSInjuryMulti), player.Physical.HandsStamina.TotalCapacity);
                    }
                }
            }
            else
            {
                if (!gotCurrentStam)
                {
                    currentStam = player.Physical.HandsStamina.Current;
                    gotCurrentStam = true;
                }

                player.Physical.Aim(0f);
                player.Physical.HandsStamina.Current = currentStam;
            }

            if (player.IsInventoryOpened || (player.IsInPronePose && !Plugin.IsAiming))
            {
                player.Physical.Aim(0f);
                player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + (0.04f * PlayerProperties.ADSInjuryMulti), player.Physical.HandsStamina.TotalCapacity);
            }
        }

        public static void ResetStanceStamina(Player player)
        {
            player.Physical.Aim(0f);
            player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + (0.04f * PlayerProperties.ADSInjuryMulti), player.Physical.HandsStamina.TotalCapacity);
        }

        public static bool IsIdle()
        {
            return !IsActiveAiming && !IsHighReady && !IsLowReady && !IsShortStock && !WasHighReady && !WasLowReady && !WasShortStock && !WasActiveAim && HasResetActiveAim && HasResetHighReady && HasResetLowReady && HasResetShortStock && HasResetPistolPos ? true : false;
        }

        public static void StanceManipCancelTimer()
        {
            ManipTime += Time.deltaTime;

            if (ManipTime >= 0.25f)
            {
                CancelHighReady = false;
                CancelLowReady = false;
                CancelShortStock = false;
                CancelPistolStance = false;
                CancelActiveAim = false;
                DoResetStances = false;
                ManipTime = 0f;
            }
        }

        public static void StanceShotTimer()
        {
            StanceShotTime += Time.deltaTime;

            if (StanceShotTime >= 0.5f)
            {
                IsFiringFromStance = false;
                StanceShotTime = 0f;
            }
        }

        public static void StanceState()
        {
            if (Utils.WeaponReady == true)
            {
                if (!PlayerProperties.IsSprinting && !Plugin.IsInInventory && WeaponProperties._WeapClass != "pistol")
                {
                    //if (Plugin.EnableTacSprint.Value && (PlayerProperties.RightArmRuined == true || PlayerProperties.LeftArmRuined == true))
                    //{
                    //    DoHighReadyInjuredAnim = true;
                    //}

                    //active aim
                    if (!Plugin.ToggleActiveAim.Value)
                    {
                        if (Input.GetKey(KeyCode.Mouse1) && !PlayerProperties.IsAllowedADS)
                        {
                            Plugin.StanceBlender.Target = 1f;
                            IsActiveAiming = true;
                            IsShortStock = false;
                            IsHighReady = false;
                            IsLowReady = false;
                            WasActiveAim = IsActiveAiming;
                            SetActiveAiming = true;
                        }
                        else if (SetActiveAiming == true)
                        {
                            Plugin.StanceBlender.Target = 0f;
                            IsActiveAiming = false;
                            IsHighReady = WasHighReady;
                            IsLowReady = WasLowReady;
                            IsShortStock = WasShortStock;
                            WasActiveAim = IsActiveAiming;
                            SetActiveAiming = false;
                        }
                    }
                    else
                    {
                        if (Input.GetKeyDown(KeyCode.Mouse1) && !PlayerProperties.IsAllowedADS)
                        {
                            Plugin.StanceBlender.Target = Plugin.StanceBlender.Target == 0f ? 1f : 0f;
                            IsActiveAiming = !IsActiveAiming;
                            IsShortStock = false;
                            IsHighReady = false;
                            IsLowReady = false;
                            WasActiveAim = IsActiveAiming;
                            if (IsActiveAiming == false)
                            {
                                IsHighReady = WasHighReady;
                                IsLowReady = WasLowReady;
                                IsShortStock = WasShortStock;
                            }
                        }
                    }

                    if (Plugin.IsAiming)
                    {
                        if (IsActiveAiming || WasActiveAim)
                        {
                            WasHighReady = false;
                            WasLowReady = false;
                            WasShortStock = false;
                        }
                        IsLowReady = false;
                        IsHighReady = false;
                        IsShortStock = false;
                        IsActiveAiming = false;
                        HaveSetAiming = true;
                    }
                    else if (HaveSetAiming)
                    {
                        IsLowReady = WasLowReady;
                        IsHighReady = WasHighReady;
                        IsShortStock = WasShortStock;
                        IsActiveAiming = WasActiveAim;
                        HaveSetAiming = false;
                    }

                    if (DoHighReadyInjuredAnim)
                    {
                        HighReadyBlackedArmTime += Time.deltaTime;
                        if (HighReadyBlackedArmTime >= 0.4f)
                        {
                            DoHighReadyInjuredAnim = false;
                            IsLowReady = true;
                            WasLowReady = IsLowReady;
                            IsHighReady = false;
                            WasHighReady = false;
                            HighReadyBlackedArmTime = 0f;
                        }
                    }

                    if ((PlayerProperties.LeftArmRuined || PlayerProperties.RightArmRuined) && !Plugin.IsAiming && !IsShortStock && !IsActiveAiming && !IsHighReady)
                    {
                        Plugin.StanceBlender.Target = 1f;
                        IsLowReady = true;
                        WasLowReady = true;
                    }
                }

                HighReadyManipBuff = !PlayerProperties.playerIsScav && PlayerProperties.IsSprinting && Plugin.EnableTacSprint.Value && !PlayerProperties.LeftArmRuined && !PlayerProperties.RightArmRuined ? 1.2f : 1f;
                ActiveAimManipBuff = !PlayerProperties.playerIsScav && IsActiveAiming && Plugin.ActiveAimReload.Value ? 1.25f : 1f;
                LowReadyManipBuff = !PlayerProperties.playerIsScav && IsLowReady ? 1.2f : 1f;

                if (DoResetStances)
                {
                    StanceManipCancelTimer();
                }

                if (Plugin.DidWeaponSwap || WeaponProperties._WeapClass == "pistol")
                {
                    if (Plugin.DidWeaponSwap)
                    {
                        PistolIsCompressed = false;
                        StanceTargetPosition = Vector3.zero;
                        Plugin.StanceBlender.Target = 0f;
                    }

                    SelectedStance = 0;
                    IsShortStock = false;
                    IsLowReady = false;
                    IsHighReady = false;
                    IsActiveAiming = false;
                    WasHighReady = false;
                    WasLowReady = false;
                    WasShortStock = false;
                    WasActiveAim = false;
                    Plugin.DidWeaponSwap = false;
                }
            }
        }

        public static void ToggleDevice(FirearmController fc, bool activating, ManualLogSource logger)
        {
            foreach (Mod mod in fc.Item.Mods)
            {
                LightComponent light;
                if (mod.TryGetItemComponent(out light))
                {
                    if (!LightDictionary.ContainsKey(mod.Id))
                    {
                        LightDictionary.Add(mod.Id, light.IsActive);
                    }

                    bool isOn = light.IsActive;
                    bool state = false;

                    if (!activating && isOn)
                    {
                        state = false;
                        LightDictionary[mod.Id] = true;
                    }
                    if (!activating && !isOn)
                    {
                        LightDictionary[mod.Id] = false;
                        return;
                    }
                    if (activating && isOn)
                    {
                        return;
                    }
                    if (activating && !isOn && LightDictionary[mod.Id])
                    {
                        state = true;
                    }
                    else if (activating && !isOn)
                    {
                        return;
                    }

                    fc.SetLightsState(new GStruct143[]
                    {
                        new GStruct143
                        {
                            Id = light.Item.Id,
                            IsActive = state,
                            LightMode = light.SelectedMode
                        }
                    }, false);
                }
            }
        }

        //move this to the patch classes
        public static float currentX = 0f;

        public static void DoPistolStances(bool isThirdPerson, ref EFT.Animations.ProceduralWeaponAnimation __instance, ref Quaternion stanceRotation, float dt, ref bool hasResetPistolPos, Player player, ManualLogSource logger, ref float rotationSpeed, ref bool isResettingPistol)
        {
            float ergoMulti = Mathf.Clamp(WeaponProperties.ErgoStanceSpeed, 0.65f, 1.45f);
            float stanceMulti = Mathf.Clamp(ergoMulti * PlayerProperties.StanceInjuryMulti * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.65f)), 0.5f, 1.45f);
            float invInjuryMulti = (1f - PlayerProperties.StanceInjuryMulti) + 1f;
            float resetErgoMulti = (1f - stanceMulti) + 1f;
            float ergoDelta = (1f - WeaponProperties.ErgoDelta);
            float intensity = Mathf.Max(1f * (1f - PlayerProperties.WeaponSkillErgo) * resetErgoMulti * invInjuryMulti * ergoDelta, 0.35f);
            float balanceFactor = 1f + (WeaponProperties.Balance / 100f);
            balanceFactor = WeaponProperties.Balance > 0f ? balanceFactor * -1f : balanceFactor;

            Vector3 pistolTargetPosition = new Vector3(Plugin.PistolOffsetX, Plugin.PistolOffsetY, Plugin.PistolOffsetZ);
            Vector3 pistolTargetRotation = new Vector3(Plugin.PistolRotationX, Plugin.PistolRotationY, Plugin.PistolRotationZ);
            Quaternion pistolTargetQuaternion = Quaternion.Euler(pistolTargetRotation);
            Quaternion pistolMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.PistolAdditionalRotationX, Plugin.PistolAdditionalRotationY, Plugin.PistolAdditionalRotationZ));
            Quaternion pistolRevertQuaternion = Quaternion.Euler(Plugin.PistolResetRotationX * balanceFactor, Plugin.PistolResetRotationY, Plugin.PistolResetRotationZ);

            //I've no idea wtf is going on here but it sort of works
            float targetPos = 0.09f;
            if (!Plugin.IsBlindFiring && !CancelPistolStance)
            {
                targetPos = Plugin.PistolOffsetX;
            }
            currentX = Mathf.Lerp(currentX, targetPos, dt * Plugin.PistolPosSpeedMulti * stanceMulti * 0.5f);

            __instance.HandsContainer.WeaponRoot.localPosition = new Vector3(currentX, __instance.HandsContainer.TrackingTransform.localPosition.y, __instance.HandsContainer.TrackingTransform.localPosition.z);

            if (!__instance.IsAiming && !CancelPistolStance && !PistolIsColliding && !Plugin.IsBlindFiring)
            {
                PistolIsCompressed = true;
                isResettingPistol = false;
                hasResetPistolPos = false;

                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                Plugin.StanceBlender.Speed = Plugin.PistolPosSpeedMulti * stanceMulti;
                StanceTargetPosition = Vector3.Lerp(StanceTargetPosition, pistolTargetPosition, Plugin.StanceTransitionSpeed * stanceMulti * dt);

                rotationSpeed = 4f * stanceMulti * dt * Plugin.PistolRotationSpeedMulti * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed : 1f);
                stanceRotation = pistolTargetQuaternion;
                if (Plugin.StanceBlender.Value < 1f)
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.PistolAdditionalRotationSpeedMulti * stanceMulti;
                    stanceRotation = pistolMiniTargetQuaternion;
                }
            }
            else if (Plugin.StanceBlender.Value > 0f && !hasResetPistolPos)
            {
                isResettingPistol = true;

                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = intensity;
                }

                rotationSpeed = 4f * stanceMulti * dt * Plugin.PistolResetRotationSpeedMulti * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed : 1f);
                stanceRotation = pistolRevertQuaternion;
                Plugin.StanceBlender.Speed = Plugin.PistolPosResetSpeedMulti * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed : 1f);
            }
            else if (Plugin.StanceBlender.Value == 0f && !hasResetPistolPos)
            {
                isResettingPistol = false;

                PistolIsCompressed = false;

                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                stanceRotation = Quaternion.identity;

                hasResetPistolPos = true;
            }
        }

        public static void DoRifleStances(ManualLogSource logger, Player player, FirearmController fc, bool isThirdPerson, ref EFT.Animations.ProceduralWeaponAnimation __instance, ref Quaternion stanceRotation, float dt, ref bool isResettingShortStock, ref bool hasResetShortStock, ref bool hasResetLowReady, ref bool hasResetActiveAim, ref bool hasResetHighReady, ref bool isResettingHighReady, ref bool isResettingLowReady, ref bool isResettingActiveAim, ref float rotationSpeed)
        {
            float ergoMulti = Mathf.Clamp(1f - ((1f - WeaponProperties.ErgoStanceSpeed) * 1.5f), 0.6f, 0.98f);
            float stanceMulti = Mathf.Clamp(ergoMulti * PlayerProperties.StanceInjuryMulti * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.65f)), 0.45f, 0.95f);
            float invInjuryMulti = (1f - PlayerProperties.StanceInjuryMulti) + 1f;
            float resetErgoMulti = (1f - stanceMulti) + 1f;
            float stocklessModifier = WeaponProperties.HasShoulderContact ? 1f : 2.4f;
            float ergoDelta = (1f - WeaponProperties.ErgoDelta);
            float intensity = Mathf.Max(1.5f * (1f - (PlayerProperties.AimSkillADSBuff * 0.5f)) * resetErgoMulti * invInjuryMulti * stocklessModifier * ergoDelta, 0.5f);

            float pitch = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_14").GetValue(__instance);

            bool isColliding = !__instance.OverlappingAllowsBlindfire;
            float collisionRotationFactor = isColliding ? 2f : 1f;
            float collisionPositionFactor = isColliding ? 2f : 1f;

            Vector3 activeAimTargetRotation = new Vector3(Plugin.ActiveAimRotationX, Plugin.ActiveAimRotationY, Plugin.ActiveAimRotationZ);
            Vector3 activeAimRevertRotation = new Vector3(Plugin.ActiveAimResetRotationX * resetErgoMulti, Plugin.ActiveAimResetRotationY * resetErgoMulti, Plugin.ActiveAimResetRotationZ * resetErgoMulti);
            Quaternion activeAimTargetQuaternion = Quaternion.Euler(activeAimTargetRotation);
            Quaternion activeAimMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.ActiveAimAdditionalRotationX * resetErgoMulti, Plugin.ActiveAimAdditionalRotationY * resetErgoMulti, Plugin.ActiveAimAdditionalRotationZ * resetErgoMulti));
            Quaternion activeAimRevertQuaternion = Quaternion.Euler(activeAimRevertRotation);
            Vector3 activeAimTargetPosition = new Vector3(Plugin.ActiveAimOffsetX, Plugin.ActiveAimOffsetY, Plugin.ActiveAimOffsetZ);

            Vector3 lowReadyTargetRotation = new Vector3(Plugin.LowReadyRotationX * collisionRotationFactor * resetErgoMulti, Plugin.LowReadyRotationY, Plugin.LowReadyRotationZ);
            Quaternion lowReadyTargetQuaternion = Quaternion.Euler(lowReadyTargetRotation);
            Quaternion lowReadyMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.LowReadyAdditionalRotationX * resetErgoMulti, Plugin.LowReadyAdditionalRotationY * resetErgoMulti, Plugin.LowReadyAdditionalRotationZ * resetErgoMulti));
            Quaternion lowReadyRevertQuaternion = Quaternion.Euler(Plugin.LowReadyResetRotationX * resetErgoMulti, Plugin.LowReadyResetRotationY * resetErgoMulti, Plugin.LowReadyResetRotationZ * resetErgoMulti);
            Vector3 lowReadyTargetPosition = new Vector3(Plugin.LowReadyOffsetX, Plugin.LowReadyOffsetY, Plugin.LowReadyOffsetZ);

            Vector3 highReadyTargetRotation = new Vector3(Plugin.HighReadyRotationX * stanceMulti * collisionRotationFactor, Plugin.HighReadyRotationY * stanceMulti, Plugin.HighReadyRotationZ * stanceMulti);
            Quaternion highReadyTargetQuaternion = Quaternion.Euler(highReadyTargetRotation);
            Quaternion highReadyMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.HighReadyAdditionalRotationX * resetErgoMulti, Plugin.HighReadyAdditionalRotationY * resetErgoMulti, Plugin.HighReadyAdditionalRotationZ * resetErgoMulti));
            Quaternion highReadyRevertQuaternion = Quaternion.Euler(Plugin.HighReadyResetRotationX * resetErgoMulti, Plugin.HighReadyResetRotationY * resetErgoMulti, Plugin.HighReadyResetRotationZ * resetErgoMulti);
            Vector3 highReadyTargetPosition = new Vector3(Plugin.HighReadyOffsetX, Plugin.HighReadyOffsetY, Plugin.HighReadyOffsetZ);

            Vector3 shortStockTargetRotation = new Vector3(Plugin.ShortStockRotationX * stanceMulti, Plugin.ShortStockRotationY * stanceMulti, Plugin.ShortStockRotationZ * stanceMulti);
            Quaternion shortStockTargetQuaternion = Quaternion.Euler(shortStockTargetRotation);
            Quaternion shortStockMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.ShortStockAdditionalRotationX * resetErgoMulti, Plugin.ShortStockAdditionalRotationY * resetErgoMulti, Plugin.ShortStockAdditionalRotationZ * resetErgoMulti));
            Quaternion shortStockRevertQuaternion = Quaternion.Euler(Plugin.ShortStockResetRotationX * resetErgoMulti, Plugin.ShortStockResetRotationY * resetErgoMulti, Plugin.ShortStockResetRotationZ * resetErgoMulti);
            Vector3 shortStockTargetPosition = new Vector3(Plugin.ShortStockOffsetX, Plugin.ShortStockOffsetY, Plugin.ShortStockOffsetZ);

            //for setting baseline position

            if (!Plugin.IsBlindFiring)
            {
                __instance.HandsContainer.WeaponRoot.localPosition = Plugin.WeaponOffsetPosition;
            }

            if (!PlayerProperties.playerIsScav && Plugin.EnableTacSprint.Value && !PlayerProperties.LeftArmRuined && !PlayerProperties.RightArmRuined)
            {
                player.BodyAnimatorCommon.SetFloat(GClass1647.WEAPON_SIZE_MODIFIER_PARAM_HASH, 2f);
                if (!setRunAnim)
                {
                    setRunAnim = true;
                    resetRunAnim = false;
                }
            }
            else if (Plugin.EnableTacSprint.Value)
            {
                if (!resetRunAnim)
                {
                    player.BodyAnimatorCommon.SetFloat(GClass1647.WEAPON_SIZE_MODIFIER_PARAM_HASH, fc.Item.CalculateCellSize().X);
                    resetRunAnim = true;
                    setRunAnim = false;
                }
            }

            if (!IsActiveAiming && !IsShortStock)
            {
                __instance.Breath.HipPenalty = WeaponProperties.BaseHipfireInaccuracy * 1.2f;
            }
            if (IsActiveAiming)
            {
                __instance.Breath.HipPenalty = WeaponProperties.BaseHipfireInaccuracy * 0.8f;
            }

            if (Plugin.StanceToggleDevice)
            {
                if (!toggledLight && (IsHighReady || IsLowReady))
                {
                    ToggleDevice(fc, false, logger);
                    toggledLight = true;
                }
                if (toggledLight && !IsHighReady && !IsLowReady)
                {
                    ToggleDevice(fc, true, logger);
                    toggledLight = false;
                }
            }

            ////short-stock////
            if (IsShortStock && !IsActiveAiming && !IsHighReady && !IsLowReady && !__instance.IsAiming && !CancelShortStock && !Plugin.IsBlindFiring && !PlayerProperties.IsSprinting)
            {
                __instance.Breath.HipPenalty = WeaponProperties.BaseHipfireInaccuracy * 1.5f;

                float activeToShort = 1f;
                float highToShort = 1f;
                float lowToShort = 1f;
                isResettingShortStock = false;
                hasResetShortStock = false;

                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                if (StanceTargetPosition != shortStockTargetPosition)
                {
                    if (!hasResetActiveAim)
                    {
                        activeToShort = 1.75f;
                    }
                    if (!hasResetHighReady)
                    {
                        highToShort = 1.15f;
                    }
                    if (!hasResetLowReady)
                    {
                        lowToShort = 1.15f;
                    }
                }
                if (StanceTargetPosition == shortStockTargetPosition)
                {
                    hasResetActiveAim = true;
                    hasResetHighReady = true;
                    hasResetLowReady = true;
                }

                float transitionSpeedFactor = activeToShort * highToShort * lowToShort;

                rotationSpeed = 4f * stanceMulti * dt * Plugin.ShortStockRotationMulti * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed : 1f) * transitionSpeedFactor;
                stanceRotation = shortStockTargetQuaternion;

                if (Plugin.StanceBlender.Value < 1f)
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.ShortStockAdditionalRotationSpeedMulti * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed : 1f) * transitionSpeedFactor;
                    stanceRotation = shortStockMiniTargetQuaternion;
                }

                Plugin.StanceBlender.Speed = Plugin.ShortStockSpeedMulti * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed : 1f);
                StanceTargetPosition = Vector3.Lerp(StanceTargetPosition, shortStockTargetPosition, Plugin.StanceTransitionSpeed * stanceMulti * dt);
            }
            else if (Plugin.StanceBlender.Value > 0f && !hasResetShortStock && !IsLowReady && !IsActiveAiming && !IsHighReady && !isResettingActiveAim && !isResettingHighReady && !isResettingLowReady)
            {
                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = intensity;
                }

                isResettingShortStock = true;
                rotationSpeed = 4f * stanceMulti * dt * Plugin.ShortStockResetRotationSpeedMulti;
                stanceRotation = shortStockRevertQuaternion;
                Plugin.StanceBlender.Speed = Plugin.ShortStockResetSpeedMulti * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed : 1f);
            }
            else if (Plugin.StanceBlender.Value == 0f && !hasResetShortStock)
            {
                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                stanceRotation = Quaternion.identity;

                isResettingShortStock = false;
                hasResetShortStock = true;
            }

            ////high ready////
            if (IsHighReady && !IsActiveAiming && !IsLowReady && !IsShortStock && !__instance.IsAiming && !IsFiringFromStance && !CancelHighReady && !Plugin.IsBlindFiring)
            {
                float shortToHighMulti = 1.0f;
                float lowToHighMulti = 1.0f;
                float activeToHighMulti = 1.0f;
                isResettingHighReady = false;
                hasResetHighReady = false;

                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                if (StanceTargetPosition != highReadyTargetPosition)
                {
                    if (!hasResetShortStock)
                    {
                        shortToHighMulti = 1.15f;
                    }
                    if (!hasResetActiveAim)
                    {
                        activeToHighMulti = 1.85f;
                    }
                    if (!hasResetLowReady)
                    {
                        lowToHighMulti = 1.25f;
                    }
                }
                if (StanceTargetPosition == highReadyTargetPosition)
                {
                    hasResetActiveAim = true;
                    hasResetLowReady = true;
                    hasResetShortStock = true;
                }

                float transitionSpeedFactor = shortToHighMulti * lowToHighMulti * activeToHighMulti;

                if (DoHighReadyInjuredAnim)
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.HighReadyAdditionalRotationSpeedMulti * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed : 1f);
                    stanceRotation = highReadyMiniTargetQuaternion;
                    if (Plugin.StanceBlender.Value < 1f)
                    {
                        rotationSpeed = 4f * stanceMulti * dt * Plugin.HighReadyRotationMulti * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed : 1f);
                        stanceRotation = lowReadyTargetQuaternion;
                    }
                }
                else
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.HighReadyRotationMulti * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed : 1f) * transitionSpeedFactor;
                    stanceRotation = highReadyTargetQuaternion;
                    if (Plugin.StanceBlender.Value < 1f)
                    {
                        rotationSpeed = 4f * stanceMulti * dt * Plugin.HighReadyAdditionalRotationSpeedMulti * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed : 1f) * transitionSpeedFactor;
                        stanceRotation = highReadyMiniTargetQuaternion;
                    }
                }

                Plugin.StanceBlender.Speed = Plugin.HighReadySpeedMulti * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed : 1f);
                StanceTargetPosition = Vector3.Lerp(StanceTargetPosition, highReadyTargetPosition, Plugin.StanceTransitionSpeed * stanceMulti * dt);

            }
            else if (Plugin.StanceBlender.Value > 0f && !hasResetHighReady && !IsLowReady && !IsActiveAiming && !IsShortStock && !isResettingActiveAim && !isResettingLowReady && !isResettingShortStock)
            {
                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = -intensity;
                }

                isResettingHighReady = true;
                rotationSpeed = 4f * stanceMulti * dt * Plugin.HighReadyResetRotationMulti * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed : 1f);
                stanceRotation = highReadyRevertQuaternion;

                Plugin.StanceBlender.Speed = Plugin.HighReadyResetSpeedMulti * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed : 1f);
            }
            else if (Plugin.StanceBlender.Value == 0f && !hasResetHighReady)
            {
                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                stanceRotation = Quaternion.identity;

                isResettingHighReady = false;
                hasResetHighReady = true;
            }

            ////low ready////
            if (IsLowReady && !IsActiveAiming && !IsHighReady && !IsShortStock && !__instance.IsAiming && !IsFiringFromStance && !CancelLowReady && !Plugin.IsBlindFiring)
            {
                float highToLow = 1.0f;
                float shortToLow = 1.0f;
                float activeToLow = 1.0f;
                isResettingLowReady = false;
                hasResetLowReady = false;

                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                if (StanceTargetPosition != lowReadyTargetPosition)
                {
                    if (!hasResetHighReady)
                    {
                        highToLow = 1.25f;
                    }
                    if (!hasResetShortStock)
                    {
                        shortToLow = 1.15f;
                    }
                    if (!hasResetActiveAim)
                    {
                        activeToLow = 1.85f;
                    }

                }
                if (StanceTargetPosition == lowReadyTargetPosition)
                {
                    hasResetHighReady = true;
                    hasResetShortStock = true;
                    hasResetActiveAim = true;
                }

                float transitionSpeedFactor = highToLow * shortToLow * activeToLow;

                rotationSpeed = 4f * stanceMulti * dt * Plugin.LowReadyRotationMulti * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed : 1f) * transitionSpeedFactor;
                stanceRotation = lowReadyTargetQuaternion;
                if (Plugin.StanceBlender.Value < 1f)
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.LowReadyAdditionalRotationSpeedMulti * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed : 1f) * transitionSpeedFactor;
                    stanceRotation = lowReadyMiniTargetQuaternion;
                }

                Plugin.StanceBlender.Speed = Plugin.LowReadySpeedMulti * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed : 1f);
                StanceTargetPosition = Vector3.Lerp(StanceTargetPosition, lowReadyTargetPosition, Plugin.StanceTransitionSpeed * stanceMulti * dt);

            }
            else if (Plugin.StanceBlender.Value > 0f && !hasResetLowReady && !IsActiveAiming && !IsHighReady && !IsShortStock && !isResettingActiveAim && !isResettingHighReady && !isResettingShortStock)
            {

                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = intensity;
                }

                isResettingLowReady = true;
                rotationSpeed = 4f * stanceMulti * dt * Plugin.LowReadyResetRotationMulti * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed : 1f);
                stanceRotation = lowReadyRevertQuaternion;

                Plugin.StanceBlender.Speed = Plugin.LowReadyResetSpeedMulti * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed : 1f);

            }
            else if (Plugin.StanceBlender.Value == 0f && !hasResetLowReady)
            {
                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                stanceRotation = Quaternion.identity;

                isResettingLowReady = false;
                hasResetLowReady = true;
            }

            ////active aiming////
            if (IsActiveAiming && !__instance.IsAiming && !IsLowReady && !IsShortStock && !IsHighReady && !CancelActiveAim && !Plugin.IsBlindFiring)
            {
                float shortToActive = 1f;
                float highToActive = 1f;
                float lowToActive = 1f;
                isResettingActiveAim = false;
                hasResetActiveAim = false;

                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                if (StanceTargetPosition != activeAimTargetPosition)
                {
                    if (!hasResetShortStock)
                    {
                        shortToActive = 2.15f;
                    }
                    if (!hasResetHighReady)
                    {
                        highToActive = 2.4f;
                    }
                    if (!hasResetLowReady)
                    {
                        lowToActive = 2.25f;
                    }
                }
                if (StanceTargetPosition == activeAimTargetPosition)
                {
                    hasResetShortStock = true;
                    hasResetHighReady = true;
                    hasResetLowReady = true;
                }

                float transitionSpeedFactor = shortToActive * highToActive * lowToActive;

                rotationSpeed = 4f * stanceMulti * dt * Plugin.ActiveAimRotationMulti * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed : 1f) * transitionSpeedFactor;
                stanceRotation = activeAimTargetQuaternion;
                if (Plugin.StanceBlender.Value < 1f)
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.ActiveAimAdditionalRotationSpeedMulti * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed : 1f) * transitionSpeedFactor;
                    stanceRotation = activeAimMiniTargetQuaternion;
                }

                Plugin.StanceBlender.Speed = Plugin.ActiveAimSpeedMulti * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed : 1f);
                StanceTargetPosition = Vector3.Lerp(StanceTargetPosition, activeAimTargetPosition, Plugin.StanceTransitionSpeed * stanceMulti * dt);
            }
            else if (Plugin.StanceBlender.Value > 0f && !hasResetActiveAim && !IsLowReady && !IsHighReady && !IsShortStock && !isResettingLowReady && !isResettingHighReady && !isResettingShortStock)
            {
                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = intensity;
                }

                isResettingActiveAim = true;
                rotationSpeed = stanceMulti * dt * Plugin.ActiveAimResetRotationSpeedMulti * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed : 1f);
                stanceRotation = activeAimRevertQuaternion;
                Plugin.StanceBlender.Speed = Plugin.ActiveAimResetSpeedMulti * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed : 1f);
            }
            else if (Plugin.StanceBlender.Value == 0f && hasResetActiveAim == false)
            {
                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                stanceRotation = Quaternion.identity;

                isResettingActiveAim = false;
                hasResetActiveAim = true;
            }
        }
    }

    public class LaserLateUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(LaserBeam).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static bool Prefix(LaserBeam __instance)
        {
            if (Utils.IsReady)
            {
                if ((StanceController.IsHighReady == true || StanceController.IsLowReady == true) && !Plugin.IsAiming)
                {
                    Vector3 playerPos = Singleton<GameWorld>.Instance.AllPlayers[0].Transform.position;
                    Vector3 lightPos = __instance.gameObject.transform.position;
                    float distanceFromPlayer = Vector3.Distance(lightPos, playerPos);
                    if (distanceFromPlayer <= 1.8f)
                    {
                        return false;
                    }
                    return true;
                }
                return true;
            }
            else
            {
                return true;
            }
        }
    }

    public class OnWeaponDrawPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SkillsClass).GetMethod("OnWeaponDraw", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(SkillsClass __instance, Item item)
        {
            if (item?.Owner?.ID != null && (item.Owner.ID.StartsWith("pmc") || item.Owner.ID.StartsWith("scav")))
            {
                Plugin.DidWeaponSwap = true;
            }
        }
    }

    public class SetFireModePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmsAnimator).GetMethod("SetFireMode", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(FirearmsAnimator __instance, Weapon.EFireMode fireMode, bool skipAnimation = false)
        {
            __instance.ResetLeftHand();
            skipAnimation = !PlayerProperties.playerIsScav && WeaponProperties._WeapClass != "pistol" & Plugin.EnableTacSprint.Value & PlayerProperties.IsSprinting ? true : skipAnimation;
            WeaponAnimationSpeedControllerClass.SetFireMode(__instance.Animator, (float)fireMode);
            if (!skipAnimation)
            {
                WeaponAnimationSpeedControllerClass.TriggerFiremodeSwitch(__instance.Animator);
            }
            return false;
        }
    }

    public class WeaponLengthPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmController).GetMethod("method_7", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(FirearmController __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(FirearmController), "_player").GetValue(__instance);
            float length = (float)AccessTools.Field(typeof(FirearmController), "WeaponLn").GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                WeaponProperties.BaseWeaponLength = length;
                WeaponProperties.NewWeaponLength = length >= 0.9f ? length * 1.15f : length;
            }
        }
    }

    public class WeaponOverlappingPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmController).GetMethod("WeaponOverlapping", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void Prefix(FirearmController __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(FirearmController), "_player").GetValue(__instance);

            if (player.IsYourPlayer == true)
            {

                if ((StanceController.IsHighReady == true || StanceController.IsLowReady == true || StanceController.IsShortStock == true))
                {
                    AccessTools.Field(typeof(FirearmController), "WeaponLn").SetValue(__instance, WeaponProperties.NewWeaponLength * 0.8f);
                    return;
                }
                if (StanceController.WasShortStock == true && Plugin.IsAiming)
                {
                    AccessTools.Field(typeof(FirearmController), "WeaponLn").SetValue(__instance, WeaponProperties.NewWeaponLength * 0.7f);
                    return;
                }
                if (__instance.Item.WeapClass == "pistol")
                {
                    if (StanceController.PistolIsCompressed == true)
                    {
                        AccessTools.Field(typeof(FirearmController), "WeaponLn").SetValue(__instance, WeaponProperties.NewWeaponLength * 0.7f);
                    }
                    else
                    {
                        AccessTools.Field(typeof(FirearmController), "WeaponLn").SetValue(__instance, WeaponProperties.NewWeaponLength * 0.9f);
                    }
                    return;
                }
                AccessTools.Field(typeof(FirearmController), "WeaponLn").SetValue(__instance, WeaponProperties.NewWeaponLength);
                return;
            }
        }
    }

    public class WeaponOverlapViewPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmController).GetMethod("WeaponOverlapView", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void Prefix(FirearmController __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(FirearmController), "_player").GetValue(__instance);
            float float_0 = (float)AccessTools.Field(typeof(FirearmController), "float_0").GetValue(__instance);

            if (float_0 > EFTHardSettings.Instance.STOP_AIMING_AT && __instance.IsAiming)
            {
                Plugin.IsAiming = true;
                return;
            }
        }
    }

    public class InitTransformsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("InitTransforms", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(EFT.Animations.ProceduralWeaponAnimation __instance)
        {
            FirearmController firearmController = (FirearmController)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "firearmController_0").GetValue(__instance);

            if (firearmController != null)
            {
                Player player = (Player)AccessTools.Field(typeof(FirearmController), "_player").GetValue(firearmController);
                if (player.IsYourPlayer == true)
                {
                    Plugin.WeaponOffsetPosition = __instance.HandsContainer.WeaponRoot.localPosition += new Vector3(Plugin.WeapOffsetX, Plugin.WeapOffsetY, Plugin.WeapOffsetZ);
                    __instance.HandsContainer.WeaponRoot.localPosition += new Vector3(Plugin.WeapOffsetX, Plugin.WeapOffsetY, Plugin.WeapOffsetZ);
                    Plugin.TransformBaseStartPosition = new Vector3(0.0f, 0.0f, 0.0f);
                }
            }
        }
    }


    public class ZeroAdjustmentsPatch : ModulePatch
    {
        private static Vector3 targetPosition = Vector3.zero;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("ZeroAdjustments", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(EFT.Animations.ProceduralWeaponAnimation __instance)
        {
            FirearmController firearmController = (FirearmController)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "firearmController_0").GetValue(__instance);

            if (firearmController != null)
            {
                Player player = (Player)AccessTools.Field(typeof(FirearmController), "_player").GetValue(firearmController);
                if (player.IsYourPlayer == true)
                {
                    float isColliding = (float)AccessTools.Property(typeof(EFT.Animations.ProceduralWeaponAnimation), "Single_3").GetValue(__instance);
                    Vector3 blindfirePosition = (Vector3)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "vector3_5").GetValue(__instance);

                    Vector3 highReadyTargetPosition = new Vector3(Plugin.HighReadyOffsetX, Plugin.HighReadyOffsetY, Plugin.HighReadyOffsetZ);

                    __instance.PositionZeroSum.y = (__instance._shouldMoveWeaponCloser ? 0.05f : 0f);
                    __instance.RotationZeroSum.y = __instance.SmoothedTilt * __instance.PossibleTilt;

                    float stanceBlendValue = Plugin.StanceBlender.Value;
                    float stanceAbs = Mathf.Abs(stanceBlendValue);

                    float blindFireBlendValue = __instance.BlindfireBlender.Value;
                    float blindFireAbs = Mathf.Abs(blindFireBlendValue);

                    if (blindFireAbs > 0f)
                    {
                        Plugin.IsBlindFiring = true;
                        float pitch = ((Mathf.Abs(__instance.Pitch) < 45f) ? 1f : ((90f - Mathf.Abs(__instance.Pitch)) / 45f));
                        AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_14").SetValue(__instance, pitch);
                        AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "vector3_6").SetValue(__instance, ((blindFireBlendValue > 0f) ? (__instance.BlindFireRotation * blindFireAbs) : (__instance.SideFireRotation * blindFireAbs)));
                        targetPosition = ((blindFireBlendValue > 0f) ? (__instance.BlindFireOffset * blindFireAbs) : (__instance.SideFireOffset * blindFireAbs));
                        targetPosition += StanceController.StanceTargetPosition;
                        __instance.BlindFireEndPosition = ((blindFireBlendValue > 0f) ? __instance.BlindFireOffset : __instance.SideFireOffset);
                        __instance.BlindFireEndPosition *= pitch;
                        __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * isColliding * targetPosition;
                        __instance.HandsContainer.HandsRotation.Zero = __instance.RotationZeroSum;

                        return false;
                    }

                    Plugin.IsBlindFiring = false;

                    if (stanceAbs > 0f)
                    {
                        float pitch = ((Mathf.Abs(__instance.Pitch) < 45f) ? 1f : ((90f - Mathf.Abs(__instance.Pitch)) / 45f));
                        AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_14").SetValue(__instance, pitch);
                        targetPosition = StanceController.StanceTargetPosition * stanceAbs;
                        __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * targetPosition;
                        __instance.HandsContainer.HandsRotation.Zero = __instance.RotationZeroSum;
                        return false;
                    }

                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + isColliding * targetPosition;
                    __instance.HandsContainer.HandsRotation.Zero = __instance.RotationZeroSum;
                    return false;
                }
            }
            return true;
        }
    }

    public class ApplySimpleRotationPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("ApplySimpleRotation", BindingFlags.Instance | BindingFlags.Public);
        }

        private static bool hasResetActiveAim = true;
        private static bool hasResetLowReady = true;
        private static bool hasResetHighReady = true;
        private static bool hasResetShortStock = true;
        private static bool hasResetPistolPos = true;

        private static bool isResettingActiveAim = false;
        private static bool isResettingLowReady = false;
        private static bool isResettingHighReady = false;
        private static bool isResettingShortStock = false;
        private static bool isResettingPistol = false;

        private static Quaternion currentRotation = Quaternion.identity;
        private static Quaternion stanceRotation = Quaternion.identity;

        private static float stanceSpeed = 1f;

        [PatchPostfix]
        private static void Postfix(ref EFT.Animations.ProceduralWeaponAnimation __instance, float dt)
        {
            FirearmController firearmController = (FirearmController)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "firearmController_0").GetValue(__instance);

            if (firearmController != null)
            {
                Player player = (Player)AccessTools.Field(typeof(FirearmController), "_player").GetValue(firearmController);

                if (player.IsYourPlayer == true)
                {
                    Plugin.IsInThirdPerson = true;

                    float float_9 = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").GetValue(__instance);
                    float float_14 = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_14").GetValue(__instance);
                    Vector3 vector3_4 = (Vector3)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "vector3_4").GetValue(__instance);
                    Vector3 vector3_6 = (Vector3)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "vector3_6").GetValue(__instance);
                    Quaternion quaternion_2 = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_2").GetValue(__instance);
                    Quaternion quaternion_6 = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_6").GetValue(__instance);
                    bool bool_1 = (bool)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "bool_1").GetValue(__instance);
                    float Single_3 = (float)AccessTools.Property(typeof(EFT.Animations.ProceduralWeaponAnimation), "Single_3").GetValue(__instance);

                    bool isPistol = firearmController.Item.WeapClass == "pistol";
                    bool allStancesReset = hasResetActiveAim && hasResetLowReady && hasResetHighReady && hasResetShortStock && hasResetPistolPos;
                    bool isInStance = StanceController.IsHighReady || StanceController.IsLowReady || StanceController.IsShortStock || StanceController.IsActiveAiming;
                    bool isInShootableStance = StanceController.IsShortStock || StanceController.IsActiveAiming || isPistol;
                    bool cancelBecauseSooting = StanceController.IsFiringFromStance && !StanceController.IsActiveAiming && !StanceController.IsShortStock && !isPistol;
                    bool doStanceRotation = (isInStance || !allStancesReset || StanceController.PistolIsCompressed) && !cancelBecauseSooting;
                    bool allowActiveAimReload = !PlayerProperties.playerIsScav && Plugin.ActiveAimReload.Value && PlayerProperties.IsInReloadOpertation && !PlayerProperties.IsAttemptingToReloadInternalMag && !PlayerProperties.IsQuickReloading;
                    bool cancelStance = (StanceController.CancelActiveAim && StanceController.IsActiveAiming && !allowActiveAimReload) || (StanceController.CancelHighReady && StanceController.IsHighReady) || (StanceController.CancelLowReady && StanceController.IsLowReady) || (StanceController.CancelShortStock && StanceController.IsShortStock) || (StanceController.CancelPistolStance && StanceController.PistolIsCompressed);

                    currentRotation = Quaternion.Slerp(currentRotation, __instance.IsAiming && allStancesReset ? quaternion_2 : doStanceRotation ? stanceRotation : Quaternion.identity, doStanceRotation ? stanceSpeed : __instance.IsAiming ? 8f * float_9 * dt : 8f * dt);

                    Quaternion rhs = Quaternion.Euler(float_14 * Single_3 * vector3_6);
                    __instance.HandsContainer.WeaponRootAnim.SetPositionAndRotation(vector3_4, quaternion_6 * rhs * currentRotation);

                    if (!Plugin.IsFiring)
                    {
                        __instance.HandsContainer.HandsPosition.Damping = 0.5f;
                    }

                    if (isPistol && !WeaponProperties.HasShoulderContact && Plugin.EnableAltPistol.Value && !PlayerProperties.playerIsScav)
                    {
                        if (StanceController.PistolIsCompressed && !Plugin.IsAiming && !isResettingPistol && !Plugin.IsBlindFiring)
                        {
                            Plugin.StanceBlender.Target = 1f;
                        }
                        else
                        {
                            Plugin.StanceBlender.Target = 0f;
                        }

                        if ((!StanceController.PistolIsCompressed && !Plugin.IsAiming && !isResettingPistol) || (Plugin.IsBlindFiring))
                        {
                            StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, Vector3.zero, 5f * dt);
                        }

                        hasResetActiveAim = true;
                        hasResetHighReady = true;
                        hasResetLowReady = true;
                        hasResetShortStock = true;
                        StanceController.DoPistolStances(true, ref __instance, ref stanceRotation, dt, ref hasResetPistolPos, player, Logger, ref stanceSpeed, ref isResettingPistol);
                    }
                    else
                    {
                        if ((!isInStance && allStancesReset) || (cancelBecauseSooting && !isInShootableStance) || Plugin.IsAiming || cancelStance || Plugin.IsBlindFiring)
                        {
                            Plugin.StanceBlender.Target = 0f;
                        }
                        else if (isInStance)
                        {
                            Plugin.StanceBlender.Target = 1f;
                        }

                        if (((!isInStance && allStancesReset) && !cancelBecauseSooting && !Plugin.IsAiming) || (Plugin.IsBlindFiring))
                        {
                            StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, Vector3.zero, 5f * dt);
                        }

                        hasResetPistolPos = true;
                        StanceController.DoRifleStances(Logger, player, firearmController, true, ref __instance, ref stanceRotation, dt, ref isResettingShortStock, ref hasResetShortStock, ref hasResetLowReady, ref hasResetActiveAim, ref hasResetHighReady, ref isResettingHighReady, ref isResettingLowReady, ref isResettingActiveAim, ref stanceSpeed);
                    }

                    StanceController.HasResetActiveAim = hasResetActiveAim;
                    StanceController.HasResetHighReady = hasResetHighReady;
                    StanceController.HasResetLowReady = hasResetLowReady;
                    StanceController.HasResetShortStock = hasResetShortStock;
                    StanceController.HasResetPistolPos = hasResetPistolPos;
                }
                else if (player.IsAI)
                {
                    Quaternion targetRotation = Quaternion.identity;
                    Quaternion quaternion_2 = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_2").GetValue(__instance);
                    Quaternion currentRotation = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_3").GetValue(__instance);
                    Quaternion quaternion_6 = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_6").GetValue(__instance);

                    float float_14 = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_14").GetValue(__instance);
                    Vector3 vector3_4 = (Vector3)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "vector3_4").GetValue(__instance);
                    Vector3 vector3_6 = (Vector3)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "vector3_6").GetValue(__instance);

                    Vector3 lowReadyTargetRotation = new Vector3(18.0f, 5.0f, -1.0f);
                    Quaternion lowReadyTargetQuaternion = Quaternion.Euler(lowReadyTargetRotation);
                    Vector3 lowReadyTargetPostion = new Vector3(0.06f, 0.04f, 0.0f);

                    Vector3 highReadyTargetRotation = new Vector3(-15.0f, 3.0f, 3.0f);
                    Quaternion highReadyTargetQuaternion = Quaternion.Euler(highReadyTargetRotation);
                    Vector3 highReadyTargetPostion = new Vector3(0.05f, 0.1f, -0.12f);

                    Vector3 activeAimTargetRotation = new Vector3(0.0f, -40.0f, 0.0f);
                    Quaternion activeAimTargetQuaternion = Quaternion.Euler(activeAimTargetRotation);
                    Vector3 activeAimTargetPostion = new Vector3(0.0f, 0.0f, 0.0f);

                    Vector3 shortStockTargetRotation = new Vector3(0.0f, -28.0f, 0.0f);
                    Quaternion shortStockTargetQuaternion = Quaternion.Euler(shortStockTargetRotation);
                    Vector3 shortStockTargetPostion = new Vector3(0.05f, 0.18f, -0.2f);

                    Vector3 tacPistolTargetRotation = new Vector3(0.0f, -20.0f, 0.0f);
                    Quaternion tacPistolTargetQuaternion = Quaternion.Euler(tacPistolTargetRotation);
                    Vector3 tacPistolTargetPosition = new Vector3(-0.1f, 0.1f, -0.05f);

                    Vector3 normalPistolTargetRotation = new Vector3(0f, -5.0f, 0.0f);
                    Quaternion normalPistolTargetQuaternion = Quaternion.Euler(normalPistolTargetRotation);
                    Vector3 normalPistolTargetPosition = new Vector3(-0.05f, 0.0f, 0.0f);

                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").SetValue(__instance, 1f);
                    float pitch = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_14").GetValue(__instance);
                    float Single_3 = (float)AccessTools.Property(typeof(EFT.Animations.ProceduralWeaponAnimation), "Single_3").GetValue(__instance);

                    FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
                    NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
                    bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);
                    bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);

                    float lastDistance = player.AIData.BotOwner.AimingData.LastDist2Target;
                    Vector3 distanceVect = player.AIData.BotOwner.AimingData.RealTargetPoint - player.AIData.BotOwner.MyHead.position;
                    float realDistance = distanceVect.magnitude;

                    bool isTacBot = StanceController.botsToUseTacticalStances.Contains(player.AIData.BotOwner.Profile.Info.Settings.Role.ToString());
                    bool isPeace = player.AIData.BotOwner.Memory.IsPeace;
                    bool notShooting = !player.AIData.BotOwner.ShootData.Shooting && Time.time - player.AIData.BotOwner.ShootData.LastTriggerPressd > 15f;
                    bool isInStance = false;
                    float stanceSpeed = 1f;

                    if (player.AIData.BotOwner.GetPlayer.MovementContext.BlindFire == 0)
                    {
                        if (isPeace && !player.IsSprintEnabled && player.MovementContext.StationaryWeapon == null && !__instance.IsAiming && !firearmController.IsInReloadOperation() && !firearmController.IsInventoryOpen() && !firearmController.IsInInteractionStrictCheck() && !firearmController.IsInSpawnOperation() && !firearmController.IsHandsProcessing()) // && player.AIData.BotOwner.WeaponManager.IsWeaponReady &&  player.AIData.BotOwner.WeaponManager.InIdleState()
                        {
                            isInStance = true;
                            player.HandsController.FirearmsAnimator.SetPatrol(true);
                        }
                        else
                        {
                            player.HandsController.FirearmsAnimator.SetPatrol(false);
                            if (firearmController.Item.WeapClass != "pistol")
                            {
                                ////low ready//// 
                                if (!isTacBot && !firearmController.IsInReloadOperation() && !player.IsSprintEnabled && !__instance.IsAiming && notShooting && (lastDistance >= 25f || lastDistance == 0f))    // (Time.time - player.AIData.BotOwner.Memory.LastEnemyTimeSeen) > 1f
                                {
                                    isInStance = true;
                                    stanceSpeed = 4f * dt * 3f;
                                    targetRotation = lowReadyTargetQuaternion;
                                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * lowReadyTargetPostion;
                                }

                                ////high ready////
                                if (isTacBot && !firearmController.IsInReloadOperation() && !__instance.IsAiming && notShooting && (lastDistance >= 25f || lastDistance == 0f))
                                {
                                    isInStance = true;
                                    player.BodyAnimatorCommon.SetFloat(GClass1647.WEAPON_SIZE_MODIFIER_PARAM_HASH, 2);
                                    stanceSpeed = 4f * dt * 2.7f;
                                    targetRotation = highReadyTargetQuaternion;
                                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * highReadyTargetPostion;
                                }
                                else
                                {
                                    player.BodyAnimatorCommon.SetFloat(GClass1647.WEAPON_SIZE_MODIFIER_PARAM_HASH, firearmController.Item.CalculateCellSize().X);
                                }

                                ///active aim//// 
                                if (isTacBot && (((nvgIsOn || fsIsON) && !player.IsSprintEnabled && !firearmController.IsInReloadOperation() && lastDistance < 25f && lastDistance > 2f && lastDistance != 0f) || (__instance.IsAiming && (nvgIsOn && __instance.CurrentScope.IsOptic || fsIsON))))
                                {
                                    isInStance = true;
                                    stanceSpeed = 4f * dt * 1.5f;
                                    targetRotation = activeAimTargetQuaternion;
                                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * activeAimTargetPostion;
                                }

                                ///short stock//// 
                                if (isTacBot && !player.IsSprintEnabled && !firearmController.IsInReloadOperation() && lastDistance <= 2f && lastDistance != 0f)
                                {
                                    isInStance = true;
                                    stanceSpeed = 4f * dt * 3f;
                                    targetRotation = shortStockTargetQuaternion;
                                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * shortStockTargetPostion;
                                }
                            }
                            else
                            {
                                if (!isTacBot && !player.IsSprintEnabled && !__instance.IsAiming && notShooting)
                                {
                                    isInStance = true;
                                    stanceSpeed = 4f * dt * 1.5f;
                                    targetRotation = normalPistolTargetQuaternion;
                                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * normalPistolTargetPosition;
                                }

                                if (isTacBot && !player.IsSprintEnabled && !__instance.IsAiming && notShooting)
                                {
                                    isInStance = true;
                                    stanceSpeed = 4f * dt * 1.5f;
                                    targetRotation = tacPistolTargetQuaternion;
                                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * tacPistolTargetPosition;
                                }
                            }
                        }
                    }
                    Quaternion rhs = Quaternion.Euler(float_14 * Single_3 * vector3_6);
                    currentRotation = Quaternion.Slerp(currentRotation, __instance.IsAiming && !isInStance ? quaternion_2 : isInStance ? targetRotation : Quaternion.identity, isInStance ? stanceSpeed : 8f * dt);
                    __instance.HandsContainer.WeaponRootAnim.SetPositionAndRotation(vector3_4, quaternion_6 * rhs * currentRotation);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_3").SetValue(__instance, currentRotation);
                }
            }
        }
    }

    public class ApplyComplexRotationPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("ApplyComplexRotation", BindingFlags.Instance | BindingFlags.Public);
        }

        private static bool hasResetActiveAim = true;
        private static bool hasResetLowReady = true;
        private static bool hasResetHighReady = true;
        private static bool hasResetShortStock = true;
        private static bool hasResetPistolPos = true;

        private static bool isResettingActiveAim = false;
        private static bool isResettingLowReady = false;
        private static bool isResettingHighReady = false;
        private static bool isResettingShortStock = false;
        private static bool isResettingPistol = false;

        private static Quaternion currentRotation = Quaternion.identity;
        private static Quaternion stanceRotation = Quaternion.identity;

        private static float stanceSpeed = 1f;

        [PatchPostfix]
        private static void Postfix(ref EFT.Animations.ProceduralWeaponAnimation __instance, float dt)
        {
            FirearmController firearmController = (FirearmController)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "firearmController_0").GetValue(__instance);

            if (firearmController != null)
            {
                Player player = (Player)AccessTools.Field(typeof(FirearmController), "_player").GetValue(firearmController);

                if (player.IsYourPlayer == true)
                {
                    Plugin.IsInThirdPerson = false;

                    float float_9 = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").GetValue(__instance);
                    float float_13 = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_13").GetValue(__instance);
                    float float_14 = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_14").GetValue(__instance);
                    float float_21 = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_21").GetValue(__instance);
                    Vector3 vector3_4 = (Vector3)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "vector3_4").GetValue(__instance);
                    Vector3 vector3_6 = (Vector3)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "vector3_6").GetValue(__instance);
                    Quaternion quaternion_2 = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_2").GetValue(__instance);
                    Quaternion quaternion_5 = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_5").GetValue(__instance);
                    Quaternion quaternion_6 = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_6").GetValue(__instance);
                    bool bool_1 = (bool)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "bool_1").GetValue(__instance);
                    float Single_3 = (float)AccessTools.Property(typeof(EFT.Animations.ProceduralWeaponAnimation), "Single_3").GetValue(__instance);

                    Vector3 vector = __instance.HandsContainer.HandsRotation.Get();
                    Vector3 value = __instance.HandsContainer.SwaySpring.Value;
                    vector += float_21 * (bool_1 ? __instance.AimingDisplacementStr : 1f) * new Vector3(value.x, 0f, value.z);
                    vector += value;
                    Vector3 position = __instance._shouldMoveWeaponCloser ? __instance.HandsContainer.RotationCenterWoStock : __instance.HandsContainer.RotationCenter;
                    Vector3 worldPivot = __instance.HandsContainer.WeaponRootAnim.TransformPoint(position);

                    __instance.DeferredRotateWithCustomOrder(__instance.HandsContainer.WeaponRootAnim, worldPivot, vector);
                    Vector3 vector2 = __instance.HandsContainer.Recoil.Get();
                    if (vector2.magnitude > 1E-45f)
                    {
                        if (float_13 < 1f && __instance.ShotNeedsFovAdjustments)
                        {
                            vector2.x = Mathf.Atan(Mathf.Tan(vector2.x * 0.017453292f) * float_13) * 57.29578f;
                            vector2.z = Mathf.Atan(Mathf.Tan(vector2.z * 0.017453292f) * float_13) * 57.29578f;
                        }
                        Vector3 worldPivot2 = vector3_4 + quaternion_6 * __instance.HandsContainer.RecoilPivot;
                        __instance.DeferredRotate(__instance.HandsContainer.WeaponRootAnim, worldPivot2, quaternion_6 * vector2);
                    }

                    __instance.ApplyAimingAlignment(dt);

                    bool isPistol = firearmController.Item.WeapClass == "pistol";
                    bool allStancesReset = hasResetActiveAim && hasResetLowReady && hasResetHighReady && hasResetShortStock && hasResetPistolPos;
                    bool isInStance = StanceController.IsHighReady || StanceController.IsLowReady || StanceController.IsShortStock || StanceController.IsActiveAiming;
                    bool isInShootableStance = StanceController.IsShortStock || StanceController.IsActiveAiming || isPistol;
                    bool cancelBecauseSooting = StanceController.IsFiringFromStance && !StanceController.IsActiveAiming && !StanceController.IsShortStock && !isPistol;
                    bool doStanceRotation = (isInStance || !allStancesReset || StanceController.PistolIsCompressed) && !cancelBecauseSooting;
                    bool allowActiveAimReload = !PlayerProperties.playerIsScav && Plugin.ActiveAimReload.Value && PlayerProperties.IsInReloadOpertation && !PlayerProperties.IsAttemptingToReloadInternalMag && !PlayerProperties.IsQuickReloading;
                    bool cancelStance = (StanceController.CancelActiveAim && StanceController.IsActiveAiming && !allowActiveAimReload) || (StanceController.CancelHighReady && StanceController.IsHighReady) || (StanceController.CancelLowReady && StanceController.IsLowReady) || (StanceController.CancelShortStock && StanceController.IsShortStock) || (StanceController.CancelPistolStance && StanceController.PistolIsCompressed);

                    currentRotation = Quaternion.Slerp(currentRotation, __instance.IsAiming && allStancesReset ? quaternion_2 : doStanceRotation ? stanceRotation : Quaternion.identity, doStanceRotation ? stanceSpeed : __instance.IsAiming ? 8f * float_9 * dt : 8f * dt);

                    Quaternion rhs = Quaternion.Euler(float_14 * Single_3 * vector3_6);
                    __instance.HandsContainer.WeaponRootAnim.SetPositionAndRotation(vector3_4, quaternion_6 * rhs * currentRotation);

                    if (!Plugin.IsFiring)
                    {
                        __instance.HandsContainer.HandsPosition.Damping = 0.5f;
                    }

                    if (isPistol && !WeaponProperties.HasShoulderContact && Plugin.EnableAltPistol.Value && !PlayerProperties.playerIsScav)
                    {
                        if (StanceController.PistolIsCompressed && !Plugin.IsAiming && !isResettingPistol && !Plugin.IsBlindFiring)
                        {
                            Plugin.StanceBlender.Target = 1f;
                        }
                        else
                        {
                            Plugin.StanceBlender.Target = 0f;
                        }

                        if ((!StanceController.PistolIsCompressed && !Plugin.IsAiming && !isResettingPistol) || (Plugin.IsBlindFiring))
                        {
                            StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, Vector3.zero, 5f * dt);
                        }

                        hasResetActiveAim = true;
                        hasResetHighReady = true;
                        hasResetLowReady = true;
                        hasResetShortStock = true;
                        StanceController.DoPistolStances(false, ref __instance, ref stanceRotation, dt, ref hasResetPistolPos, player, Logger, ref stanceSpeed, ref isResettingPistol);
                    }
                    else
                    {
                        if ((!isInStance && allStancesReset) || (cancelBecauseSooting && !isInShootableStance) || Plugin.IsAiming || cancelStance || Plugin.IsBlindFiring)
                        {
                            Plugin.StanceBlender.Target = 0f;
                        }
                        else if (isInStance)
                        {
                            Plugin.StanceBlender.Target = 1f;
                        }

                        if (((!isInStance && allStancesReset) && !cancelBecauseSooting && !Plugin.IsAiming) || (Plugin.IsBlindFiring))
                        {
                            StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, Vector3.zero, 5f * dt);
                        }

                        hasResetPistolPos = true;
                        StanceController.DoRifleStances(Logger, player, firearmController, false, ref __instance, ref stanceRotation, dt, ref isResettingShortStock, ref hasResetShortStock, ref hasResetLowReady, ref hasResetActiveAim, ref hasResetHighReady, ref isResettingHighReady, ref isResettingLowReady, ref isResettingActiveAim, ref stanceSpeed);
                    }

                    StanceController.HasResetActiveAim = hasResetActiveAim;
                    StanceController.HasResetHighReady = hasResetHighReady;
                    StanceController.HasResetLowReady = hasResetLowReady;
                    StanceController.HasResetShortStock = hasResetShortStock;
                    StanceController.HasResetPistolPos = hasResetPistolPos;
                }
            }
        }
    }

    public class UpdateHipInaccuracyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmController).GetMethod("UpdateHipInaccuracy", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref FirearmController __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(FirearmController), "_player").GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                WeaponProperties.BaseHipfireInaccuracy = player.ProceduralWeaponAnimation.Breath.HipPenalty;
            }
        }
    }
}
