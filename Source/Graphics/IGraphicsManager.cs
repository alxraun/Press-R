using System;
using System.Collections.Generic;
using PressR.Graphics.GraphicObjects;
using PressR.Graphics.Tween;

namespace PressR.Graphics
{
    public interface IGraphicsManager
    {
        bool RegisterGraphicObject(IGraphicObject graphicObject);

        bool UnregisterGraphicObject(object key);

        bool TryGetGraphicObject(object key, out IGraphicObject graphicObject);

        IReadOnlyDictionary<object, IGraphicObject> GetAllGraphicObjects();

        Guid ApplyTween<TValue>(
            object targetKey,
            Func<TValue> getter,
            Action<TValue> setter,
            TValue endValue,
            float duration,
            EasingFunction easing = null,
            Action onComplete = null
        );

        bool KillTween(Guid tweenKey);

        bool CompleteTween(Guid tweenKey);

        void UpdateTweens();

        void UpdateGraphicObjects();

        void RenderGraphicObjects();

        void Clear();
    }
}
