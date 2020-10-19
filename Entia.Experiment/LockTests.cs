using System;
using System.Linq;
using System.Threading;

namespace Entia.Experiment
{
    public static class LockTests
    {
        class Slot
        {
            public int Value;
        }

        public static void Test()
        {
            var locker = new ReaderWriterLockSlim();
            var counter = 0;
            var _8x8 = Arrays(8, 8);
            var _16x8 = Arrays(16, 8);
            var _32x8 = Arrays(32, 8);

            var _8x16 = Arrays(8, 16);
            var _16x16 = Arrays(16, 16);
            var _32x16 = Arrays(32, 16);

            var _8x32 = Arrays(8, 32);
            var _16x32 = Arrays(16, 32);
            var _32x32 = Arrays(32, 32);

            var _8x64 = Arrays(8, 64);
            var _16x64 = Arrays(16, 64);
            var _32x64 = Arrays(32, 64);
            var _64x64 = Arrays(64, 64);

            var _8x128 = Arrays(8, 128);
            var _16x128 = Arrays(16, 128);
            var _32x128 = Arrays(32, 128);
            var _64x128 = Arrays(64, 128);
            var _128x128 = Arrays(128, 128);
            var indices = Enumerable.Range(0, 128).ToArray();
            var slots = Enumerable.Range(0, 128).Select(_ => new Slot()).ToArray();

            int[][] Arrays(int x, int y) => Enumerable.Range(0, x)
                .Select(i => Enumerable.Range(0, y).Select(j => i * j).ToArray())
                .ToArray();

            void Simple(int[][] arrays)
            {
                foreach (var array in arrays) for (int i = 0; i < array.Length; i++) array[i]++;
            }

            void Indexed(int[][] arrays)
            {
                foreach (var array in arrays) for (int i = 0; i < array.Length; i++) array[indices[i]]++;
            }

            void Locked(int[][] arrays)
            {
                foreach (var array in arrays) lock (array) for (int i = 0; i < array.Length; i++) array[i]++;
            }

            void Reads(int[][] arrays)
            {
                foreach (var array in arrays)
                {
                    locker.EnterReadLock();
                    for (int i = 0; i < array.Length; i++) array[i]++;
                    locker.ExitReadLock();
                }
            }

            void Writes(int[][] arrays)
            {
                foreach (var array in arrays)
                {
                    locker.EnterWriteLock();
                    for (int i = 0; i < array.Length; i++) array[i]++;
                    locker.ExitWriteLock();
                }
            }

            void Inter(int[][] arrays)
            {
                foreach (var array in arrays)
                {
                    if (Interlocked.Increment(ref counter) == 1) for (int i = 0; i < array.Length; i++) array[i]++;
                    Interlocked.Decrement(ref counter);
                }
            }

            void Simple8x8() => Simple(_8x8);
            void Simple16x8() => Simple(_16x8);
            void Simple32x8() => Simple(_32x8);
            void Simple8x16() => Simple(_8x16);
            void Simple16x16() => Simple(_16x16);
            void Simple32x16() => Simple(_32x16);
            void Simple8x32() => Simple(_8x32);
            void Simple16x32() => Simple(_16x32);
            void Simple32x32() => Simple(_32x32);
            void Simple8x64() => Simple(_8x64);
            void Simple16x64() => Simple(_16x64);
            void Simple32x64() => Simple(_32x64);
            void Simple64x64() => Simple(_64x64);
            void Simple8x128() => Simple(_8x128);
            void Simple16x128() => Simple(_16x128);
            void Simple32x128() => Simple(_32x128);
            void Simple64x128() => Simple(_64x128);
            void Simple128x128() => Simple(_128x128);

            void Indexed8x8() => Indexed(_8x8);
            void Indexed16x8() => Indexed(_16x8);
            void Indexed32x8() => Indexed(_32x8);
            void Indexed8x16() => Indexed(_8x16);
            void Indexed16x16() => Indexed(_16x16);
            void Indexed32x16() => Indexed(_32x16);
            void Indexed8x32() => Indexed(_8x32);
            void Indexed16x32() => Indexed(_16x32);
            void Indexed32x32() => Indexed(_32x32);
            void Indexed8x64() => Indexed(_8x64);
            void Indexed16x64() => Indexed(_16x64);
            void Indexed32x64() => Indexed(_32x64);
            void Indexed64x64() => Indexed(_64x64);
            void Indexed8x128() => Indexed(_8x128);
            void Indexed16x128() => Indexed(_16x128);
            void Indexed32x128() => Indexed(_32x128);
            void Indexed64x128() => Indexed(_64x128);
            void Indexed128x128() => Indexed(_128x128);

            void Locked8x8() => Locked(_8x8);
            void Locked16x8() => Locked(_16x8);
            void Locked32x8() => Locked(_32x8);
            void Locked8x16() => Locked(_8x16);
            void Locked16x16() => Locked(_16x16);
            void Locked32x16() => Locked(_32x16);
            void Locked8x32() => Locked(_8x32);
            void Locked16x32() => Locked(_16x32);
            void Locked32x32() => Locked(_32x32);
            void Locked8x64() => Locked(_8x64);
            void Locked16x64() => Locked(_16x64);
            void Locked32x64() => Locked(_32x64);
            void Locked64x64() => Locked(_64x64);
            void Locked8x128() => Locked(_8x128);
            void Locked16x128() => Locked(_16x128);
            void Locked32x128() => Locked(_32x128);
            void Locked64x128() => Locked(_64x128);
            void Locked128x128() => Locked(_128x128);

            void Reads8x8() => Reads(_8x8);
            void Reads16x8() => Reads(_16x8);
            void Reads32x8() => Reads(_32x8);
            void Reads8x16() => Reads(_8x16);
            void Reads16x16() => Reads(_16x16);
            void Reads32x16() => Reads(_32x16);
            void Reads8x32() => Reads(_8x32);
            void Reads16x32() => Reads(_16x32);
            void Reads32x32() => Reads(_32x32);
            void Reads8x64() => Reads(_8x64);
            void Reads16x64() => Reads(_16x64);
            void Reads32x64() => Reads(_32x64);
            void Reads64x64() => Reads(_64x64);
            void Reads8x128() => Reads(_8x128);
            void Reads16x128() => Reads(_16x128);
            void Reads32x128() => Reads(_32x128);
            void Reads64x128() => Reads(_64x128);
            void Reads128x128() => Reads(_128x128);

            void Writes8x8() => Writes(_8x8);
            void Writes16x8() => Writes(_16x8);
            void Writes32x8() => Writes(_32x8);
            void Writes8x16() => Writes(_8x16);
            void Writes16x16() => Writes(_16x16);
            void Writes32x16() => Writes(_32x16);
            void Writes8x32() => Writes(_8x32);
            void Writes16x32() => Writes(_16x32);
            void Writes32x32() => Writes(_32x32);
            void Writes8x64() => Writes(_8x64);
            void Writes16x64() => Writes(_16x64);
            void Writes32x64() => Writes(_32x64);
            void Writes64x64() => Writes(_64x64);
            void Writes8x128() => Writes(_8x128);
            void Writes16x128() => Writes(_16x128);
            void Writes32x128() => Writes(_32x128);
            void Writes64x128() => Writes(_64x128);
            void Writes128x128() => Writes(_128x128);

            void Inter8x8() => Inter(_8x8);
            void Inter16x8() => Inter(_16x8);
            void Inter32x8() => Inter(_32x8);
            void Inter8x16() => Inter(_8x16);
            void Inter16x16() => Inter(_16x16);
            void Inter32x16() => Inter(_32x16);
            void Inter8x32() => Inter(_8x32);
            void Inter16x32() => Inter(_16x32);
            void Inter32x32() => Inter(_32x32);
            void Inter8x64() => Inter(_8x64);
            void Inter16x64() => Inter(_16x64);
            void Inter32x64() => Inter(_32x64);
            void Inter64x64() => Inter(_64x64);
            void Inter8x128() => Inter(_8x128);
            void Inter16x128() => Inter(_16x128);
            void Inter32x128() => Inter(_32x128);
            void Inter64x128() => Inter(_64x128);
            void Inter128x128() => Inter(_128x128);

            for (int i = 0; i < 10; i++)
            {
                Experiment.Test.Measure(Simple8x8, new Action[]{
                    Simple8x8,
                    Simple16x8,
                    Simple32x8,
                    Simple8x16,
                    Simple16x16,
                    Simple32x16,
                    Simple8x32,
                    Simple16x32,
                    Simple32x32,
                    Simple8x64,
                    Simple16x64,
                    Simple32x64,
                    Simple64x64,
                    Simple8x128,
                    Simple16x128,
                    Simple32x128,
                    Simple64x128,
                    Simple128x128,

                    Indexed8x8,
                    Indexed16x8,
                    Indexed32x8,
                    Indexed8x16,
                    Indexed16x16,
                    Indexed32x16,
                    Indexed8x32,
                    Indexed16x32,
                    Indexed32x32,
                    Indexed8x64,
                    Indexed16x64,
                    Indexed32x64,
                    Indexed64x64,
                    Indexed8x128,
                    Indexed16x128,
                    Indexed32x128,
                    Indexed64x128,
                    Indexed128x128,

                    Locked8x8,
                    Locked16x8,
                    Locked32x8,
                    Locked8x16,
                    Locked16x16,
                    Locked32x16,
                    Locked8x32,
                    Locked16x32,
                    Locked32x32,
                    Locked8x64,
                    Locked16x64,
                    Locked32x64,
                    Locked64x64,
                    Locked8x128,
                    Locked16x128,
                    Locked32x128,
                    Locked64x128,
                    Locked128x128,

                    Reads8x8,
                    Reads16x8,
                    Reads32x8,
                    Reads8x16,
                    Reads16x16,
                    Reads32x16,
                    Reads8x32,
                    Reads16x32,
                    Reads32x32,
                    Reads8x64,
                    Reads16x64,
                    Reads32x64,
                    Reads64x64,
                    Reads8x128,
                    Reads16x128,
                    Reads32x128,
                    Reads64x128,
                    Reads128x128,

                    Writes8x8,
                    Writes16x8,
                    Writes32x8,
                    Writes8x16,
                    Writes16x16,
                    Writes32x16,
                    Writes8x32,
                    Writes16x32,
                    Writes32x32,
                    Writes8x64,
                    Writes16x64,
                    Writes32x64,
                    Writes64x64,
                    Writes8x128,
                    Writes16x128,
                    Writes32x128,
                    Writes64x128,
                    Writes128x128,

                    Inter8x8,
                    Inter16x8,
                    Inter32x8,
                    Inter8x16,
                    Inter16x16,
                    Inter32x16,
                    Inter8x32,
                    Inter16x32,
                    Inter32x32,
                    Inter8x64,
                    Inter16x64,
                    Inter32x64,
                    Inter64x64,
                    Inter8x128,
                    Inter16x128,
                    Inter32x128,
                    Inter64x128,
                    Inter128x128,
                }, 100_000);
            }
        }
    }
}