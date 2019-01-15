using System;

namespace Entia.Experiment
{
    public static class Extensions
    {
        public static void Shuffle<T>(this T[] array)
        {
            var random = new Random();
            for (int i = 0; i < array.Length; i++)
            {
                var index = random.Next(array.Length);
                var item = array[i];
                array[i] = array[index];
                array[index] = item;
            }
        }
    }
}