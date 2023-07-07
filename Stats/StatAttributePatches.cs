using System;
using System.Reflection;
using Aki.Reflection.Patching;
using EFT.UI;
using UnityEngine;

namespace RealismMod
{
    public class GetAttributeIconPatches : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(StaticIcons).GetMethod("GetAttributeIcon", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(ref Sprite __result, Enum id)
        {
            if (id == null || !Plugin.IconCache.ContainsKey(id))
            {
                return true;
            }

            Sprite sprite = Plugin.IconCache[id];

            if (sprite != null)
            {
                __result = sprite;
                return false;
            }

            return true;
        }
    }

    public static class Attributes
    {
        public enum ENewItemAttributeId
        {
            HorizontalRecoil,
            VerticalRecoil,
            Balance = 11,
            CameraRecoil,
            Dispersion,
            MalfunctionChance,
            AutoROF,
            SemiROF,
            RecoilAngle,
            ReloadSpeed,
            FixSpeed,
            AimSpeed,
            ChamberSpeed,
            Firerate,
            Damage,
            BuckshotDamage,
            Penetration,
            ArmorDamage,
            FragmentationChance,
            RicochetChance,
            BluntThroughput,
            ShotDispersion,
            GearReloadSpeed,
            CanSpall,
            SpallReduction,
            CanAds,
            NoiseReduction,
            ProjectileCount,
            ProjectileDamage,
            Convergence,
            HBleedType,
            LimbHpPerTick,
            HpPerTick,
            RemoveTrnqt
        }

        public static string GetName(this ENewItemAttributeId id)
        {
            switch (id)
            {
                case ENewItemAttributeId.HorizontalRecoil:
                    return "ГОРИЗОНТАЛЬНАЯ ОТДАЧА";
                case ENewItemAttributeId.VerticalRecoil:
                    return "ВЕРТИКАЛЬНАЯ ОТДАЧА";
                case ENewItemAttributeId.Balance:
                    return "БАЛАНС";
                case ENewItemAttributeId.Dispersion:
                    return "РАЗБРОС";
                case ENewItemAttributeId.CameraRecoil:
                    return "ПОДБРОС КАМЕРЫ";
                case ENewItemAttributeId.MalfunctionChance:
                    return "ШАНС НЕИСПРАВНОСТИ";
                case ENewItemAttributeId.AutoROF:
                    return "СКОРОСТРЕЛЬНОСТЬ (АВТОМАТ)";
                case ENewItemAttributeId.SemiROF:
                    return "СКОРОСТРЕЛЬНОСТЬ (ПОЛУАВТОМАТ)";
                case ENewItemAttributeId.RecoilAngle:
                    return "УГОЛ ОТДАЧИ";
                case ENewItemAttributeId.ReloadSpeed:
                    return "СКОРОСТЬ ПЕРЕЗАРЯДКИ";
                case ENewItemAttributeId.FixSpeed:
                    return "СКОРОСТЬ ИСПРАВЛЕНИЯ НЕИСПРАВНОСТИ";
                case ENewItemAttributeId.AimSpeed:
                    return "СКОРОСТЬ ПРИЦЕЛИВАНИЯ";
                case ENewItemAttributeId.ChamberSpeed:
                    return "СКОРОСТЬ ПРОВЕРКИ ПАТРОННИКА";
                case ENewItemAttributeId.Firerate:
                    return "СКОРОСТРЕЛЬНОСТЬ";
                case ENewItemAttributeId.Damage:
                    return "УРОН";
                case ENewItemAttributeId.Penetration:
                    return "БРОНЕПРОБИВАЕМОСТЬ";
                case ENewItemAttributeId.ArmorDamage:
                    return "ПОВРЕЖДЕНИЕ БРОНИ";
                case ENewItemAttributeId.FragmentationChance:
                    return "ВЕРОЯТНОСТЬ ФРАГМЕНТАЦИИ";
                case ENewItemAttributeId.RicochetChance:
                    return "ШАНС РИКОШЕТА";
                case ENewItemAttributeId.BluntThroughput:
                    return "СНИЖЕНИЕ ЗАБРОНЕВОГО УРОНА";
                case ENewItemAttributeId.ShotDispersion:
                    return "УМЕНЬШЕНИЕ РАЗЛЁТА ПУЛЬ";
                case ENewItemAttributeId.CanSpall:
                    return "ЗАЩИТА ОТ ФРАГМЕНТАЦИИ ПУЛИ";
                case ENewItemAttributeId.SpallReduction:
                    return "СТЕПЕНЬ ЗАЩИТЫ ОТ ФРАГМЕНТАЦИИ";
                case ENewItemAttributeId.GearReloadSpeed:
                    return "МОДИФИКАТОР СКОРОСТИ ПЕРЕЗАРЯДКИ";
                case ENewItemAttributeId.CanAds:
                    return "ВОЗМОЖНОСТЬ ПРИЦЕЛИВАНИЯ";
                case ENewItemAttributeId.NoiseReduction:
                    return "УРОВЕНЬ ПОДАВЛЕНИЯ ШУМА";
                case ENewItemAttributeId.ProjectileCount:
                    return "КОЛИЧЕСТВО ФРАГМЕНТОВ";
                case ENewItemAttributeId.ProjectileDamage:
                    return "УРОН ОДНОГО ФРАГМЕНТА";
                case ENewItemAttributeId.BuckshotDamage:
                    return "ОБЩИЙ УРОН";
                case ENewItemAttributeId.Convergence:
                    return "НАСТИЛЬНОСТЬ";
                case ENewItemAttributeId.HBleedType:
                    return "СПОСОБ ЛЕЧЕНИЯ КРОВОТЕЧЕНИЙ";
                case ENewItemAttributeId.LimbHpPerTick:
                    return "ПОТЕРЯ ОЗ ЗА ЕД.ВРЕМЕНИ";
                case ENewItemAttributeId.HpPerTick:
                    return "ИЗМЕНЕНИЕ ОЗ ЗА ЕД.ВРЕМЕНИ";
                case ENewItemAttributeId.RemoveTrnqt:
                    return "УБИРАЕТ ЭФФЕКТ ЖГУТА";
                default:
                    return id.ToString();
            }
        }
    }
}
