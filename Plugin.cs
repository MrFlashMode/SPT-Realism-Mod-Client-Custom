using Aki.Common.Http;
using Aki.Common.Utils;
using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using static RealismMod.ArmorPatches;
using static RealismMod.Attributes;

namespace RealismMod
{
    public class ConfigTemplate
    {
        public bool recoil_attachment_overhaul { get; set; }
        public bool malf_changes { get; set; }
        public bool realistic_ballistics { get; set; }
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<float> ConvSemiMulti { get; set; }
        public static ConfigEntry<float> ConvAutoMulti { get; set; }
        public static ConfigEntry<float> resetTime { get; set; }
        public static ConfigEntry<float> vRecoilLimit { get; set; }
        public static ConfigEntry<float> hRecoilLimit { get; set; }
        public static ConfigEntry<float> convergenceLimit { get; set; }
        public static ConfigEntry<float> ConvergenceResetRate { get; set; }
        public static ConfigEntry<float> vRecoilChangeMulti { get; set; }
        public static ConfigEntry<float> vRecoilResetRate { get; set; }
        public static ConfigEntry<float> hRecoilChangeMulti { get; set; }
        public static ConfigEntry<float> hRecoilResetRate { get; set; }
        public static ConfigEntry<float> SensChangeRate { get; set; }
        public static ConfigEntry<float> SensResetRate { get; set; }
        public static ConfigEntry<float> SensLimit { get; set; }
        public static ConfigEntry<bool> EnableFSPatch { get; set; }
        public static ConfigEntry<bool> EnableNVGPatch { get; set; }
        public static ConfigEntry<bool> EnableReloadPatches { get; set; }
        public static ConfigEntry<bool> EnableRealArmorClass { get; set; }
        public static ConfigEntry<bool> ReduceCamRecoil { get; set; }
        public static ConfigEntry<float> ConvergenceSpeedCurve { get; set; }
        public static ConfigEntry<bool> EnableDeafen { get; set; }
        public static ConfigEntry<bool> EnableHoldBreath { get; set; }
        public static ConfigEntry<bool> EnableRecoilClimb { get; set; }
        public static ConfigEntry<float> RecoilIntensity { get; set; }
        public static ConfigEntry<bool> EnableHipfireRecoilClimb { get; set; }

        public static ConfigEntry<KeyboardShortcut> ActiveAimKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> LowReadyKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> HighReadyKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> ShortStockKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> CycleStancesKeybind { get; set; }

        public static ConfigEntry<bool> ToggleActiveAim { get; set; }
        public static ConfigEntry<bool> StanceToggleDevice { get; set; }
        public static ConfigEntry<bool> ActiveAimReload { get; set; }
        public static ConfigEntry<bool> EnableAltPistol { get; set; }
        public static ConfigEntry<bool> EnableStanceStamChanges { get; set; }
        public static ConfigEntry<bool> EnableTacSprint { get; set; }
        public static ConfigEntry<bool> EnableLogging { get; set; }
        public static ConfigEntry<bool> EnableBallisticsLogging { get; set; }

        //public static ConfigEntry<bool> EnableMedicalOvehaul { get; set; }
        //public static ConfigEntry<bool> GearBlocksHeal { get; set; }
        public static ConfigEntry<bool> GearBlocksEat { get; set; }
        //public static ConfigEntry<bool> TrnqtEffect { get; set; }
        public static ConfigEntry<bool> HealthEffects { get; set; }
        //public static ConfigEntry<KeyboardShortcut> DropGearKeybind { get; set; }

        public static ConfigEntry<bool> EnableMaterialSpeed { get; set; }
        public static ConfigEntry<bool> EnableSlopeSpeed { get; set; }

        public static Weapon CurrentlyShootingWeapon;

        public static Vector3 PistolTransformNewStartPosition;
        public static Vector3 TransformBaseStartPosition;
        public static Vector3 WeaponOffsetPosition;

        public static bool DidWeaponSwap = false;
        public static bool IsSprinting = false;
        public static bool IsInInventory = false;

        public static bool IsFiring = false;

        public static bool IsBotFiring = false;
        public static bool GrenadeExploded = false;
        public static bool IsAiming = false;
        public static bool IsBlindFiring = false;
        public static float Timer = 0.0f;
        public static float BotTimer = 0.0f;
        public static float GrenadeTimer = 0.0f;

        public static int ShotCount = 0;
        public static int PrevShotCount = ShotCount;
        public static bool StatsAreReset;

        public static float StartingRecoilAngle;

        public static float StartingAimSens;
        public static float CurrentAimSens = StartingAimSens;
        public static float StartingHipSens;
        public static float CurrentHipSens = StartingHipSens;
        public static bool CheckedForSens = false;

        public static float StartingDispersion;
        public static float CurrentDispersion;
        public static float DispersionProportionK;

        public static float StartingDamping;
        public static float CurrentDamping;

        public static float StartingHandDamping;
        public static float CurrentHandDamping;

        public static float StartingConvergence;
        public static float CurrentConvergence;
        public static float ConvergenceProporitonK;

        public static float StartingCamRecoilX;
        public static float StartingCamRecoilY;
        public static float CurrentCamRecoilX;
        public static float CurrentCamRecoilY;

        public static float StartingVRecoilX;
        public static float StartingVRecoilY;
        public static float CurrentVRecoilX;
        public static float CurrentVRecoilY;

        public static float StartingHRecoilX;
        public static float StartingHRecoilY;
        public static float CurrentHRecoilX;
        public static float CurrentHRecoilY;

        public static bool LauncherIsActive = false;

        public static Dictionary<Enum, Sprite> IconCache = new Dictionary<Enum, Sprite>();

        private string ModPath;
        private string ConfigFilePath;
        private string ConfigJson;
        public static ConfigTemplate ModConfig;

        public static float MainVolume = 0f;
        public static float GunsVolume = 0f;
        public static float AmbientVolume = 0f;
        public static float AmbientOccluded = 0f;
        public static float CompressorDistortion = 0f;
        public static float CompressorResonance = 0f;
        public static float CompressorLowpass = 0f;
        public static float Compressor = 0f;
        public static float CompressorGain = 0f;

        public static bool HasHeadSet = false;
        public static CC_FastVignette Vignette;
        public static PrismEffects PrismEffects;

        public static bool HasOptic = false;

        public static float healthControllerTick = 0f;

        public static bool IsInThirdPerson = false;

        public static Player.BetterValueBlender StanceBlender = new Player.BetterValueBlender
        {
            Speed = 5f,
            Target = 0f
        };

        //Delete some BepInEx paramenters
        public static float WeapOffsetX = 0.0f;
        public static float WeapOffsetY = 0.0f;
        public static float WeapOffsetZ = 0.0f;

        public static float StanceTransitionSpeed = 8.0f;
        public static float ThirdPersonRotationSpeed = 1.5f;
        public static float ThirdPersonPositionSpeed = 2.0f;

        public static float ActiveAimAdditionalRotationSpeedMulti = 1.0f;
        public static float ActiveAimResetRotationSpeedMulti = 3.0f;
        public static float ActiveAimRotationMulti = 1.0f;
        public static float ActiveAimSpeedMulti = 10.0f;
        public static float ActiveAimResetSpeedMulti = 8.0f;

        public static float ActiveAimOffsetX = -0.04f;
        public static float ActiveAimOffsetY = -0.01f;
        public static float ActiveAimOffsetZ = -0.01f;

        public static float ActiveAimRotationX = 0.0f;
        public static float ActiveAimRotationY = -30.0f;
        public static float ActiveAimRotationZ = 0.0f;

        public static float ActiveAimAdditionalRotationX = -1.5f;
        public static float ActiveAimAdditionalRotationY = -70f;
        public static float ActiveAimAdditionalRotationZ = 2f;

        public static float ActiveAimResetRotationX = 5.0f;
        public static float ActiveAimResetRotationY = 50.0f;
        public static float ActiveAimResetRotationZ = -3.0f;

        public static float HighReadyAdditionalRotationSpeedMulti = 1.25f;
        public static float HighReadyResetRotationMulti = 3.5f;
        public static float HighReadyRotationMulti = 1.8f;
        public static float HighReadyResetSpeedMulti = 5.0f;
        public static float HighReadySpeedMulti = 6.0f;

        public static float HighReadyOffsetX = 0.005f;
        public static float HighReadyOffsetY = 0.04f;
        public static float HighReadyOffsetZ = -0.05f;

        public static float HighReadyRotationX = -10.0f;
        public static float HighReadyRotationY = 3.0f;
        public static float HighReadyRotationZ = 3.0f;

        public static float HighReadyAdditionalRotationX = -10.0f;
        public static float HighReadyAdditionalRotationY = 10f;
        public static float HighReadyAdditionalRotationZ = 5f;

        public static float HighReadyResetRotationX = 0.5f;
        public static float HighReadyResetRotationY = 2.0f;
        public static float HighReadyResetRotationZ = 1.0f;

        public static float LowReadyAdditionalRotationSpeedMulti = 0.5f;
        public static float LowReadyResetRotationMulti = 2.5f;
        public static float LowReadyRotationMulti = 2.0f;
        public static float LowReadySpeedMulti = 15f;
        public static float LowReadyResetSpeedMulti = 6f;

        public static float LowReadyOffsetX = -0.01f;
        public static float LowReadyOffsetY = -0.01f;
        public static float LowReadyOffsetZ = 0.0f;

        public static float LowReadyRotationX = 8f;
        public static float LowReadyRotationY = -5.0f;
        public static float LowReadyRotationZ = -1.0f;

        public static float LowReadyAdditionalRotationX = 12.0f;
        public static float LowReadyAdditionalRotationY = -50.0f;
        public static float LowReadyAdditionalRotationZ = 0.5f;

        public static float LowReadyResetRotationX = -2.0f;
        public static float LowReadyResetRotationY = 2.0f;
        public static float LowReadyResetRotationZ = -0.5f;

        public static float PistolAdditionalRotationSpeedMulti = 1f;
        public static float PistolResetRotationSpeedMulti = 5f;
        public static float PistolRotationSpeedMulti = 1f;
        public static float PistolPosSpeedMulti = 10.0f;
        public static float PistolPosResetSpeedMulti = 10.0f;

        public static float PistolOffsetX = 0.025f;
        public static float PistolOffsetY = 0.05f;
        public static float PistolOffsetZ = -0.035f;

        public static float PistolRotationX = 0.0f;
        public static float PistolRotationY = -15f;
        public static float PistolRotationZ = 0.0f;

        public static float PistolAdditionalRotationX = -2.0f;
        public static float PistolAdditionalRotationY = -15.0f;
        public static float PistolAdditionalRotationZ = 1.0f;

        public static float PistolResetRotationX = 1.5f;
        public static float PistolResetRotationY = 2.0f;
        public static float PistolResetRotationZ = 1.2f;

        public static float ShortStockAdditionalRotationSpeedMulti = 2.0f;
        public static float ShortStockResetRotationSpeedMulti = 2.0f;
        public static float ShortStockRotationMulti = 2.0f;
        public static float ShortStockSpeedMulti = 5.0f;
        public static float ShortStockResetSpeedMulti = 5.0f;

        public static float ShortStockOffsetX = 0.02f;
        public static float ShortStockOffsetY = 0.1f;
        public static float ShortStockOffsetZ = -0.025f;

        public static float ShortStockRotationX = 0f;
        public static float ShortStockRotationY = -15.0f;
        public static float ShortStockRotationZ = 0.0f;

        public static float ShortStockAdditionalRotationX = -5.0f;
        public static float ShortStockAdditionalRotationY = -20.0f;
        public static float ShortStockAdditionalRotationZ = 5.0f;

        public static float ShortStockResetRotationX = -5.0f;
        public static float ShortStockResetRotationY = 12.0f;
        public static float ShortStockResetRotationZ = 1.0f;

        public static bool EnableArmorHitZones = true;
        public static bool EnableBodyHitZones = true;
        public static bool EnablePlayerArmorZones = true;
        public static bool EnableArmPen = true;
        public static bool EnableHitSounds = true;
        public static float CloseHitSoundMulti = 1.0f;
        public static float FarHitSoundMulti = 1.0f;
        public static bool EnableRagdollFix = true;

        public static bool EnableProgramK = false;

        public static bool EnableMalfPatch = true;
        public static bool InspectionlessMalfs = true;
        public static float DuraMalfThreshold = 98f;
        public static bool IncreaseCOI = true;

        public static float RealTimeGain = 13f;
        public static float GainCutoff = 0.75f;
        public static float DeafRate = 0.023f;
        public static float DeafReset = 0.033f;
        public static float VigRate = 0.02f;
        public static float VigReset = 0.02f;
        public static float DistRate = 0.16f;
        public static float DistReset = 0.25f;

        public static bool EnableMedicalOvehaul = true;

        public static bool EnableIdleStamDrain = false;

        public static float GlobalAimSpeedModifier = 1.0f;
        public static float GlobalReloadSpeedMulti = 1.0f;
        public static float GlobalFixSpeedMulti = 1.0f;
        public static float GlobalUBGLReloadMulti = 1.35f;
        public static float RechamberPistolSpeedMulti = 1.0f;
        public static float GlobalRechamberSpeedMulti = 1.0f;
        public static float GlobalBoltSpeedMulti = 1.0f;
        public static float GlobalShotgunRackSpeedFactor = 1.0f;
        public static float GlobalCheckChamberSpeedMulti = 1.0f;
        public static float GlobalCheckChamberShotgunSpeedMulti = 1f;
        public static float GlobalCheckChamberPistolSpeedMulti = 1f;
        public static float GlobalCheckAmmoPistolSpeedMulti = 1.0f;
        public static float GlobalCheckAmmoMulti = 1.0f;
        public static float GlobalArmHammerSpeedMulti = 1f;
        public static float QuickReloadSpeedMulti = 1.4f;
        public static float InternalMagReloadMulti = 1.0f;

        public static KeyboardShortcut DecGain = KeyboardShortcut.Empty;
        public static KeyboardShortcut IncGain = KeyboardShortcut.Empty;

        public static bool enableSGMastering = false;
        public static float SwayIntensity = 1.5f;

        public static bool EnableAmmoFirerateDisp = true;
        public static bool EnableStatsDelta = true;
        public static bool ShowBalance = true;
        public static bool ShowCamRecoil = true;
        public static bool ShowDispersion = true;
        public static bool ShowRecoilAngle = true;
        public static bool ShowSemiROF = false;
        public static bool enableAmmoDamageDisp = true;
        public static bool enableAmmoFragDisp = true;
        public static bool enableAmmoPenDisp = true;
        public static bool enableAmmoArmorDamageDisp = true;
        public static bool enableAmmoRicochetChanceDisp = true;
        public static bool enableAmmoProjectileStatsDisp = true;
        //

        private void GetPaths()
        {
            var mod = RequestHandler.GetJson($"/RealismMod/GetInfo");
            ModPath = Json.Deserialize<string>(mod);
            ConfigFilePath = Path.Combine(ModPath, @"config\config.json");
        }

        private void ConfigCheck()
        {
            ConfigJson = File.ReadAllText(ConfigFilePath);
            ModConfig = JsonConvert.DeserializeObject<ConfigTemplate>(ConfigJson);
        }

        private void CacheIcons()
        {
            IconCache.Add(ENewItemAttributeId.ShotDispersion, Resources.Load<Sprite>("characteristics/icons/Velocity"));
            IconCache.Add(ENewItemAttributeId.BluntThroughput, Resources.Load<Sprite>("characteristics/icons/armorMaterial"));
            IconCache.Add(ENewItemAttributeId.VerticalRecoil, Resources.Load<Sprite>("characteristics/icons/Ergonomics"));
            IconCache.Add(ENewItemAttributeId.HorizontalRecoil, Resources.Load<Sprite>("characteristics/icons/Recoil Back"));
            IconCache.Add(ENewItemAttributeId.Dispersion, Resources.Load<Sprite>("characteristics/icons/Velocity"));
            IconCache.Add(ENewItemAttributeId.CameraRecoil, Resources.Load<Sprite>("characteristics/icons/SightingRange"));
            IconCache.Add(ENewItemAttributeId.AutoROF, Resources.Load<Sprite>("characteristics/icons/bFirerate"));
            IconCache.Add(ENewItemAttributeId.SemiROF, Resources.Load<Sprite>("characteristics/icons/bFirerate"));
            IconCache.Add(ENewItemAttributeId.ReloadSpeed, Resources.Load<Sprite>("characteristics/icons/weapFireType"));
            IconCache.Add(ENewItemAttributeId.FixSpeed, Resources.Load<Sprite>("characteristics/icons/icon_info_raidmoddable"));
            IconCache.Add(ENewItemAttributeId.ChamberSpeed, Resources.Load<Sprite>("characteristics/icons/icon_info_raidmoddable"));
            IconCache.Add(ENewItemAttributeId.AimSpeed, Resources.Load<Sprite>("characteristics/icons/SightingRange"));
            IconCache.Add(ENewItemAttributeId.Firerate, Resources.Load<Sprite>("characteristics/icons/bFirerate"));
            IconCache.Add(ENewItemAttributeId.MalfunctionChance, Resources.Load<Sprite>("characteristics/icons/icon_info_raidmoddable"));
            IconCache.Add(ENewItemAttributeId.CanSpall, Resources.Load<Sprite>("characteristics/icons/icon_info_bulletspeed"));
            IconCache.Add(ENewItemAttributeId.SpallReduction, Resources.Load<Sprite>("characteristics/icons/Velocity"));
            IconCache.Add(ENewItemAttributeId.GearReloadSpeed, Resources.Load<Sprite>("characteristics/icons/weapFireType"));
            IconCache.Add(ENewItemAttributeId.CanAds, Resources.Load<Sprite>("characteristics/icons/SightingRange"));
            IconCache.Add(ENewItemAttributeId.NoiseReduction, Resources.Load<Sprite>("characteristics/icons/icon_info_loudness"));
            IconCache.Add(ENewItemAttributeId.ProjectileCount, Resources.Load<Sprite>("characteristics/icons/icon_info_bulletspeed"));
            IconCache.Add(ENewItemAttributeId.ProjectileDamage, Resources.Load<Sprite>("characteristics/icons/icon_info_bulletspeed"));
            IconCache.Add(ENewItemAttributeId.Convergence, Resources.Load<Sprite>("characteristics/icons/Ergonomics"));
            IconCache.Add(ENewItemAttributeId.HBleedType, Resources.Load<Sprite>("characteristics/icons/icon_info_bloodloss"));
            IconCache.Add(ENewItemAttributeId.LimbHpPerTick, Resources.Load<Sprite>("characteristics/icons/icon_info_bloodloss"));
            IconCache.Add(ENewItemAttributeId.HpPerTick, Resources.Load<Sprite>("characteristics/icons/icon_info_bloodloss"));
            IconCache.Add(ENewItemAttributeId.RemoveTrnqt, Resources.Load<Sprite>("characteristics/icons/hpResource"));
            
            IconCache.Add(ENewItemAttributeId.Damage, Resources.Load<Sprite>("characteristics/icons/icon_info_damage"));
            IconCache.Add(ENewItemAttributeId.BuckshotDamage, Resources.Load<Sprite>("characteristics/icons/icon_info_damage"));
            IconCache.Add(ENewItemAttributeId.FragmentationChance, Resources.Load<Sprite>("characteristics/icons/icon_info_shrapnelcount"));
            IconCache.Add(EItemAttributeId.LightBleedingDelta, Resources.Load<Sprite>("characteristics/icons/icon_info_bloodloss"));
            IconCache.Add(EItemAttributeId.HeavyBleedingDelta, Resources.Load<Sprite>("characteristics/icon_info_hydration"));
            IconCache.Add(ENewItemAttributeId.Penetration, Resources.Load<Sprite>("characteristics/icon_info_penetration"));
            _ = LoadTexture(ENewItemAttributeId.ArmorDamage, Path.Combine(ModPath, "res\\armorDamage.png"));
            _ = LoadTexture(ENewItemAttributeId.RicochetChance, Path.Combine(ModPath, "res\\ricochet.png"));

            _ = LoadTexture(ENewItemAttributeId.Balance, Path.Combine(ModPath, "res\\balance.png"));
            _ = LoadTexture(ENewItemAttributeId.RecoilAngle, Path.Combine(ModPath, "res\\recoilAngle.png"));
        }

        private async Task LoadTexture(Enum id, string path)
        {
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(path))
            {
                uwr.SendWebRequest();

                while (!uwr.isDone)
                    await Task.Delay(5);

                if (uwr.responseCode != 200)
                {
                    Logger.LogError("Realism: Error Requesting Textures");
                }
                else
                {
                    Texture2D cachedTexture = DownloadHandlerTexture.GetContent(uwr);
                    IconCache.Add(id, Sprite.Create(cachedTexture, new Rect(0, 0, cachedTexture.width, cachedTexture.height), new Vector2(0, 0)));
                }
            }
        }

        public static Dictionary<string, AudioClip> LoadedAudioClips = new Dictionary<string, AudioClip>();

        async static void LoadAudioClip(string path)
        {
            LoadedAudioClips[Path.GetFileName(path)] = await RequestAudioClip(path);
        }

        async static Task<AudioClip> RequestAudioClip(string path)
        {
            string extension = Path.GetExtension(path);
            AudioType audioType = AudioType.WAV;
            switch (extension) 
            {
                case ".wav":
                    audioType = AudioType.WAV;
                    break;
                case ".ogg":
                    audioType = AudioType.OGGVORBIS;
                    break;
            }
            UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, audioType);
            var SendWeb = www.SendWebRequest();

            while (!SendWeb.isDone)
                await Task.Yield();

            if (www.isNetworkError || www.isHttpError)
            {
                return null;
            }
            else
            {
                AudioClip audioclip = DownloadHandlerAudioClip.GetContent(www);
                return audioclip;
            }
        }

        void Awake()
        {
            try
            {
                GetPaths();
                ConfigCheck();
                CacheIcons();

                string[] AudioFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "/BepInEx/plugins/Realism/sounds/");
                foreach (string File in AudioFiles)
                {
                    LoadAudioClip(File);
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception);
            }

            InitConfigs();

            if (Plugin.EnableProgramK == true)
            {
                Utils.ProgramKEnabled = true;
                Logger.LogInfo("Realism Mod: ProgramK Compatibiltiy Enabled!");
            }

            if (ModConfig.recoil_attachment_overhaul)
            {
                //Stat assignment patches
                new COIDeltaPatch().Enable();
                new TotalShotgunDispersionPatch().Enable();
                new GetDurabilityLossOnShotPatch().Enable();
                new AutoFireRatePatch().Enable();
                new SingleFireRatePatch().Enable();
                new ErgoDeltaPatch().Enable();
                new ErgoWeightPatch().Enable();
                new method_9Patch().Enable();

                new SyncWithCharacterSkillsPatch().Enable();
                new UpdateWeaponVariablesPatch().Enable();
                new SetAimingSlowdownPatch().Enable();

                //Sway and Aim Inertia
                new method_20Patch().Enable();
                new UpdateSwayFactorsPatch().Enable();
                new GetOverweightPatch().Enable();
                new SetOverweightPatch().Enable();

                //Recoil Patches
                new OnWeaponParametersChangedPatch().Enable();
                new ProcessPatch().Enable();
                new ShootPatch().Enable();
                new SetCurveParametersPatch().Enable();

                //Sensitivity Patches
                new AimingSensitivityPatch().Enable();
                new UpdateSensitivityPatch().Enable();
                if (Plugin.EnableHipfireRecoilClimb.Value)
                {
                    new GetRotationMultiplierPatch().Enable();
                }

                //Aiming Patches
                new SetAimingPatch().Enable();
                new ToggleAimPatch().Enable();

                //Malf Patches
                if (Plugin.EnableMalfPatch & ModConfig.malf_changes)
                {
                    new GetTotalMalfunctionChancePatch().Enable();
                }
                if (Plugin.InspectionlessMalfs)
                {
                    new IsKnownMalfTypePatch().Enable();
                }

                //Reload Patches
                if (Plugin.EnableReloadPatches.Value)
                {
                    new CanStartReloadPatch().Enable();
                    new ReloadMagPatch().Enable();
                    new QuickReloadMagPatch().Enable();
                    new ReloadWithAmmoPatch().Enable();
                    new ReloadBarrelsPatch().Enable();
                    new ReloadRevolverDrumPatch().Enable();

                    new OnMagInsertedPatch().Enable();
                    new SetMagTypeCurrentPatch().Enable();
                    new SetMagTypeNewPatch().Enable();
                    new SetMagInWeaponPatch().Enable();

                    new RechamberSpeedPatch().Enable();
                    new SetMalfRepairSpeedPatch().Enable();
                    new SetBoltActionReloadPatch().Enable();

                    new CheckChamberPatch().Enable();
                    new SetSpeedParametersPatch().Enable();
                    new CheckAmmoPatch().Enable();
                    new SetHammerArmedPatch().Enable();
                    new SetLauncherPatch().Enable();
                }

                new CheckAmmoFirearmControllerPatch().Enable();
                new CheckChamberFirearmControllerPatch().Enable();

                new SetAnimatorAndProceduralValuesPatch().Enable();
                new OnItemAddedOrRemovedPatch().Enable();

                if (Plugin.enableSGMastering == true)
                {
                    new SetWeaponLevelPatch().Enable();
                }

                //Stat Display Patches
                new ModConstructorPatch().Enable();
                new WeaponConstructorPatch().Enable();
                new HRecoilDisplayStringValuePatch().Enable();
                new HRecoilDisplayDeltaPatch().Enable();
                new VRecoilDisplayStringValuePatch().Enable();
                new VRecoilDisplayDeltaPatch().Enable();
                new ModVRecoilStatDisplayPatchFloat().Enable();
                new ModVRecoilStatDisplayPatchString().Enable();
                new ErgoDisplayDeltaPatch().Enable();
                new ErgoDisplayStringValuePatch().Enable();
                new COIDisplayDeltaPatch().Enable();
                new COIDisplayStringValuePatch().Enable();
                new FireRateDisplayStringValuePatch().Enable();
                new GetCachedReadonlyQualitiesPatch().Enable();
                new CenterOfImpactMOAPatch().Enable();
                new ModErgoStatDisplayPatch().Enable();
                new GetAttributeIconPatches().Enable();
                new HeadsetConstructorPatch().Enable();
                new AmmoDuraBurnDisplayPatch().Enable();
                new AmmoMalfChanceDisplayPatch().Enable();
                new MagazineMalfChanceDisplayPatch().Enable();
                new BarrelModClassPatch().Enable();

                if (Plugin.IncreaseCOI == true)
                {
                    new GetTotalCenterOfImpactPatch().Enable();
                }
            }

            //Ballistics
            if (ModConfig.realistic_ballistics == true)
            {
                new CreateShotPatch().Enable();
                new ApplyDamagePatch().Enable();
                new DamageInfoPatch().Enable();
                new ApplyDamageInfoPatch().Enable();
                new SetPenetrationStatusPatch().Enable();

                if (EnableRagdollFix)
                {
                    new ApplyCorpseImpulsePatch().Enable();
                    /*  new RagdollPatch().Enable();*/
                }

                //Armor Class
                if (Plugin.EnableRealArmorClass.Value == true)
                {
                    new ArmorClassDisplayPatch().Enable();
                }

                if (Plugin.EnableArmorHitZones)
                {
                    new ArmorZoneBaseDisplayPatch().Enable();
                    new ArmorZoneSringValueDisplayPatch().Enable();
                }

                new IsShotDeflectedByHeavyArmorPatch().Enable();

                if (Plugin.EnableArmPen)
                {
                    new IsPenetratedPatch().Enable();
                }

                //Shot Effects
                if (Plugin.EnableDeafen.Value == true)
                {
                    new PrismEffectsPatch().Enable();
                    new VignettePatch().Enable();
                    new UpdatePhonesPatch().Enable();
                    new SetCompressorPatch().Enable();
                    new RegisterShotPatch().Enable();
                    new ExplosionPatch().Enable();
                    new GrenadeClassContusionPatch().Enable();
                }
            }

            new ArmorComponentPatch().Enable();
            new RigConstructorPatch().Enable();

            //Player
            new PlayerInitPatch().Enable();
            new ToggleHoldingBreathPatch().Enable();

            //Movement
            if (EnableMaterialSpeed.Value) 
            {
                new CalculateSurfacePatch().Enable();
            }
            if (EnableMaterialSpeed.Value)
            {
                new CalculateSurfacePatch().Enable();
                new ClampSpeedPatch().Enable();
            }
            new SprintAccelerationPatch().Enable();
            new EnduranceSprintActionPatch().Enable();
            new EnduranceMovementActionPatch().Enable();

            //LateUpdate
            new PlayerLateUpdatePatch().Enable();

            //Stances
            new ApplyComplexRotationPatch().Enable();
            new ApplySimpleRotationPatch().Enable();
            new InitTransformsPatch().Enable();
            new ZeroAdjustmentsPatch().Enable();
            new WeaponOverlappingPatch().Enable();
            new WeaponLengthPatch().Enable();
            new WeaponOverlappingPatch().Enable();
            new OnWeaponDrawPatch().Enable();
            new UpdateHipInaccuracyPatch().Enable();
            new SetFireModePatch().Enable();

            //Health
            if (EnableMedicalOvehaul)
            {
                new ApplyItemPatch().Enable();
                new SetQuickSlotPatch().Enable();
                new ProceedPatch().Enable();
                new RemoveEffectPatch().Enable();
                new StamRegenRatePatch().Enable();
                new HCApplyDamagePatch().Enable();
                new RestoreBodyPartPatch().Enable();
            }
        }

        void Update()
        {
            if (Utils.CheckIsReady())
            {
                if (ModConfig.recoil_attachment_overhaul) 
                {
                    if (Plugin.ShotCount > Plugin.PrevShotCount)
                    {
                        Plugin.IsFiring = true;
                        StanceController.IsFiringFromStance = true;

                        if (Plugin.EnableRecoilClimb.Value == true && (Plugin.IsAiming == true || Plugin.EnableHipfireRecoilClimb.Value == true))
                        {
                            RecoilController.DoRecoilClimb();
                        }

                        Plugin.PrevShotCount = Plugin.ShotCount;
                    }

                    if (Plugin.ShotCount == Plugin.PrevShotCount)
                    {
                        Plugin.Timer += Time.deltaTime;

                        if (Plugin.Timer >= Plugin.resetTime.Value)
                        {
                            Plugin.IsFiring = false;
                            Plugin.ShotCount = 0;
                            Plugin.PrevShotCount = 0;
                            Plugin.Timer = 0f;
                        }

                        StanceController.StanceShotTimer();
                    }

                    if (Plugin.EnableDeafen.Value)
                    {
                        if (Input.GetKeyDown(Plugin.IncGain.MainKey) && Plugin.HasHeadSet)
                        {
             
                            if (Plugin.RealTimeGain < 20)
                            {
                                Plugin.RealTimeGain += 1f;
                                Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0,0,0), Plugin.LoadedAudioClips["beep.wav"], 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, 1.0f, EOcclusionTest.None, null, false);
                            }
                        }
                        if (Input.GetKeyDown(Plugin.DecGain.MainKey) && Plugin.HasHeadSet)
                        {
                
                            if (Plugin.RealTimeGain > 0)
                            {
                                Plugin.RealTimeGain -= 1f;
                                Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), Plugin.LoadedAudioClips["beep.wav"], 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, 1.0f, EOcclusionTest.None, null, false);
                            }
                        }

                        if (PrismEffects != null) 
                        {
                            Deafening.DoDeafening();
                        }
            

                        if (Plugin.IsBotFiring)
                        {
                            Plugin.BotTimer += Time.deltaTime;
                            if (Plugin.BotTimer >= 0.5f)
                            {
                                Plugin.IsBotFiring = false;
                                Plugin.BotTimer = 0f;
                            }
                        }

                        if (Plugin.GrenadeExploded)
                        {
                            Plugin.GrenadeTimer += Time.deltaTime;
                            if (Plugin.GrenadeTimer >= 0.7f)
                            {
                                Plugin.GrenadeExploded = false;
                                Plugin.GrenadeTimer = 0f;
                            }
                        }
                    }

                    if (!Plugin.IsFiring)
                    {
                        RecoilController.ResetRecoil();
                    }
                }
 
                StanceController.StanceState();

                if (Plugin.EnableMedicalOvehaul)
                {
                    healthControllerTick += Time.deltaTime;
                    RealismHealthController.HealthController(Logger);
                }
            }
        }

        public void InitConfigs()
        {
            string testing = ".0. Тестирование";
            string miscSettings = ".1. Прочие настройки";
            string ballSettings = ".2. Баллистика";
            string recoilSettings = ".3. Настройки Отдачи";
            string advancedRecoilSettings = ".4. Расширенные настройки Отдачи";
            string healthSettings = ".6. Здоровье";
            string moveSettings = ".7. Передвижение";
            string deafSettings = ".8. Аудио";
            string speed = "9. Скорость работы с Оружием";
            string weapAimAndPos = "10. Положения и Позиции";

            EnableLogging = Config.Bind<bool>(testing, "Включить общее логгирование", false, new ConfigDescription("Включает логгирование для Дебага и разработки", null, new ConfigurationManagerAttributes { Order = 2, IsAdvanced = true }));
            EnableBallisticsLogging = Config.Bind<bool>(testing, "Включить логгирование Баллистики", false, new ConfigDescription("Включает логгирование для Дебага и разработки", null, new ConfigurationManagerAttributes { Order = 1, IsAdvanced = true }));

            EnableHoldBreath = Config.Bind<bool>(miscSettings, "Вернуть задержку дыхания", false, new ConfigDescription("Возвращает механику задержки дыхания. Этот мод сбалансирован для использования без возможности задержки дыхания.", null, new ConfigurationManagerAttributes { Order = 4 }));
            EnableFSPatch = Config.Bind<bool>(miscSettings, "Включить блокирование прицеливания (Забрало)", true, new ConfigDescription("Блокирует возможность прицеливания, если опущено забрало. Складывание приклада или поднятие Забрала поможет исправить этот недуг.", null, new ConfigurationManagerAttributes { Order = 3 }));
            EnableNVGPatch = Config.Bind<bool>(miscSettings, "Включить блокирование прицеливания (ПНВ)", true, new ConfigDescription("Блокирует возможность прицеливания через оптическую оптику при надетом ПНВ. Поднятие ПНВ поможет исправить этот недуг", null, new ConfigurationManagerAttributes { Order = 2 }));
            GearBlocksEat = Config.Bind<bool>(miscSettings, "Включить блокирование употребления (Забрало и ПНВ)", true, new ConfigDescription("Блокирует возможность употребление еды/воды/таблеток при опущеном Забрале или надетом ПНВ. Поднятия Забрала поможет исправить этот недуг", null, new ConfigurationManagerAttributes { Order = 1 }));

            EnableRealArmorClass = Config.Bind<bool>(ballSettings, "Отображать настоящие классы защиты", false, new ConfigDescription("Требуется перезагрузка. Заместо отображения класса брони в виде чисел, использует настоящую классификацию бронезащиты.", null, new ConfigurationManagerAttributes { Order = 1 }));

            EnableHipfireRecoilClimb = Config.Bind<bool>(recoilSettings, "Enable Hipfire Recoil Climb", true, new ConfigDescription("Requires Restart. Enabled Recoil Climbing While Hipfiring", null, new ConfigurationManagerAttributes { Order = 8 }));
            ReduceCamRecoil = Config.Bind<bool>(recoilSettings, "Reduce Camera Recoil", false, new ConfigDescription("Reduces Camera Recoil Per Shot. If Disabled, Camera Recoil Becomes More Intense As Weapon Recoil Increases.", null, new ConfigurationManagerAttributes { Order = 9 }));
            SensLimit = Config.Bind<float>(recoilSettings, "Sensitivity Lower Limit", 0.4f, new ConfigDescription("Sensitivity Lower Limit While Firing. Lower Means More Sensitivity Reduction. 100% Means No Sensitivity Reduction.", new AcceptableValueRange<float>(0.1f, 1f), new ConfigurationManagerAttributes { Order = 10 }));
            RecoilIntensity = Config.Bind<float>(recoilSettings, "Recoil Multi", 1.15f, new ConfigDescription("Changes The Overall Intenisty Of Recoil. This Will Increase/Decrease Horizontal Recoil, Dispersion, Vertical Recoil.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 11 }));

            ConvSemiMulti = Config.Bind<float>(advancedRecoilSettings, "Semi Auto Convergence Multi", 0.69f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 30, IsAdvanced = true }));
            ConvAutoMulti = Config.Bind<float>(advancedRecoilSettings, "Auto Convergence Multi", 0.59f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 20, IsAdvanced = true }));
            EnableRecoilClimb = Config.Bind<bool>(advancedRecoilSettings, "Enable Recoil Climb", true, new ConfigDescription("The Core Of The Recoil Overhaul. Recoil Increase Per Shot, Nullifying Recoil Auto-Compensation In Full Auto And Requiring A Constant Pull Of The Mouse To Control Recoil. If Diabled Weapons Will Be Completely Unbalanced Without Stat Changes.", null, new ConfigurationManagerAttributes { Order = 12 }));
            SensChangeRate = Config.Bind<float>(advancedRecoilSettings, "Sensitivity Change Rate", 0.75f, new ConfigDescription("Rate At Which Sensitivity Is Reduced While Firing. Lower Means Faster Rate.", new AcceptableValueRange<float>(0.1f, 1f), new ConfigurationManagerAttributes { Order = 13 }));
            SensResetRate = Config.Bind<float>(advancedRecoilSettings, "Senisitivity Reset Rate", 1.2f, new ConfigDescription("Rate At Which Sensitivity Recovers After Firing. Higher Means Faster Rate.", new AcceptableValueRange<float>(1.01f, 2f), new ConfigurationManagerAttributes { Order = 14, IsAdvanced = true }));
            ConvergenceSpeedCurve = Config.Bind<float>(advancedRecoilSettings, "Convergence Multi", 0.9f, new ConfigDescription("The Convergence Curve. Lower Means More Recoil/Less Convergence.", new AcceptableValueRange<float>(0.01f, 1.5f), new ConfigurationManagerAttributes { Order = 15 }));
            vRecoilLimit = Config.Bind<float>(advancedRecoilSettings, "Vertical Recoil Upper Limit", 15.0f, new ConfigDescription("The Upper Limit For Vertical Recoil Increase As A Multiplier. E.g Value Of 10 Is A Limit Of 10x Starting Recoil.", new AcceptableValueRange<float>(1f, 50f), new ConfigurationManagerAttributes { Order = 16 }));
            vRecoilChangeMulti = Config.Bind<float>(advancedRecoilSettings, "Vertical Recoil Change Rate Multi", 1.01f, new ConfigDescription("A Multiplier For The Verftical Recoil Increase Per Shot.", new AcceptableValueRange<float>(0.9f, 1.1f), new ConfigurationManagerAttributes { Order = 17 }));
            vRecoilResetRate = Config.Bind<float>(advancedRecoilSettings, "Vertical Recoil Reset Rate", 0.91f, new ConfigDescription("The Rate At Which Vertical Recoil Resets Over Time After Firing. Lower Means Faster Rate.", new AcceptableValueRange<float>(0.1f, 0.99f), new ConfigurationManagerAttributes { Order = 18, IsAdvanced = true }));
            hRecoilLimit = Config.Bind<float>(advancedRecoilSettings, "Rearward Recoil Upper Limit", 2.5f, new ConfigDescription("The Upper Limit For Rearward Recoil Increase As A Multiplier. E.g Value Of 10 Is A Limit Of 10x Starting Recoil.", new AcceptableValueRange<float>(1f, 50f), new ConfigurationManagerAttributes { Order = 18 }));
            hRecoilChangeMulti = Config.Bind<float>(advancedRecoilSettings, "Rearward Recoil Change Rate Multi", 1.0f, new ConfigDescription("A Multiplier For The Rearward Recoil Increase Per Shot.", new AcceptableValueRange<float>(0.9f, 1.1f), new ConfigurationManagerAttributes { Order = 19 }));
            hRecoilResetRate = Config.Bind<float>(advancedRecoilSettings, "Rearward Recoil Reset Rate", 0.91f, new ConfigDescription("The Rate At Which Rearward Recoil Resets Over Time After Firing. Lower Means Faster Rate.", new AcceptableValueRange<float>(0.1f, 0.99f), new ConfigurationManagerAttributes { Order = 20, IsAdvanced = true }));
            ConvergenceResetRate = Config.Bind<float>(advancedRecoilSettings, "Convergence Reset Rate", 1.16f, new ConfigDescription("The Rate At Which Convergence Resets Over Time After Firing. Higher Means Faster Rate.", new AcceptableValueRange<float>(1.01f, 2f), new ConfigurationManagerAttributes { Order = 21, IsAdvanced = true }));
            convergenceLimit = Config.Bind<float>(advancedRecoilSettings, "Convergence Lower Limit", 0.3f, new ConfigDescription("The Lower Limit For Convergence. Convergence Is Kept In Proportion With Vertical Recoil While Firing, Down To The Set Limit. Value Of 0.3 Means Convegence Lower Limit Of 0.3 * Starting Convergance.", new AcceptableValueRange<float>(0.1f, 1.0f), new ConfigurationManagerAttributes { Order = 22, IsAdvanced = true }));
            resetTime = Config.Bind<float>(advancedRecoilSettings, "Time Before Reset", 0.14f, new ConfigDescription("The Time In Seconds That Has To Be Elapsed Before Firing Is Considered Over, Stats Will Not Reset Until This Timer Is Done. Helps Prevent Spam Fire In Full Auto.", new AcceptableValueRange<float>(0.1f, 0.5f), new ConfigurationManagerAttributes { Order = 23, IsAdvanced = true }));

            HealthEffects = Config.Bind<bool>(healthSettings, "Пассивные эффекты от здоровья персонажа", true, new ConfigDescription("Остаток ХП у каждой части тела, общий остаток ХП, остаток гидратации и энергии влияют на скорость большинства действий игрока, движения и регенерацию выносливости в зависимости от части тела. Остаток ХП влияет на скорость потери гидратации и энергии.", null, new ConfigurationManagerAttributes { Order = 1 }));

            EnableMaterialSpeed = Config.Bind<bool>(moveSettings, "Включить модификатор скорости передвижения (поверхность)", true, new ConfigDescription("Включает влияние на скорость передвижения персонажа, в зависимости от материала поверхности (бетон, трава, металл, стекло и т.д.).", null, new ConfigurationManagerAttributes { Order = 1 }));
            EnableSlopeSpeed = Config.Bind<bool>(moveSettings, "Включить модификатор скорости передвижения (рельеф)", true, new ConfigDescription("Включает влияние на скрость передвижения персонажа, в зависимости от рельефа местности (подъем в гору, спуск с горы и т.д.)", null, new ConfigurationManagerAttributes { Order = 2 }));

            EnableDeafen = Config.Bind<bool>(deafSettings, "Включить оглушение", true, new ConfigDescription("Требуется перезагрузка. Включает оглушение игрока от выстрелов и взрывов.", null, new ConfigurationManagerAttributes { Order = 1 }));

            EnableReloadPatches = Config.Bind<bool>(speed, "Включить изменения в скорости работы с оружием", true, new ConfigDescription("Требуется перезагрузка. Вес оружия, Вес магазина, Различные обвесы на оружие, Параметр Баланса, Эргономики и Наличие выбитой руки влияют на множитель скорости работы с оружием.", null, new ConfigurationManagerAttributes { Order = 1 }));

            EnableTacSprint = Config.Bind<bool>(weapAimAndPos, "Включить тактический бег", true, new ConfigDescription("При беге персонаж будет поднимать оружие вверх. Также даёт даёт +15% прибавку к скорости и +30% к мувменту, если включен функционал <<Влияния стоек>>.", null, new ConfigurationManagerAttributes { Order = 11 }));
            EnableAltPistol = Config.Bind<bool>(weapAimAndPos, "Включить альтернативное положение пистолета", true, new ConfigDescription("Пистолет будет находится в центре экрана, как будто персонаж находится в защитной стойке.", null, new ConfigurationManagerAttributes { Order = 10 }));
            EnableStanceStamChanges = Config.Bind<bool>(weapAimAndPos, "Включить влияние стойки на передвижение", true, new ConfigDescription("Включает влияение стоек на передвижение персонажа и работу с выносливостью. Высокая + Низкая стойка, Компактная стойка и <<Альтернативное положение пистолета>> восстанавливают выносливость быстрее. Высокая стойка + Тактический бег повышают скорость передвижения на 30%, а маневренности на 60%, Низкая стойка повышает манёвренность на 15% во время бега.", null, new ConfigurationManagerAttributes { Order = 9 }));
            ToggleActiveAim = Config.Bind<bool>(weapAimAndPos, "Использовать режим нажатия для Угловой стойки", true, new ConfigDescription("Если стоит True, то вход и выход из Active Aim будет по нажатию ПКМ. В противном случае по зажатию ПКМ", null, new ConfigurationManagerAttributes { Order = 8 }));
            ActiveAimReload = Config.Bind<bool>(weapAimAndPos, "Разрешить перезарядку оружия не выходя из Угловой стойки", false, new ConfigDescription("Разрешает производить перезарядку оружия прямо во время Active Aim стойки с бонусом к скорости (может выглядеть немного странно и неряшливо).", null, new ConfigurationManagerAttributes { Order = 7 }));
            StanceToggleDevice = Config.Bind<bool>(weapAimAndPos, "Смена стойки выключает ЛЦУ/Фонарики", false, new ConfigDescription("При смене стойки на Высокую или Низкую, ЛЦУ и Фонарики будут выключать.", null, new ConfigurationManagerAttributes { Order = 6 }));

            CycleStancesKeybind = Config.Bind(weapAimAndPos, "Клавиша цикличной смены положения оружия", new KeyboardShortcut(KeyCode.None), new ConfigDescription("Цикличная смена положений по нажатию кнопки, между Высокой, Низкой и Компактной стойками. Двойное нажатие возвращает стандартное положение.", null, new ConfigurationManagerAttributes { Order = 5 }));
            ActiveAimKeybind = Config.Bind(weapAimAndPos, "Клавиша Угловой стойки", new KeyboardShortcut(KeyCode.None), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 4 }));
            HighReadyKeybind = Config.Bind(weapAimAndPos, "Клавиша Высокой стойки", new KeyboardShortcut(KeyCode.None), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 3 }));
            LowReadyKeybind = Config.Bind(weapAimAndPos, "Клавиша Низкой стойки", new KeyboardShortcut(KeyCode.None), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 2 }));
            ShortStockKeybind = Config.Bind(weapAimAndPos, "Клавига Компактной стойки", new KeyboardShortcut(KeyCode.None), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 1 }));

            //EnableMedicalOvehaul = Config.Bind<bool>(healthSettings, "Включить пассивную регенерацию ХП", true, new ConfigDescription("Включает пассивную регенерацию ХП, если вы потеряли здоровье от падения, от колючей проволки, либо от эффекта дегидрации", null, new ConfigurationManagerAttributes { Order = 100 }));
            //TrnqtEffect = Config.Bind<bool>(healthSettings, "Включить эффект от Жгута", true, new ConfigDescription("Жгут будет понижает очки здоровья конечность на которую он был наложен.", null, new ConfigurationManagerAttributes { Order = 90 }));
            //GearBlocksHeal = Config.Bind<bool>(healthSettings, "Экипировка блокирует лечение", true, new ConfigDescription("Экипировка будет блокировать возможность лечения, если она закрывает собой место ранения", null, new ConfigurationManagerAttributes { Order = 70 }));
            //DropGearKeybind = Config.Bind(healthSettings, "Клавиша сброса всей экипировки", new KeyboardShortcut(KeyCode.P), new ConfigDescription("Выбрасывает всю экипировку, которая мешает применить медикамент", null, new ConfigurationManagerAttributes { Order = 50 }));
        }
    }
}

