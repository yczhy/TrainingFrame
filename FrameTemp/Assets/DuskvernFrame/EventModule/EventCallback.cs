using System;

namespace Duskvern
{
    public abstract class EventCallback
    {
        public abstract bool IsEmpty { get; }
    }

    public sealed class EventCallback_0 : EventCallback
    {
        private Action _callback;

        public override bool IsEmpty => _callback == null;

        public void Add(Action callback) => _callback += callback;
        public void Remove(Action callback) => _callback -= callback;
        public void Clear() => _callback = null;
        public void Invoke() => _callback?.Invoke();
    }

    public sealed class EventCallback_1<T1> : EventCallback
    {
        private Action<T1> _callback;

        public override bool IsEmpty => _callback == null;

        public void Add(Action<T1> callback) => _callback += callback;
        public void Remove(Action<T1> callback) => _callback -= callback;
        public void Clear() => _callback = null;
        public void Invoke(T1 arg1) => _callback?.Invoke(arg1);
    }

    public sealed class EventCallback_2<T1, T2> : EventCallback
    {
        private Action<T1, T2> _callback;

        public override bool IsEmpty => _callback == null;

        public void Add(Action<T1, T2> callback) => _callback += callback;
        public void Remove(Action<T1, T2> callback) => _callback -= callback;
        public void Clear() => _callback = null;
        public void Invoke(T1 arg1, T2 arg2) => _callback?.Invoke(arg1, arg2);
    }

    public sealed class EventCallback_3<T1, T2, T3> : EventCallback
    {
        private Action<T1, T2, T3> _callback;

        public override bool IsEmpty => _callback == null;

        public void Add(Action<T1, T2, T3> callback) => _callback += callback;
        public void Remove(Action<T1, T2, T3> callback) => _callback -= callback;
        public void Clear() => _callback = null;
        public void Invoke(T1 arg1, T2 arg2, T3 arg3) => _callback?.Invoke(arg1, arg2, arg3);
    }

    public sealed class EventCallback_4<T1, T2, T3, T4> : EventCallback
    {
        private Action<T1, T2, T3, T4> _callback;

        public override bool IsEmpty => _callback == null;

        public void Add(Action<T1, T2, T3, T4> callback) => _callback += callback;
        public void Remove(Action<T1, T2, T3, T4> callback) => _callback -= callback;
        public void Clear() => _callback = null;
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4) => _callback?.Invoke(arg1, arg2, arg3, arg4);
    }

    public sealed class EventCallback_5<T1, T2, T3, T4, T5> : EventCallback
    {
        private Action<T1, T2, T3, T4, T5> _callback;

        public override bool IsEmpty => _callback == null;

        public void Add(Action<T1, T2, T3, T4, T5> callback) => _callback += callback;
        public void Remove(Action<T1, T2, T3, T4, T5> callback) => _callback -= callback;
        public void Clear() => _callback = null;

        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
            _callback?.Invoke(arg1, arg2, arg3, arg4, arg5);
    }
}