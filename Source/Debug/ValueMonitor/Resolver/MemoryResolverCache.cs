using System;
using System.Collections.Generic;

namespace PressR.Debug.ValueMonitor.Resolver
{
    public class MemoryResolverCache
    {
        private readonly Dictionary<string, Func<object>> _compiledGettersCache =
            new Dictionary<string, Func<object>>();
        private readonly Dictionary<string, string> _compilationErrorsCache =
            new Dictionary<string, string>();

        public bool TryGetGetter(string path, out Func<object> getter)
        {
            return _compiledGettersCache.TryGetValue(path, out getter);
        }

        public bool TryGetError(string path, out string error)
        {
            return _compilationErrorsCache.TryGetValue(path, out error);
        }

        public void AddGetter(string path, Func<object> getter)
        {
            _compiledGettersCache[path] = getter;
        }

        public void AddError(string path, string error)
        {
            _compilationErrorsCache[path] = error;
        }
    }
}
