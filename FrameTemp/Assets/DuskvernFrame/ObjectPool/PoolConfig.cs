using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Duskvern
{
    [CreateAssetMenu(fileName = "PoolModule.asset", menuName = "Duskvern/FrameConfig/Pool")]
    public class PoolConfig : ScriptableObject
    {
        #region 容器

        [SerializeField] // 记录当前已生成的对象（按顺序）-- 当 Recycle=true 时，需要知道生成顺序，方便回收最老对象
        private List<GameObject> spawnedClonesList = new List<GameObject>(); public List<GameObject> SpawnedClonesList { get => spawnedClonesList; set => spawnedClonesList = value; }
        
        [SerializeField] // 记录当前生成的对象（无需顺序）
        private HashSet<GameObject> spawnedClonesHashSet = new HashSet<GameObject>(); public HashSet<GameObject> SpawnedClonesHashSet { get => spawnedClonesHashSet; set => spawnedClonesHashSet = value; }

        [SerializeField] // 延迟销毁的物体
        private List<Delay> delays = new List<Delay>(); public List<Delay> Delays { get => delays; set => delays = value; }

        [SerializeField] // 存储已经回收的对象
        private List<GameObject> despawnedClones = new List<GameObject>(); public List<GameObject> DespawnedClonesList { get => despawnedClones; set => despawnedClones = value; }
        
        #endregion

        #region 字段
        
        /*
         * ----------------------------------------------------- 
         * 使用 DeactivateViaHierarchy 策略
         * ----------------------------------------------------- 
         */
        
        [SerializeField] // 当使用 DeactivateViaHierarchy 策略时，回收对象挂载的停用父对象
        private Transform deactivatedChild;
        
        // 获取停用父对象，如果不存在就创建一个 GameObject("Despawned Clones") 并停用
        public Transform DeactivatedChild
        {
            get
            {
                if (deactivatedChild == null)
                {
                    var child = new GameObject("Despawned Clones");
                    child.SetActive(false);
                    deactivatedChild = child.transform;
                    deactivatedChild.SetParent(InHierarchyTransform, false);
                }
                return deactivatedChild;
            }
        }
        
        [SerializeField] // 当使用 ActivateAndDeactivate 策略时，回收对象池的父节点
        private Transform inHierarchyTransform;
        public Transform InHierarchyTransform
        {
            get
            {
                if (inHierarchyTransform == null)
                {
                    Debug.LogError("InHierarchyTransform is null");
                }
                return inHierarchyTransform;
            }
            set
            {
                if (inHierarchyTransform != null)
                {
                    Debug.LogError("InHierarchyTransform is not null");
                }
                inHierarchyTransform = value;
            }
        }
        
        /*
         * -----------------------------------------------------
         * 对象池的配置内容
         * -----------------------------------------------------
         */
        
        [SerializeField] private GameObject prefab; // 对象池的预制件
        public GameObject Prefab  // 这个池子管理的原始 prefab
        { 
            set 
            {
                if (value != prefab)
                {
                    // 如果修改了 prefab，会先注销旧 prefab，然后注册新 prefab（保证 prefabMap 字典正确）
                    UnregisterPrefab(); 
                    prefab = value; 
                    RegisterPrefab();
                } 
            }
            get
            {
                return prefab;
            }
        }
        
        [SerializeField] // 对象池回收和取出的策略
        private NotificationType notification = NotificationType.None; public NotificationType Notification { set { notification = value; } get { return notification; } } 
        
        [SerializeField] // 对象激活和隐藏的策略
        private StrategyType strategy = StrategyType.ActivateAndDeactivate; public StrategyType Strategy { set { strategy = value; } get { return strategy; } } 
        
        [SerializeField] // 池子最大容量
        private int capacity; public int Capacity { set { capacity = value; } get { return capacity; } } 
        
        [SerializeField] // 当池子满时，是否回收最老的对象以生成新对象
        private bool recycle; public bool Recycle { set { recycle = value; } get { return recycle; } } 
        
        [SerializeField] // 是否 DontDestroyOnLoad
        private bool persist; public bool Persist { set { persist = value; } get { return persist; } } 
        
        [SerializeField] // 生成的对象是否在名字上加上索引
        private bool stamp; public bool Stamp { set { stamp = value; } get { return stamp; } } 
        
        [SerializeField] // 是否在控制台输出警告信息
        private bool warnings = true; public bool Warnings { set { warnings = value; } get { return warnings; } } 
        
        public int Despawned => despawnedClones.Count; // 已经回收的对象数量
        public int Spawned => spawnedClonesList.Count + spawnedClonesHashSet.Count; // 记录当前生成的对象数量
        public int Total => Spawned + Despawned; // 总数量（生成 + 回收）
        
        #endregion

        #region 生命周期方法

        public PoolModule Pool { get; private set; }
        public Dictionary<GameObject, PoolConfig> prefabMap
        {
            get
            {
                if (Pool == null || Pool.PrefabMap == null)
                {
                    Debug.LogError("Pool is null", this);
                    return null;
                }
                return Pool.PrefabMap;
            }
        }

        public List<IPoolable> tempPoolables
        {
            get
            {
                if (Pool == null || Pool.TempPoolables == null)
                {
                    Debug.LogError("Pool is null", this);
                    return null;
                }
                return Pool.TempPoolables;
            }
        }

        public PoolConfig Init(PoolModule pool, GameObject _prefab, Transform _transform, PoolModuleConfig _config)
        {
            if (pool == null || _transform == null)
            {
                Debug.LogError("对象池组件初始化有问题");
                return this;
            }
            
            this.Pool = pool;
            this.Prefab = _prefab;
            this.InHierarchyTransform = _transform;

            {
                this.Notification = _config.notification;
                this.Strategy = _config.strategy;
                this.Capacity = _config.capacity;
                this.Recycle = _config.recycle;
                this.Persist = _config.persist;
                this.Stamp = _config.stamp;
                this.Warnings = _config.warnings;
            }

            return this;
        }
        
        public void Release()
        {
            this.InHierarchyTransform = null;

            DespawnAll();
        }

        public void OnUpdate(float delta)
        {
            for (var i = delays.Count - 1; i >= 0; i--)
            {
                var delay = delays[i];
                delay.Life -= Time.deltaTime;
                if (delay.Life > 0.0f) continue;
                
                delays.RemoveAt(i); ClassPool<Delay>.Despawn(delay);

                if (delay.Clone != null)
                {
                    Despawn(delay.Clone);
                }
                else
                {
                    if (warnings == true) Debug.LogWarning("Attempting to update the delayed destruction of a prefab clone that no longer exists, did you accidentally destroy it?", this);
                }
            }
        }

        private void RegisterPrefab()
        {
            if (prefab != null)
            {
                var existingPool = default(PoolConfig);

                if (prefabMap.TryGetValue(prefab, out existingPool) == true)
                {
                    Debug.LogWarning("You have multiple pools managing the same prefab (" + prefab.name + ").", existingPool);
                }
                else
                {
                    prefabMap.Add(prefab, this);
                }
            }
        }
        
        private void UnregisterPrefab()
        {
            // Skip actually null prefabs, but allow destroyed prefabs
            if (Equals(prefab, null) == true)
            {
                return;
            }

            var existingPool = default(PoolConfig);

            if (prefabMap.TryGetValue(prefab, out existingPool) == true && existingPool == this)
            {
                prefabMap.Remove(prefab);
            }
        }

        #endregion
        
        
        #region 生成方法

        public void Spawn()
        {
            var clone = default(GameObject); TrySpawn(ref clone);
        }
        
        public void Spawn(Vector3 position)
        {
            var clone = default(GameObject); TrySpawn(ref clone, position, InHierarchyTransform.localRotation);
        }
        
        public GameObject Spawn(Transform parent, bool worldPositionStays = false)
        {
            var clone = default(GameObject); TrySpawn(ref clone, parent, worldPositionStays); return clone;
        }
        
        public GameObject Spawn(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var clone = default(GameObject); TrySpawn(ref clone, position, rotation, parent); return clone;
        }
        
        public bool TrySpawn(ref GameObject clone, Transform parent, bool worldPositionStays = false)
        {
            if (prefab == null) { if (warnings == true) Debug.LogWarning("You're attempting to spawn from a pool with a null prefab", this); return false; }
            if (parent != null && worldPositionStays == true)
            {
                return TrySpawn(ref clone, prefab.transform.position, Quaternion.identity, Vector3.one, parent, worldPositionStays);
            }
            return TrySpawn(ref clone, InHierarchyTransform.localPosition, InHierarchyTransform.localRotation, InHierarchyTransform.localScale, parent, worldPositionStays);
        }
        
        public bool TrySpawn(ref GameObject clone, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) { if (warnings == true) Debug.LogWarning("You're attempting to spawn from a pool with a null prefab", this); return false; }
            if (parent != null)
            {
                position = parent.InverseTransformPoint(position);
                rotation = Quaternion.Inverse(parent.rotation) * rotation;
            }
            return TrySpawn(ref clone, position, rotation, prefab.transform.localScale, parent, false);
        }
        
        public bool TrySpawn(ref GameObject clone)
        {
            if (prefab == null) { if (warnings == true) Debug.LogWarning("You're attempting to spawn from a pool with a null prefab", this); return false; }
            var transform = prefab.transform;
            return TrySpawn(ref clone, transform.localPosition, transform.localRotation, transform.localScale, null, false);
        }
        
        public bool TrySpawn(ref GameObject clone, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Transform parent, bool worldPositionStays)
        {
            if (prefab != null)
            {
                // Spawn a previously despawned/preloaded clone?
                for (var i = despawnedClones.Count - 1; i >= 0; i--)
                {
                    clone = despawnedClones[i];

                    despawnedClones.RemoveAt(i);

                    if (clone != null)
                    {
                        SpawnClone(clone, localPosition, localRotation, localScale, parent, worldPositionStays);

                        return true;
                    }

                    if (warnings == true) Debug.LogWarning("This pool contained a null despawned clone, did you accidentally destroy it?", this);
                }

                // Make a new clone?
                if (capacity <= 0 || Total < capacity)
                {
                    clone = CreateClone(localPosition, localRotation, localScale, parent, worldPositionStays);

                    // Add clone to spawned list
                    if (recycle == true)
                    {
                        spawnedClonesList.Add(clone);
                    }
                    else
                    {
                        spawnedClonesHashSet.Add(clone);
                    }

                    // Activate?
                    if (strategy == StrategyType.ActivateAndDeactivate)
                    {
                        clone.SetActive(true);
                    }

                    // Notifications
                    InvokeOnSpawn(clone);

                    return true;
                }

                // Recycle?
                if (recycle == true && TryDespawnOldest(ref clone, false) == true)
                {
                    SpawnClone(clone, localPosition, localRotation, localScale, parent, worldPositionStays);

                    return true;
                }
            }
            else
            {
                if (warnings == true) Debug.LogWarning("You're attempting to spawn from a pool with a null prefab", this);
            }

            return false;
        }
        
        private void SpawnClone(GameObject clone, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Transform parent, bool worldPositionStays)
        {
            // Register
            if (recycle == true)
            {
                spawnedClonesList.Add(clone);
            }
            else
            {
                spawnedClonesHashSet.Add(clone);
            }

            // Update transform
            var cloneTransform = clone.transform;

            cloneTransform.SetParent(null, false);

            cloneTransform.localPosition = localPosition;
            cloneTransform.localRotation = localRotation;
            cloneTransform.localScale    = localScale;

            cloneTransform.SetParent(parent, worldPositionStays);

            // Make sure it's in the current scene
            if (parent == null)
            {
                SceneManager.MoveGameObjectToScene(clone, SceneManager.GetActiveScene());
            }

            // Activate
            if (strategy == StrategyType.ActivateAndDeactivate)
            {
                clone.SetActive(true);
            }

            // Notifications
            InvokeOnSpawn(clone);
        }
        
        
        
        private GameObject CreateClone(Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Transform parent, bool worldPositionStays)
        {
            var clone = DoInstantiate(prefab, localPosition, localRotation, localScale, parent, worldPositionStays);

            if (stamp == true)
            {
                clone.name = prefab.name + " " + Total;
            }
            else
            {
                clone.name = prefab.name;
            }

            return clone;
        }
        
        private GameObject DoInstantiate(GameObject prefab, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Transform parent, bool worldPositionStays)
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false && UnityEditor.PrefabUtility.IsPartOfRegularPrefab(prefab) == true)
            {
                if (worldPositionStays == true)
                {
                    return (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
                }
                else
                {
                    var clone = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);

                    clone.transform.localPosition = localPosition;
                    clone.transform.localRotation = localRotation;
                    clone.transform.localScale    = localScale;

                    return clone;
                }
            }
#endif

            if (worldPositionStays == true)
            {
                return Instantiate(prefab, parent, true);
            }
            else
            {
                var clone = Instantiate(prefab, localPosition, localRotation, parent);

                clone.transform.localPosition = localPosition;
                clone.transform.localRotation = localRotation;
                clone.transform.localScale    = localScale;

                return clone;
            }
        }
        
        private void InvokeOnSpawn(GameObject clone)
        {
            switch (notification)
            {
                case NotificationType.SendMessage: clone.SendMessage("OnSpawn", SendMessageOptions.DontRequireReceiver); break;
                case NotificationType.BroadcastMessage: clone.BroadcastMessage("OnSpawn", SendMessageOptions.DontRequireReceiver); break;
                case NotificationType.IPoolable: clone.GetComponents(tempPoolables); for (var i = tempPoolables.Count - 1; i >= 0; i--) tempPoolables[i].OnSpawn(); break;
                case NotificationType.BroadcastIPoolable: clone.GetComponentsInChildren(tempPoolables); for (var i = tempPoolables.Count - 1; i >= 0; i--) tempPoolables[i].OnSpawn(); break;
            }
        }

        #endregion
        
        [Button("Clean"), PropertyOrder(-1)]
        public void Clean()
        {
            for (var i = despawnedClones.Count - 1; i >= 0; i--)
            {
                DestroyImmediate(despawnedClones[i]);
            }

            despawnedClones.Clear();
        }
        
        public void GetClones(List<GameObject> gameObjects, bool addSpawnedClones = true, bool addDespawnedClones = true)
        {
            if (gameObjects != null)
            {
                gameObjects.Clear();

                if (addSpawnedClones == true)
                {
                    gameObjects.AddRange(spawnedClonesList);
                    gameObjects.AddRange(spawnedClonesHashSet);
                }

                if (addDespawnedClones == true)
                {
                    gameObjects.AddRange(despawnedClones);
                }
            }
        }
        
        private void MergeSpawnedClonesToList()
        {
            if (spawnedClonesHashSet.Count > 0)
            {
                spawnedClonesList.AddRange(spawnedClonesHashSet);

                spawnedClonesHashSet.Clear();
            }
        }
        
        #region 回收

        [Button("Despawn Oldest"), PropertyOrder(-1)]
        public void DespawnOldest()
        {
            var clone = default(GameObject);

            TryDespawnOldest(ref clone, true);
        }
        
        private bool TryDespawnOldest(ref GameObject clone, bool registerDespawned)
        {
            MergeSpawnedClonesToList();

            // Loop through all spawnedClones from the front (oldest) until one is found
            while (spawnedClonesList.Count > 0)
            {
                clone = spawnedClonesList[0];

                spawnedClonesList.RemoveAt(0);

                if (clone != null)
                {
                    return true;
                }

                if (warnings == true) Debug.LogWarning("This pool contained a null spawned clone, did you accidentally destroy it?", this);
            }

            return false;
        }
        
        [Button("Despawn All"), PropertyOrder(-1)]
        public void DespawnAll()
        {
            DespawnAll(true);
        }
        
        public void DespawnAll(bool cleanLinks)
        {
            // Merge
            MergeSpawnedClonesToList();

            // Despawn
            for (var i = spawnedClonesList.Count - 1; i >= 0; i--)
            {
                var clone = spawnedClonesList[i];

                if (clone != null)
                {
                    if (cleanLinks == true)
                    {
                        PoolUtil.Links.Remove(clone);
                    }
                }
            }

            spawnedClonesList.Clear();
            
            // Clear all delays
            for (var i = delays.Count - 1; i >= 0; i--)
            {
                ClassPool<Delay>.Despawn(delays[i]);
            }

            delays.Clear();
        }
        
        public void Despawn(GameObject clone, float t = 0.0f)
        {
            if (clone != null)
            {
                if (t > 0.0f)
                {
                    DespawnWithDelay(clone, t);
                }
                else
                {
                    TryDespawn(clone);
                    for (var i = delays.Count - 1; i >= 0; i--)
                    {
                        var delay = delays[i];

                        if (delay.Clone == clone)
                        {
                            delays.RemoveAt(i);
                        }
                    }
                }
                
            }
            else
            {
                if (warnings == true) Debug.LogWarning("You're attempting to despawn a null gameObject", this);
            }
        }
        
        private void DespawnWithDelay(GameObject clone, float t)
        {
            // If this object is already marked for delayed despawn, update the time and return
            for (var i = delays.Count - 1; i >= 0; i--)
            {
                var delay = delays[i];

                if (delay.Clone == clone)
                {
                    if (t < delay.Life)
                    {
                        delay.Life = t;
                    }

                    return;
                }
            }

            // Create delay
            var newDelay = ClassPool<Delay>.Spawn() ?? new Delay(); // 这样进行对象的获取

            newDelay.Clone = clone;
            newDelay.Life  = t;

            delays.Add(newDelay);
        }
        
        private void TryDespawn(GameObject clone)
        {
            if (spawnedClonesHashSet.Remove(clone) == true || spawnedClonesList.Remove(clone) == true)
            {
                DespawnNow(clone);
            }
            else
            {
                if (warnings == true) Debug.LogWarning("You're attempting to despawn a GameObject that wasn't spawned from this pool, make sure your Spawn and Despawn calls match.", clone);
            }
        }
        
        private void DespawnNow(GameObject clone, bool register = true)
        {
            // Add clone to despawned list
            if (register == true)
            {
                despawnedClones.Add(clone);
            }

            // Messages?
            InvokeOnDespawn(clone);

            // Deactivate it
            if (strategy == StrategyType.ActivateAndDeactivate)
            {
                clone.SetActive(false);

                clone.transform.SetParent(InHierarchyTransform, false);
            }
            else
            {
                clone.transform.SetParent(DeactivatedChild, false);
            }
        }
        
        private void InvokeOnDespawn(GameObject clone)
        {
            switch (notification)
            {
                case NotificationType.SendMessage: clone.SendMessage("OnDespawn", SendMessageOptions.DontRequireReceiver); break;
                case NotificationType.BroadcastMessage: clone.BroadcastMessage("OnDespawn", SendMessageOptions.DontRequireReceiver); break;
                case NotificationType.IPoolable: clone.GetComponents(tempPoolables); for (var i = tempPoolables.Count - 1; i >= 0; i--) tempPoolables[i].OnDespawn(); break;
                case NotificationType.BroadcastIPoolable: clone.GetComponentsInChildren(tempPoolables); for (var i = tempPoolables.Count - 1; i >= 0; i--) tempPoolables[i].OnDespawn(); break;
            }
        }

        #endregion

        #region 剥离对象

        public void Detach(GameObject clone, bool cleanLinks = true)
        {
            if (clone != null)
            {
                if (spawnedClonesHashSet.Remove(clone) == true || spawnedClonesList.Remove(clone) == true || despawnedClones.Remove(clone) == true)
                {
                    if (cleanLinks == true)
                    {
                        // Remove the link between this clone and this pool if it hasn't already been
                        PoolUtil.Links.Remove(clone);
                    }
                }
                else
                {
                    if (warnings == true) Debug.LogWarning("You're attempting to detach a GameObject that wasn't spawned from this pool.", clone);
                }
            }
            else
            {
                if (warnings == true) Debug.LogWarning("You're attempting to detach a null GameObject", this);
            }
        }

        #endregion
    }
}


