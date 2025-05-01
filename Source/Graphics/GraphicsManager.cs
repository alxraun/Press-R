using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PressR.Graphics.GraphicObjects;
using PressR.Graphics.Tween;
using UnityEngine;

namespace PressR.Graphics
{
    public class GraphicsManager : IGraphicsManager
    {
        private readonly Dictionary<object, IGraphicObject> _graphicObjects = new();
        private readonly Dictionary<Guid, ITween> _activeTweens = new();
        private readonly Dictionary<object, HashSet<Guid>> _objectToActiveTweenIds = new();
        private readonly List<Guid> _finishedTweenKeysToRemove = new();
        private readonly List<object> _graphicObjectsToRemove = new();

        public bool RegisterGraphicObject(IGraphicObject graphicObject)
        {
            if (graphicObject?.Key == null)
                return false;

            object key = graphicObject.Key;

            if (_graphicObjects.TryGetValue(key, out var existingObject))
            {
                if (existingObject.State == GraphicObjectState.PendingRemoval)
                {
                    existingObject.State = GraphicObjectState.Active;
                    existingObject.OnRegistered();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                graphicObject.State = GraphicObjectState.Active;
                _graphicObjects.Add(key, graphicObject);
                graphicObject.OnRegistered();
                return true;
            }
        }

        public bool UnregisterGraphicObject(object key)
        {
            if (key == null || !_graphicObjects.TryGetValue(key, out var graphicObject))
                return false;

            if (graphicObject.State == GraphicObjectState.PendingRemoval)
                return true;

            graphicObject.State = GraphicObjectState.PendingRemoval;
            return true;
        }

        public bool TryGetGraphicObject(object key, out IGraphicObject graphicObject)
        {
            return _graphicObjects.TryGetValue(key, out graphicObject);
        }

        public IReadOnlyDictionary<object, IGraphicObject> GetAllGraphicObjects()
        {
            return new ReadOnlyDictionary<object, IGraphicObject>(_graphicObjects);
        }

        public Guid ApplyTween<TValue>(
            object targetKey,
            Func<TValue> getter,
            Action<TValue> setter,
            TValue endValue,
            float duration,
            EasingFunction easing = null,
            Action onComplete = null
        )
        {
            if (
                targetKey == null
                || !_graphicObjects.TryGetValue(targetKey, out var targetObject)
                || targetObject.State != GraphicObjectState.Active
            )
            {
                return Guid.Empty;
            }

            var tween = new Tween<TValue>(getter, setter, endValue, duration, easing);
            Guid tweenKey = tween.Key;

            tween.OnComplete = () =>
            {
                try
                {
                    onComplete?.Invoke();
                }
                finally
                {
                    _finishedTweenKeysToRemove.Add(tweenKey);
                }
            };

            _activeTweens.Add(tweenKey, tween);

            if (!_objectToActiveTweenIds.TryGetValue(targetKey, out var tweenIds))
            {
                tweenIds = new HashSet<Guid>();
                _objectToActiveTweenIds[targetKey] = tweenIds;
            }
            tweenIds.Add(tweenKey);

            return tweenKey;
        }

        public bool KillTween(Guid tweenKey)
        {
            if (_activeTweens.TryGetValue(tweenKey, out var tween))
            {
                tween.Kill();
                _finishedTweenKeysToRemove.Add(tweenKey);
                return true;
            }
            return false;
        }

        public bool CompleteTween(Guid tweenKey)
        {
            if (_activeTweens.TryGetValue(tweenKey, out var tween))
            {
                tween.Complete();
                return true;
            }
            return false;
        }

        public void UpdateTweens()
        {
            if (_activeTweens.Count == 0 && _finishedTweenKeysToRemove.Count == 0)
                return;

            foreach (var keyToRemove in _finishedTweenKeysToRemove)
            {
                if (_activeTweens.Remove(keyToRemove))
                {
                    RemoveTweenFromObjectLinks(keyToRemove);
                }
            }
            _finishedTweenKeysToRemove.Clear();

            float deltaTime = Time.deltaTime;
            foreach (var tween in _activeTweens.Values.ToList())
            {
                tween.Update(deltaTime);
            }
        }

        public void UpdateGraphicObjects()
        {
            if (_graphicObjects.Count == 0)
                return;

            foreach (var graphicObject in _graphicObjects.Values.ToList())
            {
                graphicObject.Update();
            }

            foreach (var kvp in _graphicObjects)
            {
                object key = kvp.Key;
                IGraphicObject graphicObject = kvp.Value;

                if (graphicObject.State == GraphicObjectState.PendingRemoval)
                {
                    if (
                        !_objectToActiveTweenIds.TryGetValue(key, out var tweenIds)
                        || tweenIds.Count == 0
                    )
                    {
                        _graphicObjectsToRemove.Add(key);
                    }
                }
            }

            foreach (var keyToRemove in _graphicObjectsToRemove)
            {
                if (_graphicObjects.TryGetValue(keyToRemove, out var graphicObject))
                {
                    try
                    {
                        graphicObject.Dispose();
                    }
                    catch (Exception) { }
                }
            }
            _graphicObjectsToRemove.Clear();
        }

        public void RenderGraphicObjects()
        {
            if (_graphicObjects.Count == 0)
                return;

            foreach (var graphicObject in _graphicObjects.Values.ToList())
            {
                graphicObject.Render();
            }
        }

        public void Clear()
        {
            foreach (Guid tweenKey in _activeTweens.Keys.ToList())
            {
                KillTween(tweenKey);
            }
            UpdateTweens();

            foreach (var kvp in _graphicObjects.ToList())
            {
                try
                {
                    kvp.Value.Dispose();
                }
                catch (Exception) { }
            }

            _graphicObjects.Clear();
            _activeTweens.Clear();
            _objectToActiveTweenIds.Clear();
            _finishedTweenKeysToRemove.Clear();
            _graphicObjectsToRemove.Clear();
        }

        private void RemoveTweenFromObjectLinks(Guid tweenKey)
        {
            List<object> keysToRemoveLinkFrom = new List<object>();
            foreach (var kvp in _objectToActiveTweenIds)
            {
                if (kvp.Value.Contains(tweenKey))
                {
                    kvp.Value.Remove(tweenKey);
                    if (kvp.Value.Count == 0)
                    {
                        keysToRemoveLinkFrom.Add(kvp.Key);
                    }
                }
            }

            foreach (var key in keysToRemoveLinkFrom)
            {
                _objectToActiveTweenIds.Remove(key);
            }
        }
    }
}
