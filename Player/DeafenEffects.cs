﻿using System;
using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace RealismMod
{
    public class PrismEffectsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(PrismEffects).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostFix(ref PrismEffects __instance)
        {
            if (__instance.gameObject.name == "FPS Camera")
            {
                Plugin.PrismEffects = __instance;
            }
        }
    }

    public class VignettePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EffectsController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(EffectsController __instance)
        {
            CC_FastVignette vig = (CC_FastVignette)AccessTools.Field(typeof(EffectsController), "cc_FastVignette_0").GetValue(__instance);
            Plugin.Vignette = vig;
        }
    }

    public class UpdatePhonesPatch : ModulePatch
    {
        private static float HelmDeafFactor(EquipmentClass equipment)
        {
            string deafStrength = "None";
            LootItemClass headwearItem = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as LootItemClass;

            if (headwearItem != null)
            {
                ArmorClass helmet = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as ArmorClass;

                if (helmet?.Armor?.Deaf != null)
                {
                    deafStrength = helmet.Armor.Deaf.ToString();
                }

            }

            return HelmDeafFactorHelper(deafStrength);
        }

        private static float HelmDeafFactorHelper(string deafStr)
        {
            switch (deafStr)
            {
                case "Low":
                    return 0.92f;
                case "High":
                    return 0.85f;
                default:
                    return 1f;
            }
        }

        private static float HeadsetDeafFactor(EquipmentClass equipment)
        {
            float protectionFactor;

            LootItemClass headwear = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as LootItemClass;
            GClass2297 headset = (equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem as GClass2297) ?? ((headwear != null) ? headwear.GetAllItemsFromCollection().OfType<GClass2297>().FirstOrDefault() : null);

            if (headset != null)
            {
                Plugin.HasHeadSet = true;
                GClass2204 headphone = headset.Template;
                protectionFactor = ((headphone.DryVolume / 100f) + 1f) * 1.5f;
            }
            else
            {
                Plugin.HasHeadSet = false;
                protectionFactor = 1f;
            }

            return protectionFactor;
        }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("UpdatePhones", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {
            if (__instance.IsYourPlayer)
            {
                EquipmentClass equipment = (EquipmentClass)AccessTools.Property(typeof(Player), "Equipment").GetValue(__instance);
                Deafening.EarProtectionFactor = HeadsetDeafFactor(equipment) * HelmDeafFactor(equipment);
            }
        }
    }

    public static class Deafening
    {
        public static float EarProtectionFactor;
        public static float AmmoDeafFactor;
        public static float WeaponDeafFactor;
        public static float BotDeafFactor;
        public static float GrenadeDeafFactor;

        //player
        public static float Volume = 0f;
        public static float Distortion = 0f;
        public static float VignetteDarkness = 0f;

        public static float VolumeLimit = -30f;
        public static float DistortionLimit = 70f;
        public static float VignetteDarknessLimit = 0.3f;

        //bot
        public static float BotVolume = 0f;
        public static float BotDistortion = 0f;
        public static float BotVignetteDarkness = 0f;

        //grenade
        public static float GrenadeVolume = 0f;
        public static float GrenadeDistortion = 0f;
        public static float GrenadeVignetteDarkness = 0f;

        public static float GrenadeVolumeLimit = -40f;
        public static float GrenadeDistortionLimit = 50f;
        public static float GrenadeVignetteDarknessLimit = 0.2f;

        public static float GrenadeVolumeDecreaseRate = 0.02f;
        public static float GrenadeDistortionIncreaseRate = 0.5f;
        public static float GrenadeVignetteDarknessIncreaseRate = 0.6f;

        public static float GrenadeVolumeResetRate = 0.033f;
        public static float GrenadeDistortionResetRate = 0.1f;
        public static float GrenadeVignetteDarknessResetRate = 0.5f;

        public static bool valuesAreReset = false;

        public static void DoDeafening()
        {
            float enviroMulti = PlayerProperties.enviroType == EnvironmentType.Indoor ? 1.3f : 0.95f;
            float deafFactor = AmmoDeafFactor * WeaponDeafFactor * EarProtectionFactor;
            float botDeafFactor = BotDeafFactor * EarProtectionFactor;
            float grenadeDeafFactor = GrenadeDeafFactor * EarProtectionFactor;

            if (Plugin.IsFiring == true)
            {
                ChangeDeafValues(deafFactor, ref VignetteDarkness, Plugin.VigRate, VignetteDarknessLimit, ref Volume, Plugin.DeafRate, VolumeLimit, ref Distortion, Plugin.DistRate, DistortionLimit, enviroMulti);
            }
            else if (!valuesAreReset)
            {
                ReseDeaftValues(deafFactor, ref VignetteDarkness, Plugin.VigReset, VignetteDarknessLimit, ref Volume, Plugin.DeafReset, VolumeLimit, ref Distortion, Plugin.DistReset, DistortionLimit, enviroMulti);
            }

            if (Plugin.IsBotFiring == true)
            {
                ChangeDeafValues(botDeafFactor, ref BotVignetteDarkness, Plugin.VigRate, VignetteDarknessLimit, ref BotVolume, Plugin.DeafRate, VolumeLimit, ref BotDistortion, Plugin.DistRate, DistortionLimit, enviroMulti);
            }
            else if (!valuesAreReset)
            {
                ReseDeaftValues(botDeafFactor, ref BotVignetteDarkness, Plugin.VigReset, VignetteDarknessLimit, ref BotVolume, Plugin.DeafReset, VolumeLimit, ref BotDistortion, Plugin.DistReset, DistortionLimit, enviroMulti);
            }

            if (Plugin.GrenadeExploded == true)
            {
                ChangeDeafValues(grenadeDeafFactor, ref GrenadeVignetteDarkness, GrenadeVignetteDarknessIncreaseRate, GrenadeVignetteDarknessLimit, ref GrenadeVolume, GrenadeVolumeDecreaseRate, GrenadeVolumeLimit, ref GrenadeDistortion, GrenadeDistortionIncreaseRate, GrenadeDistortionLimit, enviroMulti);
            }
            else if (!valuesAreReset)
            {
                ReseDeaftValues(grenadeDeafFactor, ref GrenadeVignetteDarkness, GrenadeVignetteDarknessResetRate, GrenadeVignetteDarknessLimit, ref GrenadeVolume, GrenadeVolumeResetRate, GrenadeVolumeLimit, ref GrenadeDistortion, GrenadeDistortionResetRate, GrenadeDistortionLimit, enviroMulti);
            }

            float totalVolume = Mathf.Clamp(Volume + BotVolume + GrenadeVolume, -40.0f, 0.0f);
            float totalDistortion = Mathf.Clamp(Distortion + BotDistortion + GrenadeDistortion, 0.0f, 70.0f);
            float totalVignette = Mathf.Clamp(VignetteDarkness + BotVignetteDarkness + GrenadeVignetteDarkness, 0.0f, 60.0f);
            float headsetAmbientVol = Plugin.AmbientVolume * (1f + ((20f - Plugin.RealTimeGain) / Plugin.HeadsetAmbientMulti));

            //for some reason this prevents the values from being fully reset to 0
            if (totalVolume != 0.0f || totalDistortion != 0.0f || totalVignette != 0.0f)
            {
                Plugin.PrismEffects.vignetteStrength = totalVignette;
                Singleton<BetterAudio>.Instance.Master.SetFloat("GunsVolume", totalVolume + Plugin.GunsVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("OcclusionVolume", totalVolume + Plugin.MainVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("EnvironmentVolume", totalVolume + Plugin.MainVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientVolume", totalVolume + Plugin.AmbientVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientOccluded", totalVolume + Plugin.AmbientOccluded);

                if (!Plugin.HasHeadSet)
                {
                    Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorResonance", totalDistortion + Plugin.CompressorResonance);
                    Singleton<BetterAudio>.Instance.Master.SetFloat("Compressor", totalDistortion + Plugin.Compressor);
                    Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorDistortion", totalDistortion + Plugin.CompressorDistortion);
                    Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorLowpass", totalDistortion + Plugin.CompressorDistortion);
                }
                else
                {
                    Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorMakeup", Plugin.RealTimeGain * Plugin.GainCutoff);
                    Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientVolume", headsetAmbientVol * (1f + (1f - Plugin.GainCutoff)));
                }

                valuesAreReset = false;
            }
            else
            {
                if (Plugin.HasHeadSet)
                {
                    Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorMakeup", Plugin.RealTimeGain);
                    Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientVolume", headsetAmbientVol);

                }

                Plugin.PrismEffects.useVignette = false;
                valuesAreReset = true;
            }
        }

        private static void ChangeDeafValues(float deafFactor, ref float vigValue, float vigIncRate, float vigLimit, ref float volValue, float volDecRate, float volLimit, ref float distValue, float distIncRate, float distLimit, float enviroMulti)
        {
            Plugin.PrismEffects.useVignette = true;

            float totalVigLimit = Mathf.Min(vigLimit * deafFactor * enviroMulti, 1.5f);
            vigValue = Mathf.Clamp(vigValue + (vigIncRate * deafFactor * enviroMulti), 0.0f, totalVigLimit);
            volValue = Mathf.Clamp(volValue - (volDecRate * deafFactor * enviroMulti), volLimit, 0.0f);
            distValue = Mathf.Clamp(distValue + (distIncRate * deafFactor), 0.0f, distLimit);
        }

        private static void ReseDeaftValues(float deafFactor, ref float vigValue, float vigResetRate, float vigLimit, ref float volValue, float volResetRate, float volLimit, ref float distValue, float distResetRate, float distLimit, float enviroMulti)
        {
            float totalVigLimit = Mathf.Min(vigLimit * deafFactor * enviroMulti, 1.5f);
            vigValue = Mathf.Clamp(vigValue - vigResetRate, 0.0f, totalVigLimit);
            volValue = Mathf.Clamp(volValue + volResetRate, volLimit, 0.0f);
            distValue = Mathf.Clamp(distValue - distResetRate, 0.0f, distLimit);
        }
    }

    public class SetCompressorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BetterAudio).GetMethod("SetCompressor", BindingFlags.Instance | BindingFlags.Public);

        }

        [PatchPrefix]
        private static bool Prefix(GClass2204 template, BetterAudio __instance)
        {
            GClass2557.CreateEvent<GClass2547>().Invoke(template);

            bool hasHeadsetTemplate = template != null;
            bool isNotHeadset = template?._id == null; //using both bools is redundant now.

            Plugin.MainVolume = hasHeadsetTemplate && !isNotHeadset ? template.DryVolume : 0f;
            Plugin.Compressor = hasHeadsetTemplate && !isNotHeadset ? template.CompressorVolume : -80f;
            Plugin.AmbientVolume = hasHeadsetTemplate && !isNotHeadset ? template.AmbientVolume : 0f;
            Plugin.AmbientOccluded = hasHeadsetTemplate && !isNotHeadset ? (template.AmbientVolume - 15f) : -5f;
            Plugin.GunsVolume = hasHeadsetTemplate && !isNotHeadset ? (template.DryVolume) : -5f;

            Plugin.CompressorDistortion = hasHeadsetTemplate && !isNotHeadset ? template.Distortion : 0.277f;
            Plugin.CompressorResonance = hasHeadsetTemplate && !isNotHeadset ? template.Resonance : 2.47f;
            Plugin.CompressorLowpass = hasHeadsetTemplate && !isNotHeadset ? template.LowpassFreq : 22000f;
            Plugin.CompressorGain = hasHeadsetTemplate && !isNotHeadset ? Plugin.RealTimeGain : 10f;

            __instance.Master.SetFloat("Compressor", Plugin.Compressor);
            __instance.Master.SetFloat("OcclusionVolume", Plugin.MainVolume);
            __instance.Master.SetFloat("EnvironmentVolume", Plugin.MainVolume);
            __instance.Master.SetFloat("AmbientVolume", Plugin.AmbientVolume);
            __instance.Master.SetFloat("AmbientOccluded", Plugin.AmbientOccluded);
            __instance.Master.SetFloat("GunsVolume", Plugin.GunsVolume);

            __instance.Master.SetFloat("CompressorAttack", hasHeadsetTemplate && !isNotHeadset ? template.CompressorAttack : 35f);
            __instance.Master.SetFloat("CompressorMakeup", Plugin.CompressorGain);
            __instance.Master.SetFloat("CompressorRelease", hasHeadsetTemplate && !isNotHeadset ? template.CompressorRelease : 215f);
            __instance.Master.SetFloat("CompressorTreshold", hasHeadsetTemplate && !isNotHeadset ? template.CompressorTreshold : -20f);
            __instance.Master.SetFloat("CompressorDistortion", Plugin.CompressorDistortion);
            __instance.Master.SetFloat("CompressorResonance", Plugin.CompressorResonance);
            __instance.Master.SetFloat("CompressorCutoff", hasHeadsetTemplate && !isNotHeadset ? template.CutoffFreq : 245f);
            __instance.Master.SetFloat("CompressorLowpass", Plugin.CompressorLowpass);

            __instance.Master.SetFloat("OcclusionVolume", hasHeadsetTemplate && !isNotHeadset ? template.CompressorAttack : 35f);
            __instance.Master.SetFloat("CompressorHighFrequenciesGain", hasHeadsetTemplate && !isNotHeadset ? template.HighFrequenciesGain : 1f);

            //cursed BSG bull shit, best just replicate it
            float vol;
            float vol2;
            __instance.Master.GetFloat("Tinnitus1", out vol);
            __instance.Master.GetFloat("Tinnitus2", out vol2);
            __instance.Master.SetFloat("Tinnitus1", vol);
            __instance.Master.SetFloat("Tinnitus2", vol2);

            return false;
        }
    }

    public class RegisterShotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("RegisterShot", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private static float GetMuzzleLoudness(Mod[] mods)
        {
            float loudness = 0f;

            for (int i = 0; i < mods.Length; i++)
            {
                if (mods[i].Slots.Length > 0 && mods[i].Slots[0].ContainedItem != null && Utils.IsSilencer((Mod)mods[i].Slots[0].ContainedItem))
                {
                    continue;
                }
                else
                {
                    loudness += mods[i].Template.Loudness;
                }
            }

            return (loudness / 100) + 1f;
        }

        private static float CalcAmmoFactor(AmmoTemplate ammTemp)
        {
            return Math.Min((ammTemp.ammoRec / 100f) + 1f, 2f);
        }

        private static float CalcVelocityFactor(Weapon weap)
        {
            return ((weap.SpeedFactor - 1f) * -2f) + 1f;
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance, Item weapon, GClass2624 shot)
        {
            Player player = (Player)AccessTools.Field(typeof(Player.FirearmController), "_player").GetValue(__instance);

            IWeapon iWeap = weapon as IWeapon;

            if (!iWeap.IsUnderbarrelWeapon)
            {
                Weapon weap = weapon as Weapon;

                if (player.IsYourPlayer == true)
                {
                    BulletClass bullet = shot.Ammo as BulletClass;
                    AmmoTemplate currentAmmoTemplate = bullet.Template as AmmoTemplate;

                    float ammoFactor = CalcAmmoFactor(currentAmmoTemplate);
                    float velocityFactor = CalcVelocityFactor(weap);
                    float ammoDeafFactor = ammoFactor * velocityFactor;

                    if (bullet.InitialSpeed * weap.SpeedFactor <= 335f)
                    {
                        ammoDeafFactor *= 0.6f;
                    }

                    Deafening.AmmoDeafFactor = ammoDeafFactor == 0f ? 1f : ammoDeafFactor;
                }
                else
                {
                    Vector3 playerPos = Singleton<GameWorld>.Instance.AllPlayers[0].Transform.position;
                    Vector3 shootePos = player.Transform.position;
                    float distanceFromPlayer = Vector3.Distance(shootePos, playerPos);

                    if (distanceFromPlayer <= 15f)
                    {
                        Plugin.IsBotFiring = true;
                        BulletClass bullet = shot.Ammo as BulletClass;
                        AmmoTemplate currentAmmoTemplate = bullet.Template as AmmoTemplate;
                        float velocityFactor = CalcVelocityFactor(weap);
                        float ammoFactor = CalcAmmoFactor(currentAmmoTemplate);
                        float muzzleFactor = GetMuzzleLoudness(weap.Mods);
                        float calFactor = StatCalc.CalibreLoudnessFactor(weap.AmmoCaliber);
                        float ammoDeafFactor = ammoFactor * velocityFactor;

                        if (bullet.InitialSpeed * weap.SpeedFactor <= 335f)
                        {
                            ammoDeafFactor *= 0.6f;
                        }

                        float totalBotDeafFactor = muzzleFactor * calFactor * ammoDeafFactor;
                        Deafening.BotDeafFactor = totalBotDeafFactor * ((-distanceFromPlayer / 100f) + 1f) * 1.25f;
                    }
                }
            }
        }
    }

    public class ExplosionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Grenade).GetMethod("Explosion", BindingFlags.Static | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void PreFix(IExplosiveItem grenadeItem, Vector3 grenadePosition)
        {
            Player player = Singleton<GameWorld>.Instance.AllPlayers[0];
            float distanceFromPlayer = Vector3.Distance(grenadePosition, player.Transform.position);

            if (distanceFromPlayer <= 10f)
            {
                Plugin.GrenadeExploded = true;

                Deafening.GrenadeDeafFactor = grenadeItem.Contusion.z * ((-distanceFromPlayer / 100f) + 1f);
            }
        }
    }

    public class GrenadeClassContusionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GrenadeClass).GetMethod("get_Contusion");
        }

        [PatchPrefix]
        private static bool PreFix(GrenadeClass __instance, ref Vector3 __result)
        {
            ThrowableWeaponClass grenade = __instance.Template as ThrowableWeaponClass;
            Vector3 contusionVect = grenade.Contusion;
            float intensity = contusionVect.z * (1f - ((1f - Deafening.EarProtectionFactor) * 1.3f));
            float distance = contusionVect.y * 2f * Deafening.EarProtectionFactor;
            intensity = PlayerProperties.enviroType == EnvironmentType.Indoor ? intensity * 1.7f : intensity;
            distance = PlayerProperties.enviroType == EnvironmentType.Indoor ? distance * 1.7f : distance;
            __result = new Vector3(contusionVect.x, distance, intensity);

            return false;
        }
    }
}
