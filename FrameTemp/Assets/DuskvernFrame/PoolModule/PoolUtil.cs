using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Duskvern
{
    public static class PoolUtil
    {
        public static Dictionary<GameObject, PoolConfig> Links = new Dictionary<GameObject, PoolConfig>();

        #region 生成 GameObject

        /// <summary>
        /// 用于需要指定父物体，但又不需要关心位置/旋转
        /// worldPositionStays ：当你把物体设置为某个父物体的子物体时，要不要“保持原来的世界坐标”
        /// </summary>
        public static T Spawn<T>(T prefab, Transform parent, bool worldPositionStays = false) where T : Component
        {
            if (prefab == null)
            {
                Debug.LogError("Attempting to spawn a null prefab.");
                return null;
            }

            var clone = Spawn(prefab.gameObject, parent, worldPositionStays);
            return clone != null ? clone.GetComponent<T>() : null;
        }

        /// <summary>
        /// 通过组件生成 GameObject
        /// 用于要明确指定「生成位置 + 旋转」的场景
        /// </summary>
        public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null)
            where T : Component
        {
            if (prefab == null)
            {
                Debug.LogError("Attempting to spawn a null prefab.");
                return null;
            }

            var clone = Spawn(prefab.gameObject, position, rotation, parent);
            return clone != null ? clone.GetComponent<T>() : null;
        }

        /// <summary>
        /// 通过组件生成 GameObject
        /// 不关心父物体、不关心初始化位置/旋转、只要生成一个对象，后续自己处理
        /// </summary>
        public static T Spawn<T>(T prefab) where T : Component
        {
            if (prefab == null)
            {
                Debug.LogError("Attempting to spawn a null prefab.");
                return null;
            }

            var clone = Spawn(prefab.gameObject);
            return clone != null ? clone.GetComponent<T>() : null;
        }

        /// <summary>
        /// 用于需要指定父物体，但又不需要关心位置/旋转
        /// worldPositionStays ：当你把物体设置为某个父物体的子物体时，要不要“保持原来的世界坐标”
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Transform parent, bool worldPositionStays = false)
        {
            if (prefab == null)
            {
                Debug.LogError("Attempting to spawn a null prefab.");
                return null;
            }

            var transform = prefab.transform;
            if (parent != null && worldPositionStays == true)
            {
                return Spawn(prefab, prefab.transform.position, Quaternion.identity, Vector3.one, parent,
                    worldPositionStays);
            }

            return Spawn(prefab, transform.localPosition, transform.localRotation, transform.localScale, parent, false);
        }

        /// <summary>
        /// 通过组件生成 GameObject
        /// 用于要明确指定「生成位置 + 旋转」的场景
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation,
            Transform parent = null)
        {
            if (prefab == null)
            {
                Debug.LogError("Attempting to spawn a null prefab.");
                return null;
            }

            if (parent != null)
            {
                // ：把一个世界坐标（world position）转换成相对于某个 Transform 的本地坐标（local position）
                position = parent.InverseTransformPoint(position);
                rotation = Quaternion.Inverse(parent.rotation) * rotation;
            }

            return Spawn(prefab, position, rotation, prefab.transform.localScale, parent, false);
        }

        /// <summary>
        /// 通过组件生成 GameObject
        /// 不关心父物体、不关心初始化位置/旋转、只要生成一个对象，后续自己处理
        /// </summary>
        public static GameObject Spawn(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("Attempting to spawn a null prefab.");
                return null;
            }

            var transform = prefab.transform;
            return Spawn(prefab, transform.localPosition, transform.localRotation, transform.localScale, null, false);
        }

        private static GameObject Spawn(GameObject prefab, Vector3 localPosition, Quaternion localRotation,
            Vector3 localScale, Transform parent, bool worldPositionStays)
        {
            if (prefab != null)
            {
                var pool = default(PoolConfig);
                if (PoolModule.Instance.TryFindPoolByPrefab(prefab, ref pool) == false)
                {
                    pool = PoolModule.Instance.CreatePool(prefab);
                }

                var clone = default(GameObject);
                if (pool.TrySpawn(ref clone, localPosition, localRotation, localScale, parent, worldPositionStays) ==
                    true)
                {
                    // 这里是在判断 是否是 Recycle 的取对象
                    if (Links.Remove(clone) == true) // Links 是全局字典，记录了 每个 clone 属于哪个池子
                    {
                        // pool.Recycle 表示池子是否允许回收旧对象
                        if (pool.Recycle == true)
                        {
                        }
                        // This shouldn't happen
                        else
                        {
                            Debug.LogWarning(
                                "You're attempting to spawn a clone that hasn't been despawned. Make sure all your Spawn and Despawn calls match, you shouldn't be manually destroying them!",
                                clone);
                        }
                    }

                    Links.Add(clone, pool);
                    return clone;
                }
            }
            else
            {
                Debug.LogError("Attempting to spawn a null prefab.");
            }

            return null;
        }

        #endregion

        #region 回收预制件

        /// <summary>
        /// 回收一个物体，通过component
        /// </summary>
        public static void DespawnAll()
        {
            foreach (var instance in PoolModule.Instance.PoolInstances)
            {
                instance.DespawnAll(false);
            }

            Links.Clear();
        }

        /// <summary>
        /// 通过组件回收一个物体
        /// </summary>
        /// <param name="clone"></param>
        public static void Despawn(Component clone, float delay = 0.0f)
        {
            if (clone != null) Despawn(clone.gameObject, delay);
        }

        /// <summary>
        /// 回收一个物体，通过gameobject
        /// </summary>
        /// <param name="clone"></param>
        public static void Despawn(GameObject clone, float delay = 0.0f)
        {
            if (clone != null)
            {
                var pool = default(PoolConfig);

                // Try and find the pool associated with this clone
                if (Links.TryGetValue(clone, out pool) == true)
                {
                    // Remove the association
                    Links.Remove(clone);

                    pool.Despawn(clone, delay);
                }
                else
                {
                    if (PoolModule.Instance.TryFindPoolByClone(clone, ref pool) == true)
                    {
                        pool.Despawn(clone, delay);
                    }
                    else
                    {
                        Debug.LogWarning(
                            "You're attempting to despawn a gameObject that wasn't spawned from a pool (or the pool was destroyed).",
                            clone);

                        // Fall back to normal destroying
#if UNITY_EDITOR
                        if (Application.isPlaying == false)
                        {
                            Object.DestroyImmediate(clone);

                            return;
                        }
#endif
                        Object.Destroy(clone);
                    }
                }
            }
            else
            {
                Debug.LogWarning("You're attempting to despawn a null gameObject.", clone);
            }
        }

        #endregion

        #region 对象池移除对象

        public static void Detach(Component clone, bool detachFromPool = true)
        {
            if (clone != null) Detach(clone.gameObject, detachFromPool);
        }

        public static void Detach(GameObject clone, bool detachFromPool)
        {
            if (clone != null)
            {
                if (detachFromPool == true)
                {
                    var pool = default(PoolConfig);

                    // Try and find the pool associated with this clone
                    if (Links.TryGetValue(clone, out pool) == true)
                    {
                        // Remove the association
                        Links.Remove(clone);

                        pool.Detach(clone, false);
                    }
                    else
                    {
                        if (PoolModule.Instance.TryFindPoolByClone(clone, ref pool) == true)
                        {
                            pool.Detach(clone, false);
                        }
                        else
                        {
                            Debug.LogWarning(
                                "You're attempting to detach a GameObject that wasn't spawned from any pool (or its pool was destroyed).",
                                clone);
                        }
                    }
                }
                else
                {
                    Links.Remove(clone);
                }
            }
            else
            {
                Debug.LogWarning("You're attempting to detach a null GameObject.", clone);
            }
        }

        #endregion
    }
}