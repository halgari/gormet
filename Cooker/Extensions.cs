using System;
using System.Collections.Generic;

namespace Cooker
{
    public static class Extensions
    {
        public static IEnumerable<IReadOnlyCollection<T>> GroupIntoSizes<T>(this IEnumerable<T> coll, Func<T, long> sizeFunc, long maxSize)
        {
            long currSize = 0;
            var currList = new List<T>();
            foreach (var itm in coll)
            {
                var size = sizeFunc(itm);
                if (currSize + size >= maxSize)
                {
                    yield return currList;
                    currSize = 0;
                    currList = new();
                }

                currSize += size;
                currList.Add(itm);
            }

            yield return currList;
        }
        
    }
}