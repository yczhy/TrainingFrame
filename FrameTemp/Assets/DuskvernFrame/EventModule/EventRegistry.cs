using UnityEngine;

namespace Duskvern
{
    public static partial class EventRegistry
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            // 触发 EventRegistry 的静态构造
        }
    }
    
    public static partial class EventRegistry
    {
        static EventRegistry()
        {
            _ = EventDeclare.Sample.sample;
        }
    }
}


