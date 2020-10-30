using System;

namespace Entia.Json.Test
{
    sealed class Program
    {
        static void Main(string[] args)
        {
            Checks.Run();
            Console.ReadLine();
            Benches.Run();
        }
    }
}
