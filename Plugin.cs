using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Aki.Common.Http;
using Aki.Common.Utils;
using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Newtonsoft.Json;
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
        public static ConfigEntry<bool> EnableReloadPatches { get; set; }
		
        public static ConfigEntry<bool> EnableRealArmorClass { get; set; }
		
        public static ConfigEntry<bool> ReduceCamRecoil { get; set; }

        public static ConfigEntry<bool> EnableHoldBreath { get; set; }
		
        public static ConfigEntry<bool> EnableStatsDelta { get; set; }
		
        public static ConfigEntry<KeyboardShortcut> IncGain { get; set; }
        public static ConfigEntry<KeyboardShortcut> DecGain { get; set; }
		
        public static ConfigEntry<bool> ToggleActiveAim { get; set; }
		
        public static ConfigEntry<bool> ActiveAimReload { get; set; }
        public static ConfigEntry<bool> EnableAltPistol { get; set; }
		
        public static ConfigEntry<bool> EnableTacSprint { get; set; }
        public static ConfigEntry<bool> EnableSprintPenalty { get; set; }
		
        public static ConfigEntry<bool> EnableLogging { get; set; }
        public static ConfigEntry<bool> EnableBallisticsLogging { get; set; }

        public static Weapon CurrentlyShootingWeapon;

        public static Vector3 TransformBaseStartPosition;
        public static Vector3 WeaponOffsetPosition;

        public static bool DidWeaponSwap = false;
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

        public static bool UniformAimIsPresent = false;
        public static bool BridgeIsPresent = false;
        public static bool checkedForUniformAim = false;


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
        public static bool EnableMaterialSpeed = true;
        public static bool EnableSlopeSpeed = true;

        public static bool EnableMedicalOvehaul = true;
        public static bool GearBlocksEat = true;
        public static bool EnableAdrenaline = true;

        public static bool EnableStockSlots = true;
        public static bool EnableFSPatch = true;
        public static bool EnableNVGPatch = true;

        public static bool EnableArmorHitZones = true;
        public static bool EnableBodyHitZones = true;
        public static bool EnablePlayerArmorZones = true;
        public static bool EnableArmPen = true;
        public static bool EnableHitSounds = true;
        public static float CloseHitSoundMulti = 1.1f;
        public static float FarHitSoundMulti = 1.2f;
        public static bool EnableRagdollFix = true;
        public static float RagdollForceModifier = 1f;

        public static bool EnableHipfireRecoilClimb = true;
        public static float SensLimit = 0.5f;
        public static float RecoilIntensity = 1.2f;
        public static float VertRecAutoMulti = 0.6f;
        public static float VertRecSemiMulti = 1.35f;
        public static float HorzRecAutoMulti = 0.4f;
        public static float HorzRecSemiMulti = 1.15f;
        public static float HorzRecLimit = 8.85f;

        public static float ConvSemiMulti = 2f;
        public static float ConvAutoMulti = 1f;
        public static bool EnableRecoilClimb = true;
        public static float SensChangeRate = 0.8f;
        public static float SensResetRate = 1.2f;
        public static float ConvergenceSpeedCurve = 0.9f;
        public static float vRecoilLimit = 17.7f;
        public static float vRecoilChangeMulti = 1f;
        public static float vRecoilResetRate = 0.9f;
        public static float hRecoilLimit = 1.1f;
        public static float hRecoilChangeMulti = 1f;
        public static float hRecoilResetRate = 0.9f;
        public static float ConvergenceResetRate = 1.2f;
        public static float convergenceLimit = 0.3f;
        public static float resetTime = 0.15f;

        public static bool EnableAmmoFirerateDisp = true;
        //public static bool EnableStatsDelta = true;
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

        public static float SwayIntensity = 1.1f;
        public static bool EnableMalfPatch = true;
        public static bool InspectionlessMalfs = true;
        public static float DuraMalfThreshold = 98f;
        public static bool enableSGMastering = false;
        public static bool IncreaseCOI = true;

        public static float HeadsetAmbientMulti = 10f;
        public static float RealTimeGain = 13f;
        public static float GainCutoff = 0.75f;
        public static float DeafRate = 0.023f;
        public static float DeafReset = 0.033f;
        public static float VigRate = 0.02f;
        public static float VigReset = 0.02f;
        public static float DistRate = 0.16f;
        public static float DistReset = 0.25f;
        public static bool EnableDeafen = true;

        public static float GlobalAimSpeedModifier = 1.0f;
        public static float GlobalReloadSpeedMulti = 1.0f;
        public static float GlobalFixSpeedMulti = 1f;
        public static float GlobalUBGLReloadMulti = 1.35f;
        public static float RechamberPistolSpeedMulti = 1.0f;
        public static float GlobalRechamberSpeedMulti = 1.0f;
        public static float GlobalBoltSpeedMulti = 1.0f;
        public static float GlobalShotgunRackSpeedFactor = 1.0f;
        public static float GlobalCheckChamberSpeedMulti = 1.0f;
        public static float GlobalCheckChamberShotgunSpeedMulti = 1f;
        public static float GlobalCheckChamberPistolSpeedMulti = 1f;
        public static float GlobalCheckAmmoPistolSpeedMulti = 1.0f;
        public static float GlobalCheckAmmoMulti = 1.1f;
        public static float GlobalArmHammerSpeedMulti = 1f;
        public static float QuickReloadSpeedMulti = 1.4f;
        public static float InternalMagReloadMulti = 1.0f;

        public static bool EnableIdleStamDrain = false;
        public static bool EnableStanceStamChanges = true;
        public static bool StanceToggleDevice = false;

        public static KeyboardShortcut CycleStancesKeybind = KeyboardShortcut.Empty;
        public static KeyboardShortcut ActiveAimKeybind = KeyboardShortcut.Empty;
        public static KeyboardShortcut HighReadyKeybind = KeyboardShortcut.Empty;
        public static KeyboardShortcut LowReadyKeybind = KeyboardShortcut.Empty;
        public static KeyboardShortcut ShortStockKeybind = KeyboardShortcut.Empty;

        public static float WeapOffsetX = 0.0f;
        public static float WeapOffsetY = 0.0f;
        public static float WeapOffsetZ = 0.0f;

        public static float StanceTransitionSpeed = 5.0f;
        public static float ThirdPersonRotationSpeed = 1.5f;
        public static float ThirdPersonPositionSpeed = 2.0f;

        public static float ActiveAimAdditionalRotationSpeedMulti = 1.0f;
        public static float ActiveAimResetRotationSpeedMulti = 3.0f;
        public static float ActiveAimRotationMulti = 1.0f;
        public static float ActiveAimSpeedMulti = 12.0f;
        public static float ActiveAimResetSpeedMulti = 9.6f;

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
        public static float HighReadyResetSpeedMulti = 6.0f;
        public static float HighReadySpeedMulti = 7.2f;

        public static float HighReadyOffsetX = 0.005f;
        public static float HighReadyOffsetY = 0.04f;
        public static float HighReadyOffsetZ = -0.05f;

        public static float HighReadyRotationX = -10.0f;
        public static float HighReadyRotationY = 3.0f;
        public static float HighReadyRotationZ = 3.0f;

        public static float HighReadyAdditionalRotationX = -10.0f;
        public static float HighReadyAdditionalRotationY = 10f;
        public static float HighReadyAdditionalRotationZ = 2.5f;

        public static float HighReadyResetRotationX = 0.5f;
        public static float HighReadyResetRotationY = 2.0f;
        public static float HighReadyResetRotationZ = 1.0f;

        public static float LowReadyAdditionalRotationSpeedMulti = 0.5f;
        public static float LowReadyResetRotationMulti = 2.5f;
        public static float LowReadyRotationMulti = 2.0f;
        public static float LowReadySpeedMulti = 18f;
        public static float LowReadyResetSpeedMulti = 7.2f;

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
        public static float PistolPosResetSpeedMulti = 7.0f;

        public static float PistolOffsetX = 0.015f;
        public static float PistolOffsetY = 0.02f;
        public static float PistolOffsetZ = -0.04f;

        public static float PistolRotationX = 0.0f;
        public static float PistolRotationY = -30f;
        public static float PistolRotationZ = 0.0f;

        public static float PistolAdditionalRotationX = 0.0f;
        public static float PistolAdditionalRotationY = -10.0f;
        public static float PistolAdditionalRotationZ = 0.0f;

        public static float PistolResetRotationX = 1.5f;
        public static float PistolResetRotationY = 2.0f;
        public static float PistolResetRotationZ = 1.2f;

        public static float ShortStockAdditionalRotationSpeedMulti = 2.0f;
        public static float ShortStockResetRotationSpeedMulti = 2.0f;
        public static float ShortStockRotationMulti = 2.0f;
        public static float ShortStockSpeedMulti = 6.0f;
        public static float ShortStockResetSpeedMulti = 6.0f;

        public static float ShortStockOffsetX = 0.02f;
        public static float ShortStockOffsetY = 0.1f;
        public static float ShortStockOffsetZ = -0.025f;

        public static float ShortStockRotationX = 0.0f;
        public static float ShortStockRotationY = -15.0f;
        public static float ShortStockRotationZ = 0.0f;

        public static float ShortStockAdditionalRotationX = -5.0f;
        public static float ShortStockAdditionalRotationY = -20.0f;
        public static float ShortStockAdditionalRotationZ = 5.0f;

        public static float ShortStockResetRotationX = -5.0f;
        public static float ShortStockResetRotationY = 12.0f;
        public static float ShortStockResetRotationZ = 1.0f;
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
                {
                    await Task.Delay(5);
                }

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

        private static async void LoadAudioClip(string path)
        {
            LoadedAudioClips[Path.GetFileName(path)] = await RequestAudioClip(path);
        }

        private static async Task<AudioClip> RequestAudioClip(string path)
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
            {
                await Task.Yield();
            }

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

        private void Awake()
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
                new SensPatch().Enable();

                if (EnableHipfireRecoilClimb)
                {
                    new GetRotationMultiplierPatch().Enable();
                }

                //Aiming Patches
                new SetAimingPatch().Enable();
                new ToggleAimPatch().Enable();

                //Malf Patches
                if (EnableMalfPatch && ModConfig.malf_changes)
                {
                    new GetTotalMalfunctionChancePatch().Enable();
                }
                if (InspectionlessMalfs)
                {
                    new IsKnownMalfTypePatch().Enable();
                }

                //Reload Patches
                if (EnableReloadPatches.Value)
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

                if (enableSGMastering == true)
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

                if (IncreaseCOI == true)
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
                }

                //Armor Class
                if (EnableRealArmorClass.Value == true)
                {
                    new ArmorClassDisplayPatch().Enable();
                }

                if (EnableArmorHitZones)
                {
                    new ArmorZoneBaseDisplayPatch().Enable();
                    new ArmorZoneSringValueDisplayPatch().Enable();
                }

                new IsShotDeflectedByHeavyArmorPatch().Enable();

                if (EnableArmPen)
                {
                    new IsPenetratedPatch().Enable();
                }

                //Shot Effects
                if (EnableDeafen == true)
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
            if (EnableMaterialSpeed)
            {
                new CalculateSurfacePatch().Enable();
            }
            if (EnableMaterialSpeed)
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
                new FlyingBulletPatch().Enable();
            }
        }

        private void Update()
        {
            if (Utils.CheckIsReady())
            {
                if (ModConfig.recoil_attachment_overhaul)
                {
                    if (ShotCount > PrevShotCount)
                    {
                        IsFiring = true;
                        StanceController.IsFiringFromStance = true;

                        if (EnableRecoilClimb == true && (IsAiming == true || EnableHipfireRecoilClimb == true))
                        {
                            RecoilController.DoRecoilClimb();
                        }

                        PrevShotCount = ShotCount;
                    }

                    if (ShotCount == PrevShotCount)
                    {
                        Timer += Time.deltaTime;

                        if (Timer >= resetTime)
                        {
                            IsFiring = false;
                            ShotCount = 0;
                            PrevShotCount = 0;
                            Timer = 0f;
                        }

                        StanceController.StanceShotTimer();
                    }

                    if (EnableDeafen)
                    {
                        if (Input.GetKeyDown(IncGain.Value.MainKey) && HasHeadSet)
                        {

                            if (RealTimeGain < 20)
                            {
                                RealTimeGain += 1f;
                                Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), LoadedAudioClips["beep.wav"], 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, 1.0f, EOcclusionTest.None, null, false);
                            }
                        }
                        if (Input.GetKeyDown(DecGain.Value.MainKey) && HasHeadSet)
                        {

                            if (RealTimeGain > 0)
                            {
                                RealTimeGain -= 1f;
                                Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), LoadedAudioClips["beep.wav"], 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, 1.0f, EOcclusionTest.None, null, false);
                            }
                        }

                        if (PrismEffects != null)
                        {
                            Deafening.DoDeafening();
                        }


                        if (IsBotFiring)
                        {
                            BotTimer += Time.deltaTime;
                            if (BotTimer >= 0.5f)
                            {
                                IsBotFiring = false;
                                BotTimer = 0f;
                            }
                        }

                        if (GrenadeExploded)
                        {
                            GrenadeTimer += Time.deltaTime;
                            if (GrenadeTimer >= 0.7f)
                            {
                                GrenadeExploded = false;
                                GrenadeTimer = 0f;
                            }
                        }
                    }

                    if (!IsFiring)
                    {
                        RecoilController.ResetRecoil();
                    }
                }

                StanceController.StanceState();

                if (EnableMedicalOvehaul)
                {
                    healthControllerTick += Time.deltaTime;
                    RealismHealthController.HealthController(Logger);
                }
            }
        }

        public void InitConfigs()
        {
            string testing = "0. Тестирование";
            string miscSettings = "1. Прочие настройки";
            string ballSettings = "2. Баллистика";
            string deafSettings = "3. Аудио";
            string recoilSettings = "4. Отдача";
            string speed = "5. Скорость работы с Оружием";
            string weapAimAndPos = "6. Положения и Позиции";

            EnableLogging = Config.Bind(testing, "Включить общее логгирование", false, new ConfigDescription("Включает логгирование для Дебага и разработки", null, new ConfigurationManagerAttributes { Order = 2, IsAdvanced = true }));
            EnableBallisticsLogging = Config.Bind(testing, "Включить логгирование Баллистики", false, new ConfigDescription("Включает логгирование для Дебага и разработки", null, new ConfigurationManagerAttributes { Order = 1, IsAdvanced = true }));

            EnableHoldBreath = Config.Bind(miscSettings, "Вернуть задержку дыхания", false, new ConfigDescription("Возвращает механику задержки дыхания. Этот мод сбалансирован для использования без возможности задержки дыхания.", null, new ConfigurationManagerAttributes { Order = 2 }));
            EnableStatsDelta = Config.Bind(miscSettings, "Показывать статистику в реальном времени", false, new ConfigDescription("Требуется перезагрузка. При замене/удалении модулей, статистика в предварительном просмотре будет обновляться в реальном времени. Внимание: значительно снижает производительность при модификации оружия в экранах Осмотра, Модификации или Сборки.", null, new ConfigurationManagerAttributes { Order = 1 }));

            EnableRealArmorClass = Config.Bind(ballSettings, "Отображать настоящие классы защиты", false, new ConfigDescription("Требуется перезагрузка. Заместо отображения класса брони в виде чисел, использует настоящую классификацию бронезащиты.", null, new ConfigurationManagerAttributes { Order = 1 }));

            ReduceCamRecoil = Config.Bind(recoilSettings, "Уменьшить визуальный подброс камеры", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 1 }));

            DecGain = Config.Bind(deafSettings, "Клавиша для уменьшения звука от окружающего шума", new KeyboardShortcut(KeyCode.Minus), new ConfigDescription("Каждое нажатие привязанной кнопки будет уменьшать звук от окружающего шума", null, new ConfigurationManagerAttributes { Order = 2 }));
            IncGain = Config.Bind(deafSettings, "Клавиша для повышение звука от окружающего шума", new KeyboardShortcut(KeyCode.Equals), new ConfigDescription("Каждое нажатие привязанной кнопки будет повышать звук от окружающего шума", null, new ConfigurationManagerAttributes { Order = 1 }));

            EnableReloadPatches = Config.Bind(speed, "Включить изменения в скорости работы с оружием", true, new ConfigDescription("Требуется перезагрузка. Вес оружия, Вес магазина, Различные обвесы на оружие, Параметр Баланса, Эргономики и Наличие выбитой руки влияют на множитель скорости работы с оружием.", null, new ConfigurationManagerAttributes { Order = 1 }));

            EnableSprintPenalty = Config.Bind(weapAimAndPos, "Включить задержку входа в прицел", true, new ConfigDescription("Добавляет задержку при прицеливании после бега. И чем дольше вы бежали, тем дольше будет задержка.", null, new ConfigurationManagerAttributes { Order = 5 }));
            EnableTacSprint = Config.Bind(weapAimAndPos, "Включить тактический бег", true, new ConfigDescription("При беге персонаж будет поднимать оружие вверх. Также даёт даёт +15% прибавку к скорости и +30% к мувменту, если включен функционал <<Влияния стоек>>.", null, new ConfigurationManagerAttributes { Order = 4 }));
            EnableAltPistol = Config.Bind(weapAimAndPos, "Включить альтернативное положение пистолета", true, new ConfigDescription("Пистолет будет находится в центре экрана, как будто персонаж находится в защитной стойке.", null, new ConfigurationManagerAttributes { Order = 3 }));
            ToggleActiveAim = Config.Bind(weapAimAndPos, "Использовать режим нажатия для Угловой стойки", true, new ConfigDescription("Если стоит True, то вход и выход из Active Aim будет по нажатию ПКМ. В противном случае по зажатию ПКМ", null, new ConfigurationManagerAttributes { Order = 2 }));
            ActiveAimReload = Config.Bind(weapAimAndPos, "Разрешить перезарядку оружия не выходя из Угловой стойки", true, new ConfigDescription("Разрешает производить перезарядку оружия прямо во время Active Aim стойки с бонусом к скорости (может выглядеть немного странно и неряшливо).", null, new ConfigurationManagerAttributes { Order = 1 }));
        }
    }
}

