using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Duskvern
{
    public class PoolModule : MonoBehaviour
    {
        #region 单例

        public static PoolModule Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // 切换场景不销毁
            }
            else
            {
                Destroy(gameObject); // 避免重复实例
            }
        }

        #endregion

        #region 字段

        [SerializeField, LabelText("生成对象池模块的默认配置")]
        private PoolModuleConfig defaultConfig;

        // 存储 prefab → 池子的映射
        [ShowInInspector] private Dictionary<GameObject, PoolConfig> prefabMap;

        public Dictionary<GameObject, PoolConfig> PrefabMap => prefabMap;

        // 所有场景中激活的对象池模块
        public LinkedList<PoolConfig> PoolInstances;

        // 当前模块中
        private LinkedListNode<PoolConfig> instancesNode;

        // 在生成一个对象的时候会在回调中使用 --- 主要是为了当作一个临时的容器，避免反复的new
        private static List<IPoolable> tempPoolables;
        public List<IPoolable> TempPoolables => tempPoolables;

        #endregion

        #region 生命周期

        public void Init()
        {
            (prefabMap ??= new()).Clear();
            (PoolInstances ??= new LinkedList<PoolConfig>()).Clear();
            (tempPoolables ??= new List<IPoolable>()).Clear();
        }

        public void Release()
        {
            ReleasePool(ReleasePoolType.All);
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var _pool in PoolInstances)
            {
                _pool.OnUpdate(deltaTime);
            }
        }

        #endregion

        public PoolConfig CreatePool(GameObject prefab)
        {
            PoolConfig pool = ScriptableObject.CreateInstance<PoolConfig>();
            pool.name = "PoolSO_" + prefab.name;
            GameObject root = new GameObject(pool.name + "_Root");
            root.transform.SetParent(transform);
            pool.Init(this, prefab, root.transform, defaultConfig);
            PoolInstances.AddLast(pool);
            return pool;
        }

        /// <summary>
        /// 根据类型释放对象池
        /// </summary>
        /// <param name="releaseType"></param>
        public void ReleasePool(ReleasePoolType releaseType)
        {
            switch (releaseType)
            {
                case ReleasePoolType.TransitionScene:
                {
                    foreach (var pool in PoolInstances)
                    {
                        if (pool.Persist) continue;
                        ReleasePool(pool);
                    }

                    break;
                }
                case ReleasePoolType.All:
                {
                    foreach (var pool in PoolInstances)
                    {
                        ReleasePool(pool);
                    }

                    break;
                }
            }

            Debug.LogError("释放对象池的类型错误");
        }

        /// <summary>
        /// 释放指定类型的对象池
        /// </summary>
        /// <param name="pool"></param>
        public void ReleasePool(PoolConfig pool)
        {
            // TODO 这里的释放逻辑尽可能放到SO中
            if (pool == null)
            {
                Debug.LogError("释放的对象池为null");
                return;
            }

            pool.Release();
            prefabMap.Remove(pool.Prefab);
            PoolInstances.Remove(pool);
            Destroy(pool);
        }

        /// -----------------------------------------
        /// 寻找对象池
        /// -----------------------------------------
        /// <summary>
        /// 通过特定 预制件 寻找对应的池子
        /// </summary>
        public bool TryFindPoolByPrefab(GameObject prefab, ref PoolConfig foundPool)
        {
            return prefabMap.TryGetValue(prefab, out foundPool);
        }

        /// <summary>
        /// 通过实例 Clone 寻找对应的池子
        /// </summary>
        public bool TryFindPoolByClone(GameObject clone, ref PoolConfig pool)
        {
            /// 为什么需要这个方法，而不是每个预制件中都保存池的引用 或者预制件的引用
            /// Prefab 是共享的，不是实例化的
            /// 实例化对象可能被任意操作 --- 有些 clone 可能被脱离池子（Detach）或者被外部 Destroy
            /// 果在每个实例上存池子引用，你每个对象都多了一个字段，序列化和内存开销都会增加，尤其是大量小对象（比如子弹、特效）时
            /// 有些对象可能从多个池子生成，或者临时生成而不属于任何池子
            foreach (var instance in PoolInstances)
            {
                if (instance.SpawnedClonesHashSet.Contains(clone) == true)
                {
                    pool = instance;

                    return true;
                }

                for (var j = instance.SpawnedClonesList.Count - 1; j >= 0; j--)
                {
                    if (instance.SpawnedClonesList[j] == clone)
                    {
                        pool = instance;

                        return true;
                    }
                }
            }

            return false;
        }
    }

    #region 数据结构

    public enum ReleasePoolType
    {
        TransitionScene,
        All
    }

    [System.Serializable] // 处理延迟回收的数据结构
    public class Delay
    {
        public GameObject Clone;
        public float Life;
    }

    public enum NotificationType // 预制件生成时 的处理方式
    {
        /// <summary>
        /// 不发送通知，对象生成或回收时不执行任何额外方法，需要自己在 OnEnable/OnDisable 里处理逻辑
        /// </summary>
        None,

        /// <summary>
        /// 发送消息 调用对象的 SendMessage("OnSpawn") / SendMessage("OnDespawn")，只作用于对象本身，不包含子对象
        /// </summary>
        SendMessage,

        /// <summary>
        /// 调用对象及其所有子对象的 BroadcastMessage("OnSpawn") / "OnDespawn"
        /// </summary>
        BroadcastMessage,

        /// <summary>
        /// 对对象上实现了 IPoolable 接口的组件，调用它们的 OnSpawn() / OnDespawn() 方法
        /// </summary>
        IPoolable,

        /// <summary>
        /// 对对象及其子对象上实现了 IPoolable 的所有组件调用 OnSpawn() / OnDespawn()
        /// </summary>
        BroadcastIPoolable
    }

    /// <summary>
    /// 用于 控制对象池中回收（Despawn）对象的管理策略，也就是对象生成和回收时 对象的父级和激活状态如何处理
    /// </summary>
    public enum StrategyType
    {
        /// <summary>
        /// 对象被回收时调用 SetActive(false)，同时将对象放回到池的 Transform 下  
        /// 再生成时调用 SetActive(true) 并设置父级和位置  
        /// 常用于需要动态控制对象激活状态的场景（比如子弹、特效）
        /// </summary>
        ActivateAndDeactivate,

        /// <summary>
        /// 回收对象时不改变对象自身激活状态，而是将其放到一个 已经停用的父对象 下（DeactivatedChild）
        /// 不触碰对象的 SetActive，只靠父对象整体控制显隐
        /// 常用于保持对象原始激活状态或需要复杂层级管理的场景
        /// </summary>
        DeactivateViaHierarchy
    }

    [Serializable]
    public struct PoolModuleConfig
    {
        [LabelText("通知类型")] public NotificationType notification;
        [LabelText("策略类型")] public StrategyType strategy;
        [LabelText("容量")] public int capacity;
        [LabelText("是否回收")] public bool recycle;
        [LabelText("是否持久化")] public bool persist;
        [LabelText("是否加索引")] public bool stamp;
        [LabelText("是否输出警告")] public bool warnings;
    }

    #endregion
}