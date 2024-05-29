using JAFDTC.Utilities;

namespace JAFDTC.Models.A10C
{
    public abstract class A10CSystemBase : BindableObject, ISystem
    {
        public abstract bool IsDefault { get; }

        public abstract void Reset();
    }
}
