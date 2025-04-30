using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PressR.Graphics.Effects;
using PressR.Graphics.GraphicObjects;
using UnityEngine;

namespace PressR.Graphics
{
    public class GraphicsManager : IGraphicsManager
    {
        private readonly Dictionary<object, IGraphicObject> _graphicObjects = new();
        private readonly Dictionary<Guid, IEffect> _activeEffects = new();
        private readonly Dictionary<object, HashSet<Guid>> _objectToActiveEffectIds = new();

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

                    if (_objectToActiveEffectIds.TryGetValue(key, out var effectIds))
                    {
                        foreach (var effectId in effectIds.ToList())
                        {
                            StopEffect(effectId);
                        }
                    }
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
                return true;
            }
        }

        public bool UnregisterGraphicObject(object key, bool force = false)
        {
            if (key == null || !_graphicObjects.TryGetValue(key, out var graphicObject))
                return false;

            if (force)
            {
                ForceRemoveGraphicObject(key);
                return true;
            }
            else
            {
                if (graphicObject.State == GraphicObjectState.PendingRemoval)
                    return true;

                graphicObject.State = GraphicObjectState.PendingRemoval;

                return true;
            }
        }

        public bool TryGetGraphicObject(object key, out IGraphicObject graphicObject)
        {
            return _graphicObjects.TryGetValue(key, out graphicObject);
        }

        public IReadOnlyDictionary<object, IGraphicObject> GetActiveGraphicObjects()
        {
            var activeObjects = _graphicObjects
                .Where(kvp => kvp.Value.State == GraphicObjectState.Active)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            return new ReadOnlyDictionary<object, IGraphicObject>(activeObjects);
        }

        public Guid ApplyEffect(IEnumerable<object> targetKeys, IEffect effectInstance)
        {
            if (effectInstance == null || targetKeys == null)
                throw new ArgumentNullException(
                    nameof(effectInstance) + " or " + nameof(targetKeys)
                );

            Guid effectId = effectInstance.Key;

            if (
                _activeEffects.TryGetValue(effectId, out var existingEffect)
                && existingEffect.State != EffectState.PendingRemoval
            )
            {
                return Guid.Empty;
            }

            effectInstance.State = EffectState.Active;
            _activeEffects[effectId] = effectInstance;

            bool appliedToAnyTarget = false;
            foreach (var key in targetKeys.ToList())
            {
                if (TryGetGraphicObject(key, out var targetObject))
                {
                    if (targetObject.State != GraphicObjectState.PendingRemoval)
                    {
                        AddLink(targetObject, effectInstance);
                        effectInstance.OnAttach(targetObject);
                        appliedToAnyTarget = true;
                    }
                }
            }

            if (!appliedToAnyTarget)
            {
                _activeEffects.Remove(effectId);
                return Guid.Empty;
            }

            return effectId;
        }

        public bool StopEffect(Guid effectId)
        {
            if (_activeEffects.TryGetValue(effectId, out var effect))
            {
                if (effect.State == EffectState.PendingRemoval)
                    return false;

                effect.State = EffectState.PendingRemoval;
                return true;
            }
            return false;
        }

        public IReadOnlyList<IEffect> GetEffectsForTarget(object targetKey)
        {
            if (_objectToActiveEffectIds.TryGetValue(targetKey, out var effectIds))
            {
                return effectIds
                    .Select(id => _activeEffects.TryGetValue(id, out var effect) ? effect : null)
                    .Where(effect => effect != null && effect.State == EffectState.Active)
                    .ToList()
                    .AsReadOnly();
            }
            return Array.Empty<IEffect>();
        }

        public void Update()
        {
            if (_graphicObjects.Count == 0)
                return;

            UpdateEffects();
            UpdateGraphicObjects();
            ProcessRemovals();
        }

        private void UpdateEffects()
        {
            float deltaTime = Time.deltaTime;
            foreach (var effect in _activeEffects.Values.ToList())
            {
                if (effect.State == EffectState.Active)
                {
                    effect.Update(deltaTime);
                }
            }
        }

        private void UpdateGraphicObjects()
        {
            foreach (var graphicObject in _graphicObjects.Values.ToList())
            {
                graphicObject.Update();
            }
        }

        public void RenderGraphicObjects()
        {
            if (_graphicObjects.Count == 0)
                return;

            foreach (var graphicObject in _graphicObjects.Values.ToList())
            {
                if (
                    graphicObject.State == GraphicObjectState.Active
                    || graphicObject.State == GraphicObjectState.PendingRemoval
                )
                {
                    graphicObject.Render();
                }
            }
        }

        public void Clear()
        {
            foreach (Guid effectId in _activeEffects.Keys.ToList())
            {
                ForceRemoveEffect(effectId);
            }

            foreach (var key in _graphicObjects.Keys.ToList())
            {
                ForceRemoveGraphicObject(key);
            }

            _graphicObjects.Clear();
            _activeEffects.Clear();
            _objectToActiveEffectIds.Clear();
        }

        private void ProcessRemovals()
        {
            foreach (var effect in _activeEffects.Values.ToList())
            {
                if (effect.State == EffectState.PendingRemoval || effect.IsFinished)
                {
                    ForceRemoveEffect(effect.Key);
                }
            }

            foreach (var kvp in _graphicObjects.ToList())
            {
                if (
                    kvp.Value.State == GraphicObjectState.PendingRemoval
                    && !_objectToActiveEffectIds.ContainsKey(kvp.Key)
                )
                {
                    ForceRemoveGraphicObject(kvp.Key);
                }
            }
        }

        private void ForceRemoveGraphicObject(object key)
        {
            if (_graphicObjects.TryGetValue(key, out var graphicObject))
            {
                if (_objectToActiveEffectIds.TryGetValue(key, out var effectIds))
                {
                    foreach (var effectId in effectIds.ToList())
                    {
                        if (_activeEffects.TryGetValue(effectId, out var effect))
                        {
                            RemoveLink(graphicObject, effect);
                            effect.OnDetach(graphicObject);
                            if (
                                effect.Targets.Count == 0
                                && effect.IsFinished
                                && effect.State == EffectState.Active
                            )
                            {
                                effect.State = EffectState.PendingRemoval;
                            }
                        }
                    }
                    _objectToActiveEffectIds.Remove(key);
                }

                graphicObject.Dispose();

                _graphicObjects.Remove(key);
            }
        }

        private void ForceRemoveEffect(Guid effectId)
        {
            if (!_activeEffects.TryGetValue(effectId, out var effect))
            {
                return;
            }

            var targetsToDetach = effect.Targets.ToList();
            foreach (var target in targetsToDetach)
            {
                RemoveLink(target, effect);
                effect.OnDetach(target);
            }

            effect.Targets.Clear();

            _activeEffects.Remove(effectId);
        }

        private void AddLink(IGraphicObject obj, IEffect effect)
        {
            if (
                obj.State == GraphicObjectState.PendingRemoval
                || effect.State == EffectState.PendingRemoval
            )
                return;

            if (!effect.Targets.Contains(obj))
            {
                effect.Targets.Add(obj);
            }

            if (!_objectToActiveEffectIds.TryGetValue(obj.Key, out var effectIds))
            {
                effectIds = new HashSet<Guid>();
                _objectToActiveEffectIds[obj.Key] = effectIds;
            }
            effectIds.Add(effect.Key);
        }

        private void RemoveLink(IGraphicObject obj, IEffect effect)
        {
            effect.Targets.Remove(obj);

            if (_objectToActiveEffectIds.TryGetValue(obj.Key, out var effectIds))
            {
                effectIds.Remove(effect.Key);
                if (effectIds.Count == 0)
                {
                    _objectToActiveEffectIds.Remove(obj.Key);
                }
            }
        }
    }
}
