using System;

namespace PressR.Graphics.Utils.Replicator2.Core
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class DecoratorPriorityAttribute : Attribute
    {
        public int Priority { get; }

        public DecoratorPriorityAttribute(int priority)
        {
            Priority = priority;
        }
    }
}
