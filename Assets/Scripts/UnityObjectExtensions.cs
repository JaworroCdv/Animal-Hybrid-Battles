namespace AnimalHybridBattles
{
    using Unity.VisualScripting;

    public static class UnityObjectExtensions
    {
        public static bool HasComponent<T>(this UnityEngine.Object obj) where T : UnityEngine.Component
        {
            return obj.GetComponent<T>() != null;
        }
    }
}