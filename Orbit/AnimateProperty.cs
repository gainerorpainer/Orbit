using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Orbit
{
    class AnimateProperty<Tobj, Tprop>
    {
        public PropertyInfo PropInfo { get; private set; }
        public Tobj Object { get; private set; }
        public object Lock { get; private set; }
        public int Steps { get; private set; }
        public int Framerate { get; private set; }
        public AnimateProperty(Tobj @object, Expression<Func<Tobj, string>> propSelector, int steps, int framerate, object @lock = null)
        {
            Object = @object;
            PropInfo = (PropertyInfo)((MemberExpression)propSelector.Body).Member;
            Steps = steps;
            Framerate = framerate;

            Lock = @lock is null ? new object() : @lock;
        }


        private void AnimateLoop()
        {
            for (int i = 0; i < Steps; i++)
            {
                lock (Lock)
                {
                    PropInfo.SetValue(Object, 0, null);
                }
            }
        }
    }
}
