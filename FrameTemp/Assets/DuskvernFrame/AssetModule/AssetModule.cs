using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Duskvern 
{
    public static class AssetUtil
    {
        /// <summary>
        /// 使用一个 MonoBehaviour 来回调加载资源 --- 用来判断调用者是否还活着
        /// </summary>
        public static void Load<T>(
            MonoBehaviour owner,
            string path,
            Action<T> onLoaded)
            where T : Object
        {
            LoadInternal(owner, path, onLoaded).Forget();
        }
        
        private static async UniTaskVoid LoadInternal<T>(
            MonoBehaviour owner,
            string path,
            Action<T> onLoaded)
            where T : Object
        {
            var asset = await AssetModule.LoadAssetAsync<T>(path);

            if (owner == null) return; // 已销毁，不回调

            onLoaded?.Invoke(asset);
        }
        
        /// <summary>
        /// 适用于非MONO异步加载资源回调的接口
        /// </summary>
        public static void Load<T>(
            string path,
            CancellationToken token,
            Action<T> onLoaded)
            where T : UnityEngine.Object
        {
            LoadInternal(token, path, onLoaded).Forget();
        }
        
        private static async UniTaskVoid LoadInternal<T>(
            CancellationToken token,
            string path,
            Action<T> onLoaded)
            where T : UnityEngine.Object
        {
            try
            {
                var asset = await AssetModule
                    .LoadAssetAsync<T>(path)
                    .AttachExternalCancellation(token);

                onLoaded?.Invoke(asset);
            }
            catch (OperationCanceledException)
            {
                // 正常取消，不算错误
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }
    
    public class AssetModule
    {
        private class AssetEntry
        {
            public Object Asset;
            public AsyncOperationHandle Handle;
        }
        private static readonly Dictionary<string, AssetEntry> _assetCache = new();
        private static readonly Dictionary<string, UniTask<Object>> _loadingTasks = new(); // 正在加载的内容
        private static readonly Dictionary<string, AsyncOperationHandle> _labelHandles = new();

        private static SceneInstance? _currentScene;
        
        #region === Asset 加载 ===
        
        /// <summary>
        /// UniTask 加载资源
        /// </summary>
        public static async UniTask<T> LoadAssetAsync<T>(string path, bool cache = true) where T : Object
        {
            if (_assetCache.TryGetValue(path, out var entry))
            {
                return entry.Asset as T;
            }

            if (_loadingTasks.TryGetValue(path, out var loadingTask))
            {
                return (T)await loadingTask;
            }
            
            var task = LoadInternal();
            _loadingTasks[path] = task;
            try
            {
                return (T)await task;
            }
            finally // finally 会在 await 的 Task 完成之后 执行
            {
                _loadingTasks.Remove(path); // 问题1 为什么在这里处理这个移除，那岂不是一直都进不来
            }

            async UniTask<Object> LoadInternal()
            {
                var handle = Addressables.LoadAssetAsync<T>(path);
                await handle;
                await UniTask.SwitchToMainThread();
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    throw new Exception($"LoadAsset failed: {path}");
                }

                if (cache)
                {
                    _assetCache[path] = new AssetEntry
                    {
                        Asset = handle.Result,
                        Handle = handle
                    };
                }
                return handle.Result;
            }
        }
        
        /// <summary>
        /// 同步加载
        /// </summary>
        public static T LoadAssetSync<T>(string path) where T : Object
        {
            if (_assetCache.TryGetValue(path, out var entry))
            {
                return entry.Asset as T;
            }
            
            var handle = Addressables.LoadAssetAsync<T>(path);
            var result = handle.WaitForCompletion();

            _assetCache[path] = new AssetEntry
            {
                Asset = result,
                Handle = handle
            };
            return result;
        }

        public static void ReleaseAsset(string path)
        {
            if (_assetCache.TryGetValue(path, out var entry))
            {
                Addressables.Release(entry.Handle);
                _assetCache.Remove(path);
            }
        }

        public static void ReleaseAllAssets()
        {
            foreach (var entry in _assetCache.Values)
            {
                Addressables.Release(entry.Handle);
            }
            _assetCache.Clear();
        }

        #endregion
        
        #region === Label 加载 ===
        
        public static async UniTask<IList<T>> LoadAssetsByLabelAsync<T>(string label)
        {
            if (_labelHandles.TryGetValue(label, out var existing))
                return existing.Result as IList<T>;

            var handle = Addressables.LoadAssetsAsync<T>(label, null);
            await handle;

            if (handle.Status != AsyncOperationStatus.Succeeded)
                throw new Exception($"LoadAssetsByLabel failed: {label}");

            _labelHandles[label] = handle;
            return handle.Result;
        }

        public static void ReleaseLabel(string label)
        {
            if (_labelHandles.TryGetValue(label, out var handle))
            {
                Addressables.Release(handle);
                _labelHandles.Remove(label);
            }
        }
        
        #endregion
        
        #region === Scene 加载 ===

        /// <summary>
        /// 这里只有加载场景的功能，加载就意味着卸载当前场景
        /// </summary>
        public static async UniTask LoadSceneAsync(string sceneKey, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (_currentScene.HasValue)
            {
                await Addressables.UnloadSceneAsync(_currentScene.Value);
                _currentScene = null;
            }

            var handle = Addressables.LoadSceneAsync(sceneKey, mode, activateOnLoad: true);
            await handle;

            if (handle.Status != AsyncOperationStatus.Succeeded)
                throw new Exception($"LoadScene failed: {sceneKey}");

            _currentScene = handle.Result;
        }

        #endregion
    }
}

