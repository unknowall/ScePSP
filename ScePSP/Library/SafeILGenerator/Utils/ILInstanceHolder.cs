//#define DEBUG_ILINSTANCEHOLDERPOOL_TIME

using System;
using System.Collections.Generic;
using System.Linq;

namespace SafeILGenerator.Utils
{
    public class ILInstanceHolder
    {
        private static Dictionary<Type, List<ILInstanceHolderPool>> TypePools =
            new Dictionary<Type, List<ILInstanceHolderPool>>();

        public static ILInstanceHolderPoolItem Alloc(Type type, object value = null)
        {
            lock (TypePools)
            {
                if (!TypePools.ContainsKey(type))
                {
                    TypePools[type] = new List<ILInstanceHolderPool>();
                }
                var poolsType = TypePools[type];
                var freePool = poolsType.FirstOrDefault(pool => pool.HasAvailable);
                if (freePool == null)
                {
                    var nextPoolSize = 1 << (poolsType.Count + 2);
                    //if (NextPoolSize < 2048) NextPoolSize = 2048;

#if DEBUG_ILINSTANCEHOLDERPOOL_TIME
					Console.BackgroundColor = ConsoleColor.DarkRed;
					Console.Error.Write("Create ILInstanceHolderPool({0})[{1}]...", Type, NextPoolSize);
					var Start = DateTime.UtcNow;
#endif
                    poolsType.Add(freePool = new ILInstanceHolderPool(type, nextPoolSize));
#if DEBUG_ILINSTANCEHOLDERPOOL_TIME
					var End = DateTime.UtcNow;
					Console.Error.WriteLine("Ok({0})", End - Start);
					Console.ResetColor();
#endif
                }
                var item = freePool.Alloc();
                item.Value = value;
                return item;
            }
        }

        public static ILInstanceHolderPoolItem<TType> TAlloc<TType>(TType value = default(TType))
        {
            return new ILInstanceHolderPoolItem<TType>(Alloc(typeof(TType), value));
        }

        public static int FreeCount => TypePools.Values.Sum(pools => pools.Sum(pool => pool.FreeCount));

        public static int CapacityCount => TypePools.Values.Sum(pools => pools.Sum(pool => pool.CapacityCount));
    }
}