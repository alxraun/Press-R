using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PressR.Graphics.GraphicObjects;
using PressR.Graphics.Tween;
using UnityEngine;
using Verse;

namespace PressR.Graphics
{
    public class GraphicsManager : IGraphicsManager
    {
        private readonly Dictionary<object, IGraphicObject> _graphicObjects = new();
        private readonly Dictionary<Guid, ITween> _activeTweens = new();
        private readonly Dictionary<object, HashSet<Guid>> _objectToActiveTweenIds = new();
        private readonly List<Guid> _finishedTweenKeysToRemove = new();

        public IGraphicObject RegisterGraphicObject(IGraphicObject graphicObject)
        {
            if (graphicObject?.Key == null)
                return null;

            object key = graphicObject.Key;

            if (_graphicObjects.TryGetValue(key, out var existingObject))
            {
                if (existingObject.State == GraphicObjectState.PendingRemoval)
                {
                    existingObject.State = GraphicObjectState.Active;
                    existingObject.OnRegistered();
                    return existingObject;
                }
                else
                {
                    return existingObject;
                }
            }
            else
            {
                graphicObject.State = GraphicObjectState.Active;
                _graphicObjects.Add(key, graphicObject);
                graphicObject.OnRegistered();
                return graphicObject;
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
            string propertyId,
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

            if (_objectToActiveTweenIds.TryGetValue(targetKey, out var existingTweenIds))
            {
                foreach (var existingTweenId in existingTweenIds)
                {
                    if (
                        _activeTweens.TryGetValue(existingTweenId, out var existingTween)
                        && existingTween.PropertyId == propertyId
                    )
                    {
                        KillTween(existingTweenId);
                    }
                }
            }

            var tween = new Tween<TValue>(getter, setter, endValue, duration, propertyId, easing);
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
                if (!_finishedTweenKeysToRemove.Contains(tweenKey))
                {
                    _finishedTweenKeysToRemove.Add(tweenKey);
                }
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

        public bool TryGetTween(Guid tweenKey, out ITween tween)
        {
            return _activeTweens.TryGetValue(tweenKey, out tween);
        }

        public void KillAllTweensForTarget(object targetKey)
        {
            if (
                targetKey != null
                && _objectToActiveTweenIds.TryGetValue(targetKey, out var tweenIds)
            )
            {
                foreach (var tweenId in tweenIds.ToList())
                {
                    KillTween(tweenId);
                }
            }
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
                try
                {
                    graphicObject.Update();
                }
                catch (Exception ex)
                {
                    Log.Error(
                        $"[GraphicsManager] Exception during Update for key {graphicObject.Key}: {ex}"
                    );
                }
            }

            var keysToRemoveNow = new List<object>();
            foreach (var kvp in _graphicObjects)
            {
                object key = kvp.Key;
                IGraphicObject graphicObject = kvp.Value;

                if (graphicObject.State == GraphicObjectState.PendingRemoval)
                {
                    bool hasActiveTweens =
                        _objectToActiveTweenIds.TryGetValue(key, out var tweenIds)
                        && tweenIds.Count > 0;

                    if (!hasActiveTweens)
                    {
                        keysToRemoveNow.Add(key);
                    }
                }
            }

            foreach (var keyToRemove in keysToRemoveNow)
            {
                if (_graphicObjects.TryGetValue(keyToRemove, out var graphicObjectToDispose))
                {
                    try
                    {
                        graphicObjectToDispose.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"[GraphicsManager] Exception during Dispose for key {keyToRemove}: {ex}"
                        );
                    }
                    _graphicObjects.Remove(keyToRemove);
                    _objectToActiveTweenIds.Remove(keyToRemove);
                }
            }
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
                catch (Exception ex)
                {
                    Log.Error(
                        $"[GraphicsManager] Exception during Dispose in Clear for key {kvp.Key}: {ex}"
                    );
                }
            }

            _graphicObjects.Clear();
            _activeTweens.Clear();
            _objectToActiveTweenIds.Clear();
            _finishedTweenKeysToRemove.Clear();
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
