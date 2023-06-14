﻿using EFT.InventoryLogic;

namespace RealismMod
{
    public static class DisplayWeaponProperties
    {
        public static float ErgoDelta = 0;

        public static int AutoFireRate = 0;

        public static int SemiFireRate = 0;

        public static float Balance = 0;

        public static float VRecoilDelta = 0;

        public static float HRecoilDelta = 0;

        public static bool HasShoulderContact = true;

        public static float COIDelta = 0;

        public static float CamRecoil = 0;

        public static float Dispersion = 0;

        public static float RecoilAngle = 0;

        public static float TotalVRecoil = 0;

        public static float TotalHRecoil = 0;

        public static float TotalErgo = 0;

        public static float ErgnomicWeight = 0;
    }

    public static class WeaponProperties
    {

        public static string WeaponType(Weapon weapon)
        {
            return !Utils.NullCheck(weapon.ConflictingItems) ? weapon.ConflictingItems[1] : "Unknown";

        }

        public static float BaseTorqueDistance(Weapon weapon)
        {
            return !Utils.NullCheck(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[2], out float result) ? result : 0f;

        }

        public static bool WepHasShoulderContact(Weapon weapon)
        {
            return !Utils.NullCheck(weapon.ConflictingItems) && bool.TryParse(weapon.ConflictingItems[3], out bool result) ? result : false;

        }

        public static float BaseReloadSpeed(Weapon weapon)
        {
            return !Utils.NullCheck(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[4], out float result) ? result : 1f;

        }

        public static string OperationType(Weapon weapon)
        {
            return !Utils.NullCheck(weapon.ConflictingItems) ? weapon.ConflictingItems[5] : "Unknown";

        }

        public static float WeaponAccuracy(Weapon weapon)
        {
 
            return !Utils.NullCheck(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[6], out float result) ? result : 0f;

        }

        public static float RecoilDamping(Weapon weapon)
        {;
            return !Utils.NullCheck(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[7], out float result) ? result : 0f;

        }

        public static float RecoilHandDamping(Weapon weapon)
        {
            return !Utils.NullCheck(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[8], out float result) ? result : 0.65f;

        }

        public static bool WeaponAllowsADS(Weapon weapon)
        {
            return !Utils.NullCheck(weapon.ConflictingItems) && bool.TryParse(weapon.ConflictingItems[9], out bool result) ? result : false;

        }

        public static float BaseChamberSpeed(Weapon weapon)
        {
            return !Utils.NullCheck(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[10], out float result) ? result : 1f;

        }

        public static float MaxChamberSpeed(Weapon weapon)
        {
            return !Utils.NullCheck(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[11], out float result) ? result : 1.2f;

        }


        public static float MinChamberSpeed(Weapon weapon)
        {
            return !Utils.NullCheck(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[12], out float result) ? result : 0.7f;

        }

        public static bool IsManuallyOperated(Weapon weapon)
        {
            return !Utils.NullCheck(weapon.ConflictingItems) && bool.TryParse(weapon.ConflictingItems[13], out bool result) ? result : false;

        }

        public static float MaxReloadSpeed(Weapon weapon)
        {
            return !Utils.NullCheck(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[14], out float result) ? result : 1.2f;

        }

        public static float MinReloadSpeed(Weapon weapon)
        {
            return !Utils.NullCheck(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[15], out float result) ? result : 0.7f;

        }

        public static float BaseChamberCheckSpeed(Weapon weapon)
        {
            return !Utils.NullCheck(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[16], out float result) ? result : 1f;

        }

        public static float BaseFixSpeed(Weapon weapon)
        {
            return !Utils.NullCheck(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[17], out float result) ? result : 1f;

        }

        public static float AnimationWeightFactor = 1f;

        public static float BaseHipfireInaccuracy;

        public static float BaseWeaponLength = 0f;
        public static float NewWeaponLength = 0f;

        public static string WeapID = "";

        public static float TotalChamberCheckSpeed = 1;

        public static bool _IsManuallyOperated = false;

        public static float TotalModDuraBurn = 1;

        public static float TotalMalfChance = 0;

        public static bool CanCycleSubs = false;

        public static string _WeapClass = "";

        public static bool ShouldGetSemiIncrease = false;

        public static float AdapterPistolGripBonusVRecoil = -1;

        public static float AdapterPistolGripBonusHRecoil = -2;

        public static float AdapterPistolGripBonusDispersion = -1;

        public static float AdapterPistolGripBonusChamber = 10;

        public static float AdapterPistolGripBonusErgo = 2;

        public static float PumpGripReloadBonus = 18f;

        public static float FoldedErgoFactor = 1.0f;

        public static float FoldedHRecoilFactor = 1.15f;

        public static float FoldedVRecoilFactor = 1.5f;

        public static float FoldedCOIFactor = 2f;

        public static float FoldedCamRecoilFactor = 0.4f;

        public static float FoldedDispersionFactor = 1.55f;

        public static float FoldedRecoilAngleFactor = 1.35f;

        public static float ErgoStatFactor = 7f;

        public static float RecoilStatFactor = 3.5f;

        public static float ErgoDelta = 0f;

        public static int AutoFireRate = 0;

        public static int SemiFireRate = 0;

        public static float Balance = 0f;

        public static float VRecoilDelta = 0f;

        public static float HRecoilDelta = 0f;

        public static bool HasShoulderContact = true;

        public static float ShotDispDelta = 0f;

        public static float COIDelta = 0f;

        public static float CamRecoil = 0f;

        public static float Dispersion = 0f;

        public static float RecoilAngle = 0f;

        public static float TotalVRecoil = 0f;

        public static float TotalHRecoil = 0f;

        public static float TotalErgo = 0f;

        public static float InitTotalErgo = 0f;

        public static float InitTotalVRecoil = 0f;

        public static float InitTotalHRecoil = 0f;

        public static float InitBalance = 0f;

        public static float InitCamRecoil = 0f;

        public static float ModdedConv = 0f;

        public static float InitDispersion = 0f;

        public static float InitRecoilAngle = 0f;

        public static float InitTotalCOI = 0f;

        public static string SavedInstanceID = "";

        public static float InitPureErgo = 0f;

        public static float PureRecoilDelta = 0f;

        public static float PureErgoDelta = 0f;

        public static string Placement = "";

        public static float ErgonomicWeight = 1f;

        public static float ErgoFactor = 1f;

        public static float ADSDelta = 0f;

        public static float TotalRecoilDamping;

        public static float TotalRecoilHandDamping;

        public static bool WeaponCanFSADS = false;

        public static bool Folded = false;

        public static float SDReloadSpeedModifier = 1f;

        public static float SDFixSpeedModifier = 1f;

        public static float TotalReloadSpeedLessMag = 1f;

        public static float TotalChamberSpeed = 1f;

        public static float TotalFixSpeed = 1f;

        public static float TotalFiringChamberSpeed = 1f;

        public static float SDChamberSpeedModifier = 1f;

        public static float AimMoveSpeedModifier = 1f;

        public static float ModAimSpeedModifier = 1f;

        public static float GlobalAimSpeedModifier = 1f;

        public static float SightlessAimSpeed = 1f;

        public static float ErgoStanceSpeed = 1f;

        public static float CurrentMagReloadSpeed = 1f;
        public static float NewMagReloadSpeed = 1f;

        public static float ConvergenceChangeRate = 0.98f;
        public static float ConvergenceResetRate = 1.16f;
        public static float ConvergenceLimit = 0.3f;

        public static float CamRecoilChangeRate = 0.987f;
        public static float CamRecoilResetRate = 1.17f;
        public static float CamRecoilLimit = 0.45f;

        public static float VRecoilChangeRate = 1.005f;
        public static float VRecoilResetRate = 0.91f;
        public static float VRecoilLimit = 10;

        public static float HRecoilChangeRate = 1.005f;
        public static float HRecoilResetRate = 0.91f;
        public static float HRecoilLimit = 10;

        public static float DampingChangeRate = 0.98f;
        public static float DampingResetRate = 1.07f;
        public static float DampingLimit = 0.5f;

        public static float DispersionChangeRate = 0.95f;
        public static float DispersionResetRate = 1.05f;
        public static float DispersionLimit = 0.5f;
    }
}
