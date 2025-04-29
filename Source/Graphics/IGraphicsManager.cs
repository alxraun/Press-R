using System;
using System.Collections.Generic;
using PressR.Graphics.Effects;
using PressR.Graphics.GraphicObjects;

namespace PressR.Graphics
{
    public interface IGraphicsManager
    {
        bool RegisterGraphicObject(IGraphicObject graphicObject);

        bool UnregisterGraphicObject(object key, bool force = false);

        bool TryGetGraphicObject(object key, out IGraphicObject graphicObject);

        Guid ApplyEffect(IEnumerable<object> targetKeys, IEffect effectPrototype);

        bool StopEffect(Guid effectId);

        IReadOnlyList<IEffect> GetEffectsForTarget(object targetKey);

        IReadOnlyDictionary<object, IGraphicObject> GetActiveGraphicObjects();

        void Update();

        void RenderGraphicObjects();

        void Clear();
    }
}
