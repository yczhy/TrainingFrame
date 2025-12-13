
namespace Duskvern
{
    public abstract class EventCell
    {
        private static int nextIndex;
        public readonly int index;

        protected EventCell()
        {
            index = nextIndex++;
        }
    }

    public sealed class EventNode : EventCell
    {
    
    }

    public sealed class EventNode<T1> : EventCell
    {
    
    }

    public sealed class EventNode<T1, T2> : EventCell
    {
    
    }

    public sealed class EventNode<T1, T2, T3> : EventCell
    {
    
    }

    public sealed class EventNode<T1, T2, T3, T4> : EventCell
    {
    
    }
    
    public sealed class EventNode<T1, T2, T3, T4, T5> : EventCell
    {
    
    }
}
