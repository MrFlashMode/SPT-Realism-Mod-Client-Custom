using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;
using static ActiveHealthControllerClass;

namespace RealismMod
{
    public static class MedProperties
    {
        public static string MedType(Item med)
        {
            return !Utils.NullCheck(med.ConflictingItems) ? med.ConflictingItems[1] : "Unknown";

        }

        public static readonly Dictionary<string, Type> EffectTypes = new Dictionary<string, Type>
        {
            { "Painkiller", typeof(GInterface207) },
            { "Tremor", typeof(GInterface210) },
            { "BrokenBone", typeof(GInterface192) },
            { "TunnelVision", typeof(GInterface212) },
            { "Contusion", typeof(GInterface202)  },
            { "HeavyBleeding", typeof(GInterface190) },
            { "LightBleeding", typeof(GInterface189) },
            { "Dehydration", typeof(GInterface193) },
            { "Exhaustion", typeof(GInterface194) },
            { "Pain", typeof(GInterface206) }
        };
    }

    public static class DamageTracker
    {
        public static Dictionary<EDamageType, Dictionary<EBodyPart, float>> DamageRecord = new Dictionary<EDamageType, Dictionary<EBodyPart, float>>();

        public static float TotalDehydrationDamage = 0f;
        public static float TotalExhaustionDamage = 0f;

        public static void ResetTracker()
        {
            DamageRecord.Clear();
            TotalDehydrationDamage = 0f;
            TotalExhaustionDamage = 0f;
        }
    }

    public static class RealismHealthController
    {
        public static EBodyPart[] BodyParts = { EBodyPart.Head, EBodyPart.Chest, EBodyPart.Stomach, EBodyPart.RightLeg, EBodyPart.LeftLeg, EBodyPart.RightArm, EBodyPart.LeftArm };

        private static readonly List<IHealthEffect> activeHealthEffects = new List<IHealthEffect>();

        private static float adrenalineCooldownTime = 60f * (1f - PlayerProperties.StressResistanceFactor);
        public static bool AdrenalineCooldownActive = false;

        public static void HealthController(ManualLogSource logger)
        {
            if (!Utils.IsInHideout())
            {
                if (Plugin.healthControllerTick >= 1f)
                {
                    ControllerTick(logger, Singleton<GameWorld>.Instance.AllPlayers[0]);
                    Plugin.healthControllerTick = 0f;
                }
            }
            if (Utils.IsInHideout() || !Utils.IsReady)
            {
                RemoveAllEffects();
                DamageTracker.ResetTracker();
            }

            if (AdrenalineCooldownActive && adrenalineCooldownTime > 0.0f)
            {
                adrenalineCooldownTime -= Time.deltaTime;
            }
            if (AdrenalineCooldownActive && adrenalineCooldownTime <= 0.0f)
            {
                adrenalineCooldownTime = 60f * (1f - PlayerProperties.StressResistanceFactor);
                AdrenalineCooldownActive = false;
            }
        }

        public static void AddBasesEFTEffect(Player player, string effect, EBodyPart bodyPart, float delayTime, float duration, float residueTime, float strength)
        {
            MethodInfo effectMethod = GetAddBaseEFTEffectMethodInfo();
            effectMethod.MakeGenericMethod(typeof(ActiveHealthControllerClass).GetNestedType(effect, BindingFlags.NonPublic | BindingFlags.Instance)).Invoke(player.ActiveHealthController, new object[] { bodyPart, delayTime, duration, residueTime, strength, null });
        }

        public static void AddBaseEFTEffectIfNoneExisting(Player player, string effect, EBodyPart bodyPart, float delayTime, float duration, float residueTime, float strength)
        {
            if (!player.ActiveHealthController.BodyPartEffects.Effects[0].Any(v => v.Key == effect))
            {
                AddBasesEFTEffect(player, effect, bodyPart, delayTime, duration, residueTime, strength);
            }
        }

        public static void AddToExistingBaseEFTEffect(Player player, string targetEffect, EBodyPart bodyPart, float delayTime, float duration, float residueTime, float strength)
        {
            if (!player.ActiveHealthController.BodyPartEffects.Effects[0].Any(v => v.Key == targetEffect))
            {
                AddBasesEFTEffect(player, targetEffect, bodyPart, delayTime, duration, residueTime, strength);
            }
            else
            {
                IReadOnlyList<GClass2102> effectsList = (IReadOnlyList<GClass2102>)AccessTools.Property(typeof(ActiveHealthControllerClass), "IReadOnlyList_0").GetValue(player.ActiveHealthController);
                Type targetType = null;
                MedProperties.EffectTypes.TryGetValue(targetEffect, out targetType);
                for (int i = effectsList.Count - 1; i >= 0; i--)
                {
                    GClass2102 existingEffect = effectsList[i];
                    Type effectType = existingEffect.Type;
                    EBodyPart effectPart = existingEffect.BodyPart;

                    if (effectType == targetType)
                    {
                        existingEffect.AddWorkTime(duration, false);
                    }
                }
            }
        }

        public static void AddAdrenaline(Player player, float painkillerDuration, float negativeEffectDuration, float negativeEffectStrength)
        {
            if (Plugin.EnableAdrenaline && !AdrenalineCooldownActive)
            //if (Plugin.EnableAdrenaline)
            {
                AdrenalineCooldownActive = true;
                //AddToExistingBaseEFTEffect(player, "PainKiller", EBodyPart.Head, 0f, painkillerDuration, 3f, 1f);
                AddToExistingBaseEFTEffect(player, "TunnelVision", EBodyPart.Head, 0f, negativeEffectDuration, 3f, negativeEffectStrength);
                AddToExistingBaseEFTEffect(player, "Tremor", EBodyPart.Head, painkillerDuration, negativeEffectDuration, 3f, negativeEffectStrength);
            }
        }

        public static MethodInfo GetAddBaseEFTEffectMethodInfo()
        {
            MethodInfo effectMethodInfo = typeof(ActiveHealthControllerClass).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).First(m =>
            m.GetParameters().Length == 6
            && m.GetParameters()[0].Name == "bodyPart"
            && m.GetParameters()[5].Name == "initCallback"
            && m.IsGenericMethod);

            return effectMethodInfo;
        }

        public static void RemoveBaseEFTEffect(Player player, EBodyPart targetBodyPart, string targetEffect)
        {
            IReadOnlyList<GClass2102> effectsList = (IReadOnlyList<GClass2102>)AccessTools.Property(typeof(ActiveHealthControllerClass), "IReadOnlyList_0").GetValue(player.ActiveHealthController);

            Type targetType = null;
            if (MedProperties.EffectTypes.TryGetValue(targetEffect, out targetType))
            {
                for (int i = effectsList.Count - 1; i >= 0; i--)
                {
                    GClass2102 effect = effectsList[i];
                    Type effectType = effect.Type;
                    EBodyPart effectPart = effect.BodyPart;

                    if (effectType == targetType && effectPart == targetBodyPart)
                    {
                        effect.ForceResidue();
                    }
                }
            }
        }

        public static void AddCustomEffect(IHealthEffect effect, bool canStack)
        {
            if (!canStack)
            {
                foreach (IHealthEffect eff in activeHealthEffects)
                {
                    if (eff.GetType() == effect.GetType() && eff.BodyPart == effect.BodyPart)
                    {
                        RemoveEffectOfType(effect.GetType(), effect.BodyPart);
                        break;
                    }
                }
            }

            activeHealthEffects.Add(effect);
        }

        public static void RemoveEffectOfType(Type effect, EBodyPart bodyPart)
        {
            for (int i = activeHealthEffects.Count - 1; i >= 0; i--)
            {
                if (activeHealthEffects[i].GetType() == effect && activeHealthEffects[i].BodyPart == bodyPart)
                {
                    activeHealthEffects.RemoveAt(i);
                }
            }
        }

        public static void CancelEffects()
        {
            for (int i = activeHealthEffects.Count - 1; i >= 0; i--)
            {
                if (activeHealthEffects[i].Delay > 0f)
                {
                    activeHealthEffects.RemoveAt(i);
                }
            }
        }

        public static void RemoveRegenEffectsOfDamageType(EDamageType damageType)
        {
            List<HealthRegenEffect> regenEffects = activeHealthEffects.OfType<HealthRegenEffect>().ToList();
            regenEffects.RemoveAll(x => x.DamageType == damageType);
            activeHealthEffects.RemoveAll(x => !regenEffects.Contains(x));
        }

        public static void RemoveEffectsOfType(EHealthEffectType effectType)
        {
            activeHealthEffects.RemoveAll(x => x.EffectType == effectType);
        }

        public static void RemoveAllEffects()
        {
            activeHealthEffects.Clear();
        }

        public static bool HasBaseEFTEffect(Player player, string targetEffect)
        {
            IReadOnlyList<GClass2102> effectsList = (IReadOnlyList<GClass2102>)AccessTools.Property(typeof(ActiveHealthControllerClass), "IReadOnlyList_0").GetValue(player.ActiveHealthController);

            Type targetType = null;
            if (MedProperties.EffectTypes.TryGetValue(targetEffect, out targetType))
            {
                for (int i = effectsList.Count - 1; i >= 0; i--)
                {
                    GClass2102 effect = effectsList[i];
                    Type effectType = effect.Type;

                    if (effectType == targetType)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static void ResourceRegenCheck(Player player, ManualLogSource logger)
        {
            float vitalitySkill = player.Skills.VitalityBuffSurviobilityInc.Value;
            float delay = (float)Math.Round(15f * (1f - vitalitySkill), 2);
            float tickRate = (float)Math.Round(0.22f * (1f + vitalitySkill), 2);

            bool isDehydrated = HasBaseEFTEffect(player, "Dehydration");
            bool isExhausted = HasBaseEFTEffect(player, "Exhaustion");

            if (isDehydrated)
            {
                RemoveRegenEffectsOfDamageType(EDamageType.Dehydration);
            }
            if (!isDehydrated && DamageTracker.TotalDehydrationDamage > 0f)
            {
                RestoreHPArossBody(player, DamageTracker.TotalDehydrationDamage, delay, EDamageType.Dehydration, tickRate);
                DamageTracker.TotalDehydrationDamage = 0;
            }
            if (isExhausted)
            {
                RemoveRegenEffectsOfDamageType(EDamageType.Exhaustion);
            }
            if (!isExhausted && DamageTracker.TotalExhaustionDamage > 0f)
            {
                RestoreHPArossBody(player, DamageTracker.TotalExhaustionDamage, delay, EDamageType.Exhaustion, tickRate);
                DamageTracker.TotalExhaustionDamage = 0;
            }
        }

        public static void ControllerTick(ManualLogSource logger, Player player)
        {
            if ((int)(Time.time % 4) == 0)
            {
                ResourceRegenCheck(player, logger);
            }
            if ((int)(Time.time % 6) == 0)
            {
                PlayerInjuryStateCheck(player, logger);
            }

            for (int i = activeHealthEffects.Count - 1; i >= 0; i--)
            {
                IHealthEffect effect = activeHealthEffects[i];
                if (Plugin.EnableLogging.Value)
                {
                    logger.LogWarning("Type = " + effect.GetType().ToString());
                    logger.LogWarning("Delay = " + effect.Delay);
                }

                effect.Delay = effect.Delay > 0 ? effect.Delay - 1f : effect.Delay;

                if ((int)(Time.time % 3) == 0)
                {
                    if (effect.Duration == null || effect.Duration > 0f)
                    {
                        effect.Tick();
                    }
                    else
                    {
                        if (Plugin.EnableLogging.Value)
                        {
                            logger.LogWarning("Removing Effect Due to Duration");
                        }
                        activeHealthEffects.RemoveAt(i);
                    }
                }
            }
        }

        public static bool MouthIsBlocked(Item head, Item face, EquipmentClass equipment)
        {
            bool faceBlocksMouth = false;
            bool headBlocksMouth = false;

            LootItemClass headwear = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as LootItemClass;
            IEnumerable<Item> nestedItems = headwear != null ? headwear.GetAllItemsFromCollection().OfType<Item>() : null;

            if (nestedItems != null)
            {
                foreach (Item item in nestedItems)
                {
                    FaceShieldComponent fs = item.GetItemComponent<FaceShieldComponent>();
                    if (GearProperties.BlocksMouth(item) && fs == null)
                    {
                        return true;
                    }
                }
            }

            if (head != null)
            {
                faceBlocksMouth = GearProperties.BlocksMouth(head);
            }
            if (face != null)
            {
                headBlocksMouth = GearProperties.BlocksMouth(face);
            }

            return faceBlocksMouth || headBlocksMouth;
        }

        public static void GetBodyPartType(EBodyPart part, ref bool isNotLimb, ref bool isHead, ref bool isBody)
        {
            isHead = part == EBodyPart.Head;
            isBody = part == EBodyPart.Chest || part == EBodyPart.Stomach;
            isNotLimb = part == EBodyPart.Chest || part == EBodyPart.Stomach || part == EBodyPart.Head;
        }

        public static void CanConsume(ManualLogSource Logger, Player player, Item item, ref bool canUse)
        {
            EquipmentClass equipment = (EquipmentClass)AccessTools.Property(typeof(Player), "Equipment").GetValue(player);
            Item face = equipment.GetSlot(EquipmentSlot.FaceCover).ContainedItem;
            Item head = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem;

            FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
            NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
            bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On) && GearProperties.BlocksMouth(fsComponent.Item);
            bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);

            bool mouthBlocked = MouthIsBlocked(head, face, equipment);

            if (fsIsON || nvgIsOn || mouthBlocked)
            {
                NotificationManagerClass.DisplayWarningNotification("Не могу употребить провизию, мешает Забрало/ПНВ/Маска.", EFT.Communications.ENotificationDurationType.Long);
                canUse = false;
                return;
            }
        }

        public static void RestoreHPArossBody(Player player, float hpToRestore, float delay, EDamageType damageType, float tickRate)
        {
            hpToRestore = Mathf.RoundToInt((hpToRestore) / BodyParts.Length);

            foreach (EBodyPart part in BodyParts)
            {
                HealthRegenEffect regenEffect = new HealthRegenEffect(tickRate, null, part, player, delay, hpToRestore, damageType);
                AddCustomEffect(regenEffect, false);
            }
        }

        public static void CanUseMedItem(ManualLogSource Logger, Player player, EBodyPart bodyPart, Item item, ref bool canUse)
        {
            if (item.Template.Parent._id == "5448f3a64bdc2d60728b456a" || MedProperties.MedType(item) == "drug")
            {
                return;
            }

            EquipmentClass equipment = (EquipmentClass)AccessTools.Property(typeof(Player), "Equipment").GetValue(player);

            Item head = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem;
            Item face = equipment.GetSlot(EquipmentSlot.FaceCover).ContainedItem;

            bool mouthBlocked = MouthIsBlocked(head, face, equipment);

            string medType = MedProperties.MedType(item);

            FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
            NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
            bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);
            bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);

            if (Plugin.GearBlocksEat && medType == "pills" && (mouthBlocked || fsIsON || nvgIsOn))
            {
                NotificationManagerClass.DisplayWarningNotification("Не могу принять таблетки, мешает Забрало/ПНВ/Маска.", EFT.Communications.ENotificationDurationType.Long);
                canUse = false;
                return;
            }

            //if (medType == "vas")
            //{
            //    return;
            //}

            return;
        }

        public static void PlayerInjuryStateCheck(Player player, ManualLogSource logger)
        {
            float aimMoveSpeedBase = 0.42f;
            float ergoDeltaInjuryMulti = 1f;
            float adsInjuryMulti = 1f;
            float stanceInjuryMulti = 1f;
            float reloadInjuryMulti = 1f;
            float recoilInjuryMulti = 1f;
            float sprintSpeedInjuryMulti = 1f;
            float sprintAccelInjuryMulti = 1f;
            float walkSpeedInjuryMulti = 1f;
            float stamRegenInjuryMulti = 1f;
            float resourceRateInjuryMulti = 1f;

            float currentEnergy = player.ActiveHealthController.Energy.Current;
            float maxEnergy = player.ActiveHealthController.Energy.Maximum;
            float percentEnergy = currentEnergy / maxEnergy;

            float currentHydro = player.ActiveHealthController.Hydration.Current;
            float maxHydro = player.ActiveHealthController.Hydration.Maximum;
            float percentHydro = currentHydro / maxHydro;

            float totalMaxHp = 0f;
            float totalCurrentHp = 0f;

            Type fractureType;
            MedProperties.EffectTypes.TryGetValue("BrokenBone", out fractureType);

            foreach (EBodyPart part in BodyParts)
            {
                IEnumerable<IEffect> effects = player.ActiveHealthController.GetAllActiveEffects(part);
                bool hasFracture = fractureType != null && effects.Any(e => e.Type == fractureType);

                bool isLeftArm = part == EBodyPart.LeftArm;
                bool isRightArm = part == EBodyPart.LeftArm;
                bool isArm = isLeftArm || isRightArm;
                bool isLeg = part == EBodyPart.LeftLeg || part == EBodyPart.RightLeg;
                bool isBody = part == EBodyPart.Chest || part == EBodyPart.Stomach;

                float currentHp = player.ActiveHealthController.GetBodyPartHealth(part).Current;
                float maxHp = player.ActiveHealthController.GetBodyPartHealth(part).Maximum;
                totalMaxHp += maxHp;
                totalCurrentHp += currentHp;

                float percentHp = (currentHp / maxHp);
                float percentHpStamRegen = 1f - ((1f - percentHp) / (isBody ? 10f : 5f));
                float percentHpWalk = 1f - ((1f - percentHp) / (isBody ? 15f : 7.5f));
                float percentHpSprint = 1f - ((1f - percentHp) / (isBody ? 8f : 4f));
                float percentHpAimMove = 1f - ((1f - percentHp) / (isArm ? 20f : 14f));
                float percentHpADS = 1f - ((1f - percentHp) / (isRightArm ? 1f : 2f));
                float percentHpStance = 1f - ((1f - percentHp) / (isRightArm ? 1.5f : 3f));
                float percentHpReload = 1f - ((1f - percentHp) / (isLeftArm ? 2f : 3.5f));
                float percentHpRecoil = 1f - ((1f - percentHp) / (isLeftArm ? 10f : 20f));

                if (percentHp <= 0.5f)
                {
                    AddBaseEFTEffectIfNoneExisting(player, "Pain", part, 0f, 10f, 1f, 1f);
                }

                if (isLeg || isBody)
                {
                    aimMoveSpeedBase *= percentHpAimMove;
                    sprintSpeedInjuryMulti *= percentHpSprint;
                    sprintAccelInjuryMulti *= percentHp;
                    walkSpeedInjuryMulti *= percentHpWalk;
                    stamRegenInjuryMulti *= percentHpStamRegen;
                }

                if (isArm)
                {
                    if (isLeftArm)
                    {
                        PlayerProperties.LeftArmRuined = player.ActiveHealthController.GetBodyPartHealth(EBodyPart.LeftArm).Current <= 0 || hasFracture;
                    }
                    if (isRightArm)
                    {
                        PlayerProperties.RightArmRuined = player.ActiveHealthController.GetBodyPartHealth(EBodyPart.RightArm).Current <= 0 || hasFracture;
                    }

                    float armFractureFactor = isLeftArm && hasFracture ? 0.8f : isRightArm && hasFracture ? 0.9f : 1f;

                    aimMoveSpeedBase *= percentHpAimMove * armFractureFactor;
                    ergoDeltaInjuryMulti *= (1f + (1f - percentHp)) * armFractureFactor;
                    adsInjuryMulti *= percentHpADS * armFractureFactor;
                    stanceInjuryMulti *= percentHpStance * armFractureFactor;
                    reloadInjuryMulti *= percentHpReload * armFractureFactor;
                    recoilInjuryMulti *= (1f + (1f - percentHpRecoil)) * armFractureFactor;
                }
            }

            float totalHpPercent = totalCurrentHp / totalMaxHp;
            resourceRateInjuryMulti = (1f - totalHpPercent);

            if (totalHpPercent <= 0.5f)
            {
                AddBaseEFTEffectIfNoneExisting(player, "Pain", EBodyPart.Chest, 0f, 10f, 1f, 1f);
            }

            float percentEnergyFactor = percentEnergy * 1.2f;
            float percentHydroFactor = percentHydro * 1.2f;

            float percentEnergySprint = 1f - ((1f - percentEnergyFactor) / 8f);
            float percentEnergyWalk = 1f - ((1f - percentEnergyFactor) / 12f);
            float percentEnergyAimMove = 1f - ((1f - percentEnergyFactor) / 20f);
            float percentEnergyADS = 1f - ((1f - percentEnergyFactor) / 5f);
            float percentEnergyStance = 1f - ((1f - percentEnergyFactor) / 2f);
            float percentEnergyReload = 1f - ((1f - percentEnergyFactor) / 10f);
            float percentEnergyRecoil = 1f - ((1f - percentEnergyFactor) / 40f);
            float percentEnergyErgo = 1f - ((1f - percentEnergyFactor) / 2f);
            float percentEnergyStamRegen = 1f - ((1f - percentEnergyFactor) / 10f);

            float percentHydroLowerLimit = 1f - ((1f - percentHydro) / 4f);
            float percentHydroLimitRecoil = 1f + ((1f - percentHydro) / 20f);
            float percentHydroUpperLimit = 1f + (1f - percentHydroLowerLimit);

            PlayerProperties.AimMoveSpeedBase = Mathf.Max(aimMoveSpeedBase, 0.3f * percentHydroLowerLimit);
            PlayerProperties.ErgoDeltaInjuryMulti = Mathf.Min(ergoDeltaInjuryMulti * (1f + (1f - percentEnergyErgo)), 3.5f);
            PlayerProperties.ADSInjuryMulti = Mathf.Max(adsInjuryMulti * percentEnergyADS, 0.35f * percentHydroLowerLimit);
            PlayerProperties.StanceInjuryMulti = Mathf.Max(stanceInjuryMulti * percentEnergyStance, 0.45f * percentHydroLowerLimit);
            PlayerProperties.ReloadInjuryMulti = Mathf.Max(reloadInjuryMulti * percentEnergyReload, 0.65f * percentHydroLowerLimit);
            PlayerProperties.RecoilInjuryMulti = Mathf.Min(recoilInjuryMulti * (1f + (1f - percentEnergyRecoil)), 1.15f * percentHydroLimitRecoil);
            PlayerProperties.HealthSprintSpeedFactor = Mathf.Max(sprintSpeedInjuryMulti * percentEnergySprint, 0.4f * percentHydroLowerLimit);
            PlayerProperties.HealthSprintAccelFactor = Mathf.Max(sprintAccelInjuryMulti * percentEnergySprint, 0.4f * percentHydroLowerLimit);
            PlayerProperties.HealthWalkSpeedFactor = Mathf.Max(walkSpeedInjuryMulti * percentEnergyWalk, 0.6f * percentHydroLowerLimit);
            PlayerProperties.HealthStamRegenFactor = Mathf.Max(stamRegenInjuryMulti * percentEnergyStamRegen, 0.5f * percentHydroLowerLimit);

            ResourceRateEffect resEffect = new ResourceRateEffect(resourceRateInjuryMulti, 3f, player, 0f);
            AddCustomEffect(resEffect, true);
        }
    }
}
