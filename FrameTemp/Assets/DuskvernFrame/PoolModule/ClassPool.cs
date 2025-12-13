using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Duskvern
{
    public class ClassPool<T> where T : class
    {
        private static List<T> cache = new List<T>();

        /// <summary>
        /// 直接取出一个对象，不做任何初始化
        /// </summary>
        /// <returns></returns>
        public static T Spawn()
        {
            var count = cache.Count;

            if (count > 0)
            {
                var index = count - 1;
                var instance = cache[index];
                cache.RemoveAt(index);
                return instance;
            }

            return null;
        }

        // 取到对象，立即执行初始化回调
        public static T Spawn(System.Action<T> onSpawn)
        {
            var instance = default(T);

            TrySpawn(onSpawn, ref instance);

            return instance;
        }

        public static bool TrySpawn(System.Action<T> onSpawn, ref T instance)
        {
            var count = cache.Count;

            if (count > 0)
            {
                var index = count - 1;
                instance = cache[index];
                cache.RemoveAt(index);
                onSpawn(instance);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 从池子里找到“符合条件”的对象再取出
        /// </summary>
        public static T Spawn(System.Predicate<T> match)
        {
            var instance = default(T);

            TrySpawn(match, ref instance);

            return instance;
        }

        public static bool TrySpawn(System.Predicate<T> match, ref T instance)
        {
            var index = cache.FindIndex(match);

            if (index >= 0)
            {
                instance = cache[index];

                cache.RemoveAt(index);

                return true;
            }

            return false;
        }

        /// <summary>
        /// 找符合条件的对象，并初始化它
        /// </summary>
        public static T Spawn(System.Predicate<T> match, System.Action<T> onSpawn)
        {
            var instance = default(T);

            TrySpawn(match, onSpawn, ref instance);

            return instance;
        }

        public static bool TrySpawn(System.Predicate<T> match, System.Action<T> onSpawn, ref T instance)
        {
            var index = cache.FindIndex(match);

            if (index >= 0)
            {
                instance = cache[index];

                cache.RemoveAt(index);

                onSpawn(instance);

                return true;
            }

            return false;
        }

        public static void Despawn(T instance)
        {
            if (instance != null)
            {
                cache.Add(instance);
            }
        }

        /// <summary>This will pool the passed class instance.
        /// If you need to perform despawning code then you can do that via onDespawn.</summary>
        public static void Despawn(T instance, System.Action<T> onDespawn)
        {
            if (instance != null)
            {
                onDespawn(instance);

                cache.Add(instance);
            }
        }
    }
}