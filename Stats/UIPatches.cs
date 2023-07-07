﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using BepInEx.Logging;
using Comfort.Common;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;
using static RealismMod.Attributes;

namespace RealismMod
{
    public class GetCachedReadonlyQualitiesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(AmmoTemplate).GetMethod("GetCachedReadonlyQualities", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostFix(AmmoTemplate __instance, ref List<ItemAttributeClass> __result)
        {
            if (!__result.Any((ItemAttributeClass i) => (ENewItemAttributeId)i.Id == ENewItemAttributeId.Firerate) && !__result.Any((ItemAttributeClass i) => (ENewItemAttributeId)i.Id == ENewItemAttributeId.ProjectileCount))
            {
                AddCustomAttributes(__instance, ref __result);
            }
        }

        public static void AddCustomAttributes(AmmoTemplate ammoTemplate, ref List<ItemAttributeClass> ammoAttributes)
        {
            if (Plugin.EnableAmmoFirerateDisp == true)
            {
                float fireRate = (float)Math.Round((ammoTemplate.casingMass - 1) * 100, 2);

                if (fireRate != 0)
                {
                    ItemAttributeClass fireRateAtt = new ItemAttributeClass(ENewItemAttributeId.Firerate);
                    fireRateAtt.Name = ENewItemAttributeId.Firerate.GetName();
                    fireRateAtt.Base = () => fireRate;
                    fireRateAtt.StringValue = () => $"{fireRate} %";
                    fireRateAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                    fireRateAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
                    fireRateAtt.LessIsGood = true;
                    ammoAttributes.Add(fireRateAtt);
                }
            }

            if (Plugin.enableAmmoProjectileStatsDisp == true)
            {
                var pelletCount = ammoTemplate.ProjectileCount;
                var pelletDamage = ammoTemplate.Damage;

                if (pelletCount > 1 & pelletDamage > 1)
                {
                    ItemAttributeClass pelletCountAtt = new ItemAttributeClass(ENewItemAttributeId.ProjectileCount);
                    ItemAttributeClass pelletDamageAtt = new ItemAttributeClass(ENewItemAttributeId.ProjectileDamage);

                    pelletCountAtt.Name = ENewItemAttributeId.ProjectileCount.GetName();
                    pelletDamageAtt.Name = ENewItemAttributeId.ProjectileDamage.GetName();

                    pelletCountAtt.Base = () => pelletCount;
                    pelletDamageAtt.Base = () => pelletDamage;

                    pelletCountAtt.StringValue = () => pelletCount.ToString();
                    pelletDamageAtt.StringValue = () => pelletDamage.ToString();

                    pelletCountAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                    pelletDamageAtt.DisplayType = () => EItemAttributeDisplayType.Compact;

                    pelletCountAtt.LabelVariations = EItemAttributeLabelVariations.None;
                    pelletDamageAtt.LabelVariations = EItemAttributeLabelVariations.None;

                    ammoAttributes.Add(pelletCountAtt);
                    ammoAttributes.Add(pelletDamageAtt);
                }
            }

            if (Plugin.enableAmmoDamageDisp == true)
            {
                var BuckshotDamage = ammoTemplate.Damage * ammoTemplate.ProjectileCount;
                var damageString = BuckshotDamage.ToString();

                if (ammoTemplate.ProjectileCount > 1)
                {
                    damageString += $" ({ammoTemplate.Damage} x {ammoTemplate.ProjectileCount})";

                    ItemAttributeClass buckshotdamageAtt = new ItemAttributeClass(ENewItemAttributeId.BuckshotDamage);
                    buckshotdamageAtt.Name = ENewItemAttributeId.BuckshotDamage.GetName();
                    buckshotdamageAtt.Base = () => BuckshotDamage;
                    buckshotdamageAtt.StringValue = () => damageString;
                    buckshotdamageAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                    ammoAttributes.Add(buckshotdamageAtt);
                }
                else if (ammoTemplate.ProjectileCount == 1)
                {
                    ItemAttributeClass damageAtt = new ItemAttributeClass(ENewItemAttributeId.Damage);
                    damageAtt.Name = ENewItemAttributeId.Damage.GetName();
                    damageAtt.Base = () => ammoTemplate.Damage;
                    damageAtt.StringValue = () => $"{ammoTemplate.Damage}";
                    damageAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                    ammoAttributes.Add(damageAtt);
                }
            }

            if (Plugin.enableAmmoFragDisp == true)
            {
                if (ammoTemplate.FragmentationChance > 0)
                {
                    ItemAttributeClass fragAtt = new ItemAttributeClass(ENewItemAttributeId.FragmentationChance);
                    fragAtt.Name = ENewItemAttributeId.FragmentationChance.GetName();
                    fragAtt.Base = () => ammoTemplate.FragmentationChance;
                    fragAtt.StringValue = () => $"{ammoTemplate.FragmentationChance * 100}%";
                    fragAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                    ammoAttributes.Add(fragAtt);
                }
            }

            if (Plugin.enableAmmoPenDisp == true)
            {
                if (ammoTemplate.PenetrationPower > 0)
                {
                    string AmmoClassPen()
                    {
                        int ratedClass = 0;

                        if (!Singleton<BackendConfigSettingsClass>.Instantiated) { return $"[MunitionsExpert]: CLASS_DATA_MISSING {ammoTemplate.PenetrationPower}"; }
                        var armorClasses = Singleton<BackendConfigSettingsClass>.Instance.Armor.ArmorClass;

                        for (var i = 0; i < armorClasses.Length; i++)
                        {
                            if (armorClasses[i].Resistance > ammoTemplate.PenetrationPower)
                            {
                                continue;
                            }

                            ratedClass = Math.Max(ratedClass, i);
                        }

                        return $"{(ratedClass > 0 ? $"Класс {ratedClass}" : "Нет пробития")} ({ammoTemplate.PenetrationPower})";
                    }

                    ItemAttributeClass penAtt = new ItemAttributeClass(ENewItemAttributeId.Penetration);
                    penAtt.Name = ENewItemAttributeId.Penetration.GetName();
                    penAtt.Base = () => ammoTemplate.PenetrationPower;
                    penAtt.StringValue = AmmoClassPen; //$"{ammoTemplate.PenetrationPower}";
                    penAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                    ammoAttributes.Add(penAtt);
                }
            }

            if (Plugin.enableAmmoArmorDamageDisp == true)
            {
                if (ammoTemplate.ArmorDamage > 0)
                {
                    ItemAttributeClass armorDamAtt = new ItemAttributeClass(ENewItemAttributeId.ArmorDamage);
                    armorDamAtt.Name = ENewItemAttributeId.ArmorDamage.GetName();
                    armorDamAtt.Base = () => ammoTemplate.ArmorDamage;
                    armorDamAtt.StringValue = () => $"{ammoTemplate.ArmorDamage}%";
                    armorDamAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                    ammoAttributes.Add(armorDamAtt);
                }
            }

            if (Plugin.enableAmmoRicochetChanceDisp == true)
            {
                if (ammoTemplate.RicochetChance > 0)
                {
                    ItemAttributeClass ricochetAtt = new ItemAttributeClass(ENewItemAttributeId.RicochetChance);
                    ricochetAtt.Name = ENewItemAttributeId.RicochetChance.GetName();
                    ricochetAtt.Base = () => ammoTemplate.RicochetChance;
                    ricochetAtt.StringValue = () => $"{ammoTemplate.RicochetChance * 100}%";
                    ricochetAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                    ammoAttributes.Add(ricochetAtt);
                }
            }
        }
    }

    public class HeadsetConstructorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass2297).GetConstructor(new Type[] { typeof(string), typeof(GClass2204) });
        }

        [PatchPostfix]
        private static void PatchPostfix(GClass2297 __instance)
        {
            Item item = __instance;

            float dB = GearProperties.DbLevel(item);

            if (dB > 0)
            {
                List<ItemAttributeClass> dbAtt = item.Attributes;
                ItemAttributeClass dbAttClass = new ItemAttributeClass(ENewItemAttributeId.NoiseReduction);
                dbAttClass.Name = ENewItemAttributeId.NoiseReduction.GetName();
                dbAttClass.Base = () => dB;
                dbAttClass.StringValue = () => dB.ToString() + " Дб";
                dbAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                dbAtt.Add(dbAttClass);
            }
        }
    }

    public class AmmoMalfChanceDisplayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(AmmoTemplate).GetMethod("method_12", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private static readonly string[] malfChancesKeys = new string[]
        {
            "Malfunction/NoneChance",
            "Malfunction/VeryLowChance",
            "Malfunction/LowChance",
            "Malfunction/MediumChance",
            "Malfunction/HighChance",
            "Malfunction/VeryHighChance"
        };

        [PatchPrefix]
        private static bool Prefix(AmmoTemplate __instance, ref string __result)
        {
            float malfChance = __instance.MalfMisfireChance;
            string text = "";
            switch (malfChance)
            {
                case <= 0f:
                    text = malfChancesKeys[0];
                    break;
                case <= 0.15f:
                    text = malfChancesKeys[1];
                    break;
                case <= 0.3f:
                    text = malfChancesKeys[2];
                    break;
                case <= 0.6f:
                    text = malfChancesKeys[3];
                    break;
                case <= 1.2f:
                    text = malfChancesKeys[4];
                    break;
                case > 1.2f:
                    text = malfChancesKeys[5];
                    break;
            }
            __result = text.Localized(null);
            return false;
        }
    }

    public class MagazineMalfChanceDisplayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MagazineClass).GetMethod("method_39", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private static readonly string[] malfChancesKeys = new string[]
        {
        "Malfunction/NoneChance",
        "Malfunction/VeryLowChance",
        "Malfunction/LowChance",
        "Malfunction/MediumChance",
        "Malfunction/HighChance",
        "Malfunction/VeryHighChance"
        };

        [PatchPrefix]
        private static bool Prefix(MagazineClass __instance, ref string __result)
        {
            float malfChance = __instance.MalfunctionChance;
            string text = "";
            switch (malfChance)
            {
                case <= 0f:
                    text = malfChancesKeys[0];
                    break;
                case <= 0.05f:
                    text = malfChancesKeys[1];
                    break;
                case <= 0.2f:
                    text = malfChancesKeys[2];
                    break;
                case <= 0.6f:
                    text = malfChancesKeys[3];
                    break;
                case <= 1.2f:
                    text = malfChancesKeys[4];
                    break;
                case > 1.2f:
                    text = malfChancesKeys[5];
                    break;
            }
            __result = text.Localized(null);
            return false;
        }
    }

    public class AmmoDuraBurnDisplayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(AmmoTemplate).GetMethod("method_16", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static bool Prefix(AmmoTemplate __instance, ref string __result)
        {
            float duraBurn = __instance.DurabilityBurnModificator - 1f;

            switch (duraBurn)
            {
                case <= 1f:
                    __result = duraBurn.ToString("P1");
                    break;
                case <= 6f:
                    __result = "ЗНАЧИТЕЛЬНОЕ ПОВЫШЕНИЕ";
                    break;
                case <= 10f:
                    __result = "БОЛЬШОЕ ПОВЫШЕНИЕ";
                    break;
                case <= 15f:
                    __result = "ОЧЕНЬ БОЛЬШОЕ ПОВЫШЕНИЕ";
                    break;
                case <= 100f:
                    __result = "ОГРОМНОЕ ПОВЫШЕНИЕ";
                    break;
            }
            return false;
        }
    }

    public class ModVRecoilStatDisplayPatchFloat : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Mod).GetMethod("method_15", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static bool Prefix(ref float __result)
        {
            __result = 0;
            return false;
        }
    }

    public class ModVRecoilStatDisplayPatchString : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Mod).GetMethod("method_16", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static bool Prefix(ref string __result)
        {
            __result = "";
            return false;
        }
    }

    public class ModErgoStatDisplayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Mod).GetMethod("method_18", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static bool Prefix(ref Mod __instance, ref string __result)
        {
            __result = __instance.Ergonomics + "%";
            return false;
        }
    }

    public class BarrelModClassPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BarrelModClass).GetConstructor(new Type[] { typeof(string), typeof(GClass2240) });
        }

        [PatchPostfix]
        private static void PatchPostfix(ref BarrelModClass __instance, GClass2240 template)
        {
            float shotDisp = (template.ShotgunDispersion - 1f) * 100f;

            ItemAttributeClass shotDispAtt = new ItemAttributeClass(ENewItemAttributeId.ShotDispersion);
            shotDispAtt.Name = ENewItemAttributeId.ShotDispersion.GetName();
            shotDispAtt.Base = () => shotDisp;
            shotDispAtt.StringValue = () => $"{shotDisp}%";
            shotDispAtt.LessIsGood = false;
            shotDispAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            shotDispAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(shotDispAtt, __instance);
        }
    }

    public class ModConstructorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Mod).GetConstructor(new Type[] { typeof(string), typeof(ModTemplate) });
        }

        [PatchPostfix]
        private static void PatchPostfix(Mod __instance, string id, ModTemplate template)
        {
            float vRecoil = AttachmentProperties.VerticalRecoil(__instance);
            float hRecoil = AttachmentProperties.HorizontalRecoil(__instance);
            float disperion = AttachmentProperties.Dispersion(__instance);
            float cameraRecoil = AttachmentProperties.CameraRecoil(__instance);
            float autoROF = AttachmentProperties.AutoROF(__instance);
            float semiROF = AttachmentProperties.SemiROF(__instance);
            float malfChance = AttachmentProperties.ModMalfunctionChance(__instance);
            float angle = AttachmentProperties.RecoilAngle(__instance);
            float reloadSpeed = AttachmentProperties.ReloadSpeed(__instance);
            float chamberSpeed = AttachmentProperties.ChamberSpeed(__instance);
            float aimSpeed = AttachmentProperties.AimSpeed(__instance);
            float shotDisp = AttachmentProperties.ModShotDispersion(__instance);
            float conv = AttachmentProperties.ModConvergence(__instance);

            if (Plugin.EnableMalfPatch == true && Plugin.ModConfig.malf_changes == true)
            {
                ItemAttributeClass malfAtt = new ItemAttributeClass(ENewItemAttributeId.MalfunctionChance);
                malfAtt.Name = ENewItemAttributeId.MalfunctionChance.GetName();
                malfAtt.Base = () => malfChance;
                malfAtt.StringValue = () => $"{getMalfOdds(malfChance)}";
                malfAtt.LessIsGood = true;
                malfAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                malfAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
                Utils.SafelyAddAttributeToList(malfAtt, __instance);
            }

            ItemAttributeClass hRecoilAtt = new ItemAttributeClass(ENewItemAttributeId.HorizontalRecoil);
            hRecoilAtt.Name = ENewItemAttributeId.HorizontalRecoil.GetName();
            hRecoilAtt.Base = () => hRecoil;
            hRecoilAtt.StringValue = () => $"{hRecoil}%";
            hRecoilAtt.LessIsGood = true;
            hRecoilAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            hRecoilAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(hRecoilAtt, __instance);

            ItemAttributeClass vRecoilAtt = new ItemAttributeClass(ENewItemAttributeId.VerticalRecoil);
            vRecoilAtt.Name = ENewItemAttributeId.VerticalRecoil.GetName();
            vRecoilAtt.Base = () => vRecoil;
            vRecoilAtt.StringValue = () => $"{vRecoil}%";
            vRecoilAtt.LessIsGood = true;
            vRecoilAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            vRecoilAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(vRecoilAtt, __instance);

            ItemAttributeClass dispersionAtt = new ItemAttributeClass(ENewItemAttributeId.Dispersion);
            dispersionAtt.Name = ENewItemAttributeId.Dispersion.GetName();
            dispersionAtt.Base = () => disperion;
            dispersionAtt.StringValue = () => $"{disperion}%";
            dispersionAtt.LessIsGood = true;
            dispersionAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            dispersionAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(dispersionAtt, __instance);

            ItemAttributeClass cameraRecAtt = new ItemAttributeClass(ENewItemAttributeId.CameraRecoil);
            cameraRecAtt.Name = ENewItemAttributeId.CameraRecoil.GetName();
            cameraRecAtt.Base = () => cameraRecoil;
            cameraRecAtt.StringValue = () => $"{cameraRecoil}%";
            cameraRecAtt.LessIsGood = true;
            cameraRecAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            cameraRecAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(cameraRecAtt, __instance);

            ItemAttributeClass autoROFAtt = new ItemAttributeClass(ENewItemAttributeId.AutoROF);
            autoROFAtt.Name = ENewItemAttributeId.AutoROF.GetName();
            autoROFAtt.Base = () => autoROF;
            autoROFAtt.StringValue = () => $"{autoROF}%";
            autoROFAtt.LessIsGood = false;
            autoROFAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            autoROFAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(autoROFAtt, __instance);

            ItemAttributeClass semiROFAtt = new ItemAttributeClass(ENewItemAttributeId.SemiROF);
            semiROFAtt.Name = ENewItemAttributeId.SemiROF.GetName();
            semiROFAtt.Base = () => semiROF;
            semiROFAtt.StringValue = () => $"{semiROF}%";
            semiROFAtt.LessIsGood = false;
            semiROFAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            semiROFAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(semiROFAtt, __instance);

            ItemAttributeClass angleAtt = new ItemAttributeClass(ENewItemAttributeId.RecoilAngle);
            angleAtt.Name = ENewItemAttributeId.RecoilAngle.GetName();
            angleAtt.Base = () => angle;
            angleAtt.StringValue = () => $"{angle}%";
            angleAtt.LessIsGood = false;
            angleAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            angleAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(angleAtt, __instance);

            ItemAttributeClass reloadSpeedAtt = new ItemAttributeClass(ENewItemAttributeId.ReloadSpeed);
            reloadSpeedAtt.Name = ENewItemAttributeId.ReloadSpeed.GetName();
            reloadSpeedAtt.Base = () => reloadSpeed;
            reloadSpeedAtt.StringValue = () => $"{reloadSpeed}%";
            reloadSpeedAtt.LessIsGood = false;
            reloadSpeedAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            reloadSpeedAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(reloadSpeedAtt, __instance);

            ItemAttributeClass chamberSpeedAtt = new ItemAttributeClass(ENewItemAttributeId.ChamberSpeed);
            chamberSpeedAtt.Name = ENewItemAttributeId.ChamberSpeed.GetName();
            chamberSpeedAtt.Base = () => chamberSpeed;
            chamberSpeedAtt.StringValue = () => $"{chamberSpeed}%";
            chamberSpeedAtt.LessIsGood = false;
            chamberSpeedAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            chamberSpeedAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(chamberSpeedAtt, __instance);

            ItemAttributeClass aimSpeedAtt = new ItemAttributeClass(ENewItemAttributeId.AimSpeed);
            aimSpeedAtt.Name = ENewItemAttributeId.AimSpeed.GetName();
            aimSpeedAtt.Base = () => aimSpeed;
            aimSpeedAtt.StringValue = () => $"{aimSpeed}%";
            aimSpeedAtt.LessIsGood = false;
            aimSpeedAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            aimSpeedAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(aimSpeedAtt, __instance);

            ItemAttributeClass shotDispAtt = new ItemAttributeClass(ENewItemAttributeId.ShotDispersion);
            shotDispAtt.Name = ENewItemAttributeId.ShotDispersion.GetName();
            shotDispAtt.Base = () => shotDisp;
            shotDispAtt.StringValue = () => $"{shotDisp}%";
            shotDispAtt.LessIsGood = false;
            shotDispAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            shotDispAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(shotDispAtt, __instance);

            ItemAttributeClass convAtt = new ItemAttributeClass(ENewItemAttributeId.Convergence);
            convAtt.Name = ENewItemAttributeId.Convergence.GetName();
            convAtt.Base = () => conv;
            convAtt.StringValue = () => $"{conv}%";
            convAtt.LessIsGood = false;
            convAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            convAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(convAtt, __instance);
        }

        public static string getMalfOdds(float malfChance)
        {
            switch (malfChance)
            {
                case < 0:
                    return $"{malfChance}%";
                case 0:
                    return "Без изменений";
                case <= 50:
                    return $"{malfChance}%";
                case <= 100:
                    return "Небольшое повышение";
                case <= 500:
                    return "Значительное повышение";
                case <= 1000:
                    return "Большое повышение";
                case <= 5000:
                    return "Критическое повышение";
                default:
                    return "";
            }
        }
    }

    public class CenterOfImpactMOAPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BarrelModClass).GetMethod("get_CenterOfImpactMOA", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(ref BarrelModClass __instance, ref float __result)
        {
            BarrelComponent itemComponent = __instance.GetItemComponent<BarrelComponent>();

            if (itemComponent == null)
            {
                __result = 0f;
            }
            else
            {
                __result = (float)Math.Round((double)(100f * itemComponent.Template.CenterOfImpact / 2.9089f) * 2, 2);
            }

            return false;
        }
    }

    public class WeaponConstructorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetConstructor(new Type[] { typeof(string), typeof(WeaponTemplate) });
        }

        [PatchPostfix]
        private static void PatchPostfix(Weapon __instance, string id, WeaponTemplate template)
        {
            if (Plugin.ShowBalance == true)
            {
                List<ItemAttributeClass> balanceAttList = __instance.Attributes;
                GClass2408 balanceAtt = new GClass2408((EItemAttributeId)ENewItemAttributeId.Balance);
                balanceAtt.Name = ENewItemAttributeId.Balance.GetName();
                balanceAtt.Range = new Vector2(100f, 200f);
                balanceAtt.LessIsGood = false;
                balanceAtt.Base = () => 150;
                balanceAtt.Delta = () => BalanceDelta();
                balanceAtt.StringValue = () => Math.Round(DisplayWeaponProperties.Balance, 1).ToString();
                balanceAtt.DisplayType = () => EItemAttributeDisplayType.FullBar;
                balanceAttList.Add(balanceAtt);
            }

            if (Plugin.ShowDispersion == true)
            {
                List<ItemAttributeClass> dispersionAttList = __instance.Attributes;
                GClass2408 dispersionAtt = new GClass2408((EItemAttributeId)ENewItemAttributeId.Dispersion);
                dispersionAtt.Name = ENewItemAttributeId.Dispersion.GetName();
                dispersionAtt.Range = new Vector2(0f, 50f);
                dispersionAtt.LessIsGood = true;
                dispersionAtt.Base = () => __instance.Template.RecolDispersion;
                dispersionAtt.Delta = () => DispersionDelta(__instance);
                dispersionAtt.StringValue = () => Math.Round(DisplayWeaponProperties.Dispersion, 1).ToString();
                dispersionAtt.DisplayType = () => EItemAttributeDisplayType.FullBar;
                dispersionAttList.Add(dispersionAtt);
            }

            if (Plugin.ShowCamRecoil == true)
            {
                List<ItemAttributeClass> camRecoilAttList = __instance.Attributes;
                GClass2408 camRecoilAtt = new GClass2408((EItemAttributeId)ENewItemAttributeId.CameraRecoil);
                camRecoilAtt.Name = ENewItemAttributeId.CameraRecoil.GetName();
                camRecoilAtt.Range = new Vector2(0f, 50f);
                camRecoilAtt.LessIsGood = true;
                camRecoilAtt.Base = () => __instance.Template.CameraRecoil * 100f;
                camRecoilAtt.Delta = () => CamRecoilDelta(__instance);
                camRecoilAtt.StringValue = () => Math.Round(DisplayWeaponProperties.CamRecoil * 100f, 2).ToString();
                camRecoilAtt.DisplayType = () => EItemAttributeDisplayType.FullBar;
                camRecoilAttList.Add(camRecoilAtt);
            }

            if (Plugin.ShowRecoilAngle == true)
            {
                List<ItemAttributeClass> recoilAngleAttList = __instance.Attributes;
                ItemAttributeClass recoilAngleAtt = new ItemAttributeClass(ENewItemAttributeId.RecoilAngle);
                recoilAngleAtt.Name = ENewItemAttributeId.RecoilAngle.GetName();
                recoilAngleAtt.Base = () => DisplayWeaponProperties.RecoilAngle;
                recoilAngleAtt.StringValue = () => Math.Round(DisplayWeaponProperties.RecoilAngle).ToString();
                recoilAngleAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                recoilAngleAttList.Add(recoilAngleAtt);
            }

            if (Plugin.ShowSemiROF == true)
            {
                List<ItemAttributeClass> semiROFAttList = __instance.Attributes;
                ItemAttributeClass semiROFAtt = new ItemAttributeClass(ENewItemAttributeId.SemiROF);
                semiROFAtt.Name = ENewItemAttributeId.SemiROF.GetName();
                semiROFAtt.Base = () => DisplayWeaponProperties.SemiFireRate;
                semiROFAtt.StringValue = () => DisplayWeaponProperties.SemiFireRate.ToString() + " " + "RPM".Localized(null);
                semiROFAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                semiROFAttList.Add(semiROFAtt);
            }
        }

        private static float BalanceDelta()
        {
            float currentBalance = 150f - (DisplayWeaponProperties.Balance * -1f);
            return (150f - currentBalance) / (150f * -1f);
        }

        private static float DispersionDelta(Weapon __instance)
        {
            return (__instance.Template.RecolDispersion - DisplayWeaponProperties.Dispersion) / (__instance.Template.RecolDispersion * -1f);
        }

        private static float CamRecoilDelta(Weapon __instance)
        {
            float tempalteCam = __instance.Template.CameraRecoil * 100f;
            return (tempalteCam - (DisplayWeaponProperties.CamRecoil * 100f)) / (tempalteCam * -1f);
        }
    }

    public class HRecoilDisplayDeltaPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_25", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static bool Prefix(ref Weapon __instance, ref float __result)
        {
            __result = DisplayWeaponProperties.HRecoilDelta;
            return false;
        }
    }

    public class HRecoilDisplayStringValuePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_26", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static bool Prefix(ref Weapon __instance, ref string __result)
        {
            __result = Math.Round(__instance.Template.RecoilForceBack + __instance.Template.RecoilForceBack * DisplayWeaponProperties.HRecoilDelta, 1).ToString();
            return false;
        }
    }

    public class VRecoilDisplayDeltaPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_22", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static bool Prefix(ref Weapon __instance, ref float __result)
        {
            __result = DisplayWeaponProperties.VRecoilDelta;
            return false;
        }
    }

    public class VRecoilDisplayStringValuePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_23", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static bool Prefix(ref Weapon __instance, ref string __result)
        {
            __result = Math.Round(__instance.Template.RecoilForceUp + __instance.Template.RecoilForceUp * DisplayWeaponProperties.VRecoilDelta, 1).ToString();
            return false;
        }
    }

    public class COIDisplayDeltaPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_17", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static bool Prefix(ref Weapon __instance, ref float __result)
        {
            float Single_0 = (float)AccessTools.Property(typeof(Weapon), "Single_0").GetValue(__instance);
            MethodInfo method_9 = AccessTools.Method(typeof(Weapon), "method_9");

            float num = (float)method_9.Invoke(__instance, new object[] { __instance.Repairable.TemplateDurability });
            float num2 = (__instance.GetBarrelDeviation() - num) / (Single_0 - num);
            __result = DisplayWeaponProperties.COIDelta + num2;
            return false;
        }
    }

    public class COIDisplayStringValuePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_18", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static bool Prefix(ref Weapon __instance, ref string __result)
        {
            __result = ((GetTotalCOI(ref __instance, true) * __instance.GetBarrelDeviation() * 100f / 2.9089f) * 2f).ToString("0.0#") + " " + "MOA".Localized(null);
            return false;
        }

        private static float GetTotalCOI(ref Weapon __instance, bool includeAmmo)
        {
            float num = __instance.CenterOfImpactBase * (1f + DisplayWeaponProperties.COIDelta);

            if (!includeAmmo)
            {
                return num;
            }

            AmmoTemplate currentAmmoTemplate = __instance.CurrentAmmoTemplate;
            return num * ((currentAmmoTemplate != null) ? currentAmmoTemplate.AmmoFactor : 1f);
        }
    }

    public class FireRateDisplayStringValuePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_35", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static bool Prefix(ref string __result)
        {
            __result = DisplayWeaponProperties.AutoFireRate.ToString() + " " + "выс/мин".Localized(null);
            return false;
        }
    }

    public class ErgoDisplayDeltaPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_14", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static bool Prefix(ref Weapon __instance, ref float __result)
        {
            if (Plugin.EnableStatsDelta.Value == true)
            {
                StatDeltaDisplay.DisplayDelta(__instance, Logger);
            }

            __result = DisplayWeaponProperties.ErgoDelta;
            return false;
        }
    }

    public class ErgoDisplayStringValuePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_15", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static bool Prefix(ref Weapon __instance, ref string __result)
        {
            StatDeltaDisplay.DisplayDelta(__instance, Logger);
            float ergoTotal = __instance.Template.Ergonomics * (1f + DisplayWeaponProperties.ErgoDelta);
            __result = Mathf.Clamp(ergoTotal, 0f, 100f).ToString("0.##");
            return false;
        }
    }

    public static class StatDeltaDisplay
    {
        public static void DisplayDelta(Weapon __instance, ManualLogSource logger)
        {
            float baseCOI = __instance.CenterOfImpactBase;
            float currentCOI = baseCOI;

            float baseAutoROF = __instance.Template.bFirerate;
            float currentAutoROF = baseAutoROF;

            float baseSemiROF = Mathf.Max(__instance.Template.SingleFireRate, 240);
            float currentSemiROF = baseSemiROF;

            float baseCamRecoil = __instance.Template.CameraRecoil;
            float currentCamRecoil = baseCamRecoil;

            float baseConv = __instance.Template.Convergence;
            float currentConv = baseConv;

            float baseDispersion = __instance.Template.RecolDispersion;
            float currentDispersion = baseDispersion;

            float baseAngle = __instance.Template.RecoilAngle;
            float currentRecoilAngle = baseAngle;

            float baseVRecoil = __instance.Template.RecoilForceUp;
            float currentVRecoil = baseVRecoil;
            float baseHRecoil = __instance.Template.RecoilForceBack;
            float currentHRecoil = baseHRecoil;

            float baseErgo = __instance.Template.Ergonomics;
            float currentErgo = baseErgo;
            float pureErgo = baseErgo;
            float pureRecoil = 0;

            float currentTorque = 0f;

            float currentReloadSpeed = 0f;

            float currentAimSpeed = 0f;

            float currentChamberSpeed = 0f;

            float currentShotDisp = 0f;

            float currentMalfChance = 0f;

            float currentFixSpeed = 0f;

            string weapOpType = WeaponProperties.OperationType(__instance);
            string weapType = WeaponProperties.WeaponType(__instance);

            float currentLoudness = 0;

            bool folded = __instance.Folded;

            bool hasShoulderContact = false;

            bool stockAllowsFSADS = false;

            if (WeaponProperties.WepHasShoulderContact(__instance) && !folded)
            {
                hasShoulderContact = true;
            }

            for (int i = 0; i < __instance.Mods.Length; i++)
            {
                Mod mod = __instance.Mods[i];
                float modWeight = __instance.Mods[i].Weight;
                if (Utils.IsMagazine(__instance.Mods[i]))
                {
                    modWeight = __instance.Mods[i].GetSingleItemTotalWeight();
                }
                float modWeightFactored = StatCalc.FactoredWeight(modWeight);
                float modErgo = __instance.Mods[i].Ergonomics;
                float modVRecoil = AttachmentProperties.VerticalRecoil(__instance.Mods[i]);
                float modHRecoil = AttachmentProperties.HorizontalRecoil(__instance.Mods[i]);
                float modAutoROF = AttachmentProperties.AutoROF(__instance.Mods[i]);
                float modSemiROF = AttachmentProperties.SemiROF(__instance.Mods[i]);
                float modCamRecoil = AttachmentProperties.CameraRecoil(__instance.Mods[i]);
                float modConv = AttachmentProperties.ModConvergence(__instance.Mods[i]);
                float modDispersion = AttachmentProperties.Dispersion(__instance.Mods[i]);
                float modAngle = AttachmentProperties.RecoilAngle(__instance.Mods[i]);
                float modAccuracy = __instance.Mods[i].Accuracy;
                float modReload = 0f;
                float modChamber = 0f;
                float modAim = 0f;
                float modLoudness = 0f;
                float modMalfChance = 0f;
                float modDuraBurn = 0f;
                float modFix = 0f;
                string modType = AttachmentProperties.ModType(__instance.Mods[i]);
                string position = StatCalc.GetModPosition(__instance.Mods[i], weapType, weapOpType, modType);

                StatCalc.ModConditionalStatCalc(__instance, mod, folded, weapType, weapOpType, ref hasShoulderContact, ref modAutoROF, ref modSemiROF, ref stockAllowsFSADS, ref modVRecoil, ref modHRecoil, ref modCamRecoil, ref modAngle, ref modDispersion, ref modErgo, ref modAccuracy, ref modType, ref position, ref modChamber, ref modLoudness, ref modMalfChance, ref modDuraBurn, ref modConv);
                StatCalc.ModStatCalc(mod, modWeight, ref currentTorque, position, modWeightFactored, modAutoROF, ref currentAutoROF, modSemiROF, ref currentSemiROF, modCamRecoil, ref currentCamRecoil, modDispersion, ref currentDispersion, modAngle, ref currentRecoilAngle, modAccuracy, ref currentCOI, modAim, ref currentAimSpeed, modReload, ref currentReloadSpeed, modFix, ref currentFixSpeed, modErgo, ref currentErgo, modVRecoil, ref currentVRecoil, modHRecoil, ref currentHRecoil, ref currentChamberSpeed, modChamber, true, __instance.WeapClass, ref pureErgo, 0, ref currentShotDisp, modLoudness, ref currentLoudness, ref currentMalfChance, modMalfChance, ref pureRecoil, ref currentConv, modConv);
            }

            float totalTorque = 0;
            float totalErgo = 0;
            float totalVRecoil = 0;
            float totalHRecoil = 0;
            float totalDispersion = 0;
            float totalCamRecoil = 0;
            float totalRecoilAngle = 0;
            float totalRecoilDamping = 0;
            float totalRecoilHandDamping = 0;

            float totalErgoDelta = 0;
            float totalVRecoilDelta = 0;
            float totalHRecoilDelta = 0;

            float totalCOI = 0;
            float totalCOIDelta = 0;
            float pureErgoDelta = 0f;

            StatCalc.WeaponStatCalc(__instance, currentTorque, ref totalTorque, currentErgo, currentVRecoil, currentHRecoil, currentDispersion, currentCamRecoil, currentRecoilAngle, baseErgo, baseVRecoil, baseHRecoil, ref totalErgo, ref totalVRecoil, ref totalHRecoil, ref totalDispersion, ref totalCamRecoil, ref totalRecoilAngle, ref totalRecoilDamping, ref totalRecoilHandDamping, ref totalErgoDelta, ref totalVRecoilDelta, ref totalHRecoilDelta, ref totalRecoilDamping, ref totalRecoilHandDamping, currentCOI, hasShoulderContact, ref totalCOI, ref totalCOIDelta, baseCOI, pureErgo, ref pureErgoDelta, true);

            DisplayWeaponProperties.HasShoulderContact = hasShoulderContact;
            DisplayWeaponProperties.Dispersion = totalDispersion;
            DisplayWeaponProperties.CamRecoil = totalCamRecoil;
            DisplayWeaponProperties.RecoilAngle = totalRecoilAngle;
            DisplayWeaponProperties.TotalVRecoil = totalVRecoil;
            DisplayWeaponProperties.TotalHRecoil = totalHRecoil;
            DisplayWeaponProperties.Balance = totalTorque;
            DisplayWeaponProperties.TotalErgo = totalErgo;
            DisplayWeaponProperties.ErgoDelta = totalErgoDelta;
            DisplayWeaponProperties.VRecoilDelta = totalVRecoilDelta;
            DisplayWeaponProperties.HRecoilDelta = totalHRecoilDelta;
            DisplayWeaponProperties.AutoFireRate = Mathf.Max(300, (int)currentAutoROF);
            DisplayWeaponProperties.SemiFireRate = Mathf.Max(200, (int)currentSemiROF);
            DisplayWeaponProperties.COIDelta = totalCOIDelta;
        }
    }
}
