using EFT;
using System;
using System.Reflection;

namespace RealismMod
{
    public enum EHealthEffectType 
    {
        Surgery,
        Tourniquet,
        HealthRegen
    }

    public interface IHealthEffect
    {
        public EBodyPart BodyPart { get; set; }
        public int? Duration { get; }
        public float TimeExisted { get; set; }
        public void Tick();
        public Player Player { get; }
        public float Delay { get; set; }
        public EHealthEffectType EffectType { get; }
    }

    public class HealthRegenEffect : IHealthEffect
    {
        public EBodyPart BodyPart { get; set; }
        public int? Duration { get; set; }
        public float TimeExisted { get; set; }
        public float HpPerTick { get; }
        public Player Player { get; }
        public float HpRegened { get; set; }
        public float HpRegenLimit { get; }
        public float Delay { get; set; }
        public EDamageType DamageType { get; }
        public EHealthEffectType EffectType { get; }

        public HealthRegenEffect(float hpTick, int? dur, EBodyPart part, Player player, float delay, float limit, EDamageType damageType)
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
                    MethodInfo addEffectMethod = RealismHealthController.GetAddBaseEFTEffectMethod();
                    Type healthChangeType = typeof(HealthChange);
                    MethodInfo genericEffectMethod = addEffectMethod.MakeGenericMethod(healthChangeType);
                    HealthChange healthChangeInstance = new HealthChange();
                    genericEffectMethod.Invoke(Player.ActiveHealthController, new object[] { BodyPart, 0f, 3f, 1f, HpPerTick, null });
                    HpRegened += HpPerTick;
                }
            }

            if(HpRegened >= HpRegenLimit || (currentHp >= maxHp) || currentHp == 0)
            {
                Duration = 0;
            }
        }
    }

    public class HealthChange : ActiveHealthControllerClass.GClass2102, IEffect, GInterface184, GInterface199
    {
        protected override void Started()
        {
            this.hpPerTick = base.Strength;
            this.SetHealthRatesPerSecond(this.hpPerTick, 0f, 0f, 0f);
            this.bodyPart = base.BodyPart;
        }

        protected override void RegularUpdate(float deltaTime)
        {
            this.time += deltaTime;
            if (this.time < 3f)
            {
                return;
            }
            this.time -= 3f;
            base.HealthController.ChangeHealth(bodyPart, this.hpPerTick, GClass2146.Existence);
        }

        private float hpPerTick;

        private float time;

        private EBodyPart bodyPart;

    }
}
