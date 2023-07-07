using System.Reflection;
using EFT.HealthSystem;

namespace RealismMod
{
    public class BodyPartStateWrapper
    {
        private readonly object bodyPartStateInstance;
        private readonly FieldInfo isDestroyedField;
        private readonly FieldInfo healthField;

        public BodyPartStateWrapper(object bodyPartStateInstance)
        {
            this.bodyPartStateInstance = bodyPartStateInstance;
            isDestroyedField = bodyPartStateInstance.GetType().GetField("IsDestroyed");
            healthField = bodyPartStateInstance.GetType().GetField("Health");
        }

        public bool IsDestroyed
        {
            get
            {
                return (bool)isDestroyedField.GetValue(bodyPartStateInstance);
            }
            set
            {
                isDestroyedField.SetValue(bodyPartStateInstance, value);
            }
        }

        public HealthValue Health
        {
            get
            {
                return (HealthValue)healthField.GetValue(bodyPartStateInstance);
            }
            set
            {
                healthField.SetValue(bodyPartStateInstance, value);
            }
        }
    }
}
