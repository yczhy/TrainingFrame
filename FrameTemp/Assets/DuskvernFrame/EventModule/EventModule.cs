using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Duskvern
{
    public static class EventModule
    {
        private static readonly List<EventCallback> _callbacks = new();

        private static void EnSure(int index)
        {
            while (_callbacks.Count <= index)
            {
                _callbacks.Add(null);
            }
        }

        public static void Add(EventNode evt, Action callback)
        {
            EnSure(evt.index);
            if (_callbacks[evt.index] is not EventCallback_0 ecb)
            {
                _callbacks[evt.index] = ecb = new EventCallback_0();
            }

            ecb.Add(callback);
        }

        public static void Remove(EventNode evt, Action callback)
        {
            EnSure(evt.index);
            if (_callbacks[evt.index] is not EventCallback_0 ecb)
            {
                _callbacks[evt.index] = ecb = new EventCallback_0();
            }
            ecb.Remove(callback);
            if (ecb.IsEmpty)
            {
                _callbacks[evt.index] = null;
            }
        }

        public static void Switch(EventNode evt, Action callback, bool add)
        {
            EnSure(evt.index);
            if (add)
            {
                Add(evt, callback);
            }
            else
            {
                Remove(evt, callback);
            }
        }

        public static void Trigger(EventNode evt)
        {
            if (evt.index >= _callbacks.Count)
            {
                return;
            }

            if (_callbacks[evt.index] is EventCallback_0 ecb)
            {
                ecb.Invoke();
            }
        }
        
        public static void Add<T1>(EventNode<T1> evt, Action<T1> callback)
        {
            EnSure(evt.index);
            if (_callbacks[evt.index] is not EventCallback_1<T1> ecb)
            {
                _callbacks[evt.index] = ecb = new EventCallback_1<T1>();
            }
            ecb.Add(callback);
        }

        public static void Remove<T1>(EventNode<T1> evt, Action<T1> callback)
        {
            EnSure(evt.index);
            if (_callbacks[evt.index] is EventCallback_1<T1> ecb)
            {
                ecb.Remove(callback);
                if (ecb.IsEmpty)
                {
                    _callbacks[evt.index] = null;
                }
            }
        }

        public static void Trigger<T1>(EventNode<T1> evt, T1 arg1)
        {
            if (evt.index >= _callbacks.Count) return;
            if (_callbacks[evt.index] is EventCallback_1<T1> ecb)
            {
                ecb.Invoke(arg1);
            }
        }
        
        public static void Add<T1, T2>(EventNode<T1, T2> evt, Action<T1, T2> callback)
        {
            EnSure(evt.index);
            if (_callbacks[evt.index] is not EventCallback_2<T1, T2> ecb)
            {
                _callbacks[evt.index] = ecb = new EventCallback_2<T1, T2>();
            }
            ecb.Add(callback);
        }

        public static void Remove<T1, T2>(EventNode<T1, T2> evt, Action<T1, T2> callback)
        {
            EnSure(evt.index);
            if (_callbacks[evt.index] is EventCallback_2<T1, T2> ecb)
            {
                ecb.Remove(callback);
                if (ecb.IsEmpty)
                {
                    _callbacks[evt.index] = null;
                }
            }
        }

        public static void Trigger<T1, T2>(EventNode<T1, T2> evt, T1 arg1, T2 arg2)
        {
            if (evt.index >= _callbacks.Count) return;
            if (_callbacks[evt.index] is EventCallback_2<T1, T2> ecb)
            {
                ecb.Invoke(arg1, arg2);
            }
        }
        
        public static void Add<T1, T2, T3>(EventNode<T1, T2, T3> evt, Action<T1, T2, T3> callback)
        {
            EnSure(evt.index);
            if (_callbacks[evt.index] is not EventCallback_3<T1, T2, T3> ecb)
            {
                _callbacks[evt.index] = ecb = new EventCallback_3<T1, T2, T3>();
            }
            ecb.Add(callback);
        }

        public static void Remove<T1, T2, T3>(EventNode<T1, T2, T3> evt, Action<T1, T2, T3> callback)
        {
            EnSure(evt.index);
            if (_callbacks[evt.index] is EventCallback_3<T1, T2, T3> ecb)
            {
                ecb.Remove(callback);
                if (ecb.IsEmpty)
                {
                    _callbacks[evt.index] = null;
                }
            }
        }

        public static void Trigger<T1, T2, T3>(EventNode<T1, T2, T3> evt, T1 arg1, T2 arg2, T3 arg3)
        {
            if (evt.index >= _callbacks.Count) return;
            if (_callbacks[evt.index] is EventCallback_3<T1, T2, T3> ecb)
            {
                ecb.Invoke(arg1, arg2, arg3);
            }
        }
        
        public static void Add<T1, T2, T3, T4>(EventNode<T1, T2, T3, T4> evt, Action<T1, T2, T3, T4> callback)
        {
            EnSure(evt.index);
            if (_callbacks[evt.index] is not EventCallback_4<T1, T2, T3, T4> ecb)
            {
                _callbacks[evt.index] = ecb = new EventCallback_4<T1, T2, T3, T4>();
            }
            ecb.Add(callback);
        }

        public static void Remove<T1, T2, T3, T4>(EventNode<T1, T2, T3, T4> evt, Action<T1, T2, T3, T4> callback)
        {
            EnSure(evt.index);
            if (_callbacks[evt.index] is EventCallback_4<T1, T2, T3, T4> ecb)
            {
                ecb.Remove(callback);
                if (ecb.IsEmpty)
                {
                    _callbacks[evt.index] = null;
                }
            }
        }

        public static void Trigger<T1, T2, T3, T4>(EventNode<T1, T2, T3, T4> evt, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (evt.index >= _callbacks.Count) return;
            if (_callbacks[evt.index] is EventCallback_4<T1, T2, T3, T4> ecb)
            {
                ecb.Invoke(arg1, arg2, arg3, arg4);
            }
        }
        
        public static void Add<T1, T2, T3, T4, T5>(EventNode<T1, T2, T3, T4, T5> evt, Action<T1, T2, T3, T4, T5> callback)
        {
            EnSure(evt.index);
            if (_callbacks[evt.index] is not EventCallback_5<T1, T2, T3, T4, T5> ecb)
            {
                _callbacks[evt.index] = ecb = new EventCallback_5<T1, T2, T3, T4, T5>();
            }
            ecb.Add(callback);
        }

        public static void Remove<T1, T2, T3, T4, T5>(EventNode<T1, T2, T3, T4, T5> evt, Action<T1, T2, T3, T4, T5> callback)
        {
            EnSure(evt.index);
            if (_callbacks[evt.index] is EventCallback_5<T1, T2, T3, T4, T5> ecb)
            {
                ecb.Remove(callback);
                if (ecb.IsEmpty)
                {
                    _callbacks[evt.index] = null;
                }
            }
        }

        public static void Trigger<T1, T2, T3, T4, T5>(EventNode<T1, T2, T3, T4, T5> evt, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (evt.index >= _callbacks.Count) return;
            if (_callbacks[evt.index] is EventCallback_5<T1, T2, T3, T4, T5> ecb)
            {
                ecb.Invoke(arg1, arg2, arg3, arg4, arg5);
            }
        }
        
        public static void Switch<T1>(EventNode<T1> evt, Action<T1> callback, bool add)
        {
            EnSure(evt.index);
            if (add)
            {
                Add(evt, callback);
            }
            else
            {
                Remove(evt, callback);
            }
        }
        
        public static void Switch<T1, T2>(EventNode<T1, T2> evt, Action<T1, T2> callback, bool add)
        {
            EnSure(evt.index);
            if (add)
            {
                Add(evt, callback);
            }
            else
            {
                Remove(evt, callback);
            }
        }
        
        public static void Switch<T1, T2, T3>(EventNode<T1, T2, T3> evt, Action<T1, T2, T3> callback, bool add)
        {
            EnSure(evt.index);
            if (add)
            {
                Add(evt, callback);
            }
            else
            {
                Remove(evt, callback);
            }
        }
        
        public static void Switch<T1, T2, T3, T4>(EventNode<T1, T2, T3, T4> evt, Action<T1, T2, T3, T4> callback, bool add)
        {
            EnSure(evt.index);
            if (add)
            {
                Add(evt, callback);
            }
            else
            {
                Remove(evt, callback);
            }
        }
        
        public static void Switch<T1, T2, T3, T4, T5>(EventNode<T1, T2, T3, T4, T5> evt, Action<T1, T2, T3, T4, T5> callback, bool add)
        {
            EnSure(evt.index);
            if (add)
            {
                Add(evt, callback);
            }
            else
            {
                Remove(evt, callback);
            }
        }
    }
}