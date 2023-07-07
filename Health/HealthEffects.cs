using System;
using System.Reflection;
using BepInEx.Logging;
using EFT;

namespace RealismMod
{
    public enum EHealthEffectType
    {
        Surgery,
        Tourniquet,
        HealthRegen,
        Adrenaline,
        ResourceRate
    }

    public interface IHealthEffect
    {
        public EBodyPart BodyPart { get; set; }
        public float? Duration { get; }
        public float TimeExisted { get; set; }
        public void Tick();
        public Player Player { get; }
        public float Delay { get; set; }
        public EHealthEffectType EffectType { get; }
    }

    public class HealthRegenEffect : IHealthEffect
    {
        public EBodyPart BodyPart { get; set; }
        public float? Duration { get; set; }
        public float TimeExisted { get; set; }
        public float HpPerTick { get; }
        public Player Player { get; }
        public float HpRegened { get; set; }
        public float HpRegenLimit { get; }
        public float Delay { get; set; }
        public EDamageType DamageType { get; }
        public EHealthEffectType EffectType { get; }

        public HealthRegenEffect(float hpTick, float? dur, EBodyPart part, Player player, float delay, float limit, EDamageType damageType)
        {
            TimeExisted = 0;
            HpRegened = 0;
            HpRegenLimit = limit;
            HpPerTick = hpTick;
            Duration = dur;
            BodyPart = part;
            Player = player;
            Delay = delay;
            DamageType = damageType;
            EffectType = EHealthEffectType.HealthRegen;
        }

        public void Tick()
        {
            float currentHp = Player.ActiveHealthController.GetBodyPartHealth(BodyPart).Current;
            float maxHp = Player.ActiveHealthController.GetBodyPartHealth(BodyPart).Maximum;

            if (HpRegened < HpRegenLimit)
            {
                if (Delay <= 0f)
                {
                    MethodInfo addEffectMethod = RealismHealthController.GetAddBaseEFTEffectMethodInfo();
                    Type healthChangeType = typeof(HealthChange);
                    MethodInfo genericEffectMethod = addEffectMethod.MakeGenericMethod(healthChangeType);
                    HealthChange healthChangeInstance = new HealthChange();
                    genericEffectMethod.Invoke(Player.ActiveHealthController, new object[] { BodyPart, 0f, 3f, 1f, HpPerTick, null });
                    HpRegened += HpPerTick;
                }
            }

            if (HpRegened >= HpRegenLimit || (currentHp >= maxHp) || currentHp == 0)
            {
                Duration = 0;
            }
        }
    }

    public class ResourceRateEffect : IHealthEffect
    {
        public EBodyPart BodyPart { get; set; }
        public float? Duration { get; set; }
        public float TimeExisted { get; set; }
        public float ResourcePerTick { get; }
        public Player Player { get; }
        public float Delay { get; set; }
        public EHealthEffectType EffectType { get; }

        public ResourceRateEffect(float resourcePerTick, float? dur, Player player, float delay)
        {
            TimeExisted = 0;
            Duration = dur;
            Player = player;
            Delay = delay;
            EffectType = EHealthEffectType.ResourceRate;
            BodyPart = EBodyPart.Stomach;
            ResourcePerTick = resourcePerTick;
        }

        public void Tick()
        {
            if (Delay <= 0f)
            {
                Duration -= 3;

                MethodInfo addEffectMethod = RealismHealthController.GetAddBaseEFTEffectMethodInfo();
                Type resourceRatesType = typeof(ResourceRates);
                MethodInfo genericEffectMethod = addEffectMethod.MakeGenericMethod(resourceRatesType);
                ResourceRates healthChangeInstance = new ResourceRates();
                genericEffectMethod.Invoke(Player.ActiveHealthController, new object[] { BodyPart, 0f, 3f, 0f, ResourcePerTick, null });
            }
        }
    }

    public class HealthChange : ActiveHealthControllerClass.GClass2102, IEffect, GInterface184, GInterface199
    {
        protected override void Started()
        {
            hpPerTick = Strength;
            SetHealthRatesPerSecond(hpPerTick, 0f, 0f, 0f);
            bodyPart = BodyPart;
        }

        protected override void RegularUpdate(float deltaTime)
        {
            time += deltaTime;
            if (time < 3f)
            {
                return;
            }
            time -= 3f;
            HealthController.ChangeHealth(bodyPart, hpPerTick, GClass2146.Existence);
        }

        private float hpPerTick;

        private float time;

        private EBodyPart bodyPart;
    }

    public class ResourceRates : ActiveHealthControllerClass.GClass2102, IEffect, GInterface184, GInterface199
    {
        protected override void Started()
        {
            resourcePerTick = base.Strength;
            bodyPart = base.BodyPart;
            SetHealthRatesPerSecond(0f, -resourcePerTick, -resourcePerTick, 0f);
        }

        protected override void RegularUpdate(float deltaTime)
        {
            time += deltaTime;
            if (time < 3f)
            {
                return;
            }
            time -= 3f;
            base.HealthController.ChangeEnergy(-resourcePerTick);
            base.HealthController.ChangeHydration(-resourcePerTick);
        }

        private float resourcePerTick;

        private float time;

        private EBodyPart bodyPart;
    }
}
