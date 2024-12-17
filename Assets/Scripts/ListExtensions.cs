namespace AnimalHybridBattles
{
    using System.Collections.Generic;

    public static class ListExtensions
    {
        public static T RandomItem<T>(this List<T> collection)
        {
            return collection[UnityEngine.Random.Range(0, collection.Count)];
        }
    }
}