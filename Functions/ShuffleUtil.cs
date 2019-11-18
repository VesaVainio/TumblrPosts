using System;
using System.Collections.Generic;

namespace Functions
{
    public static class ShuffleUtil
    {
        public static void Shuffle<T>(this IList<T> list, Random rnd)
        {
            for (int i = 0; i < list.Count - 1; i++)
            {
                list.Swap(i, rnd.Next(i, list.Count));
            }
        }

        public static void Swap<T>(this IList<T> list, int i, int j)
        {
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}