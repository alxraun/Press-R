using System;
using System.Collections.Generic;
using PressR.Graphics.GraphicObjects;
using PressR.Graphics.Tween;

namespace PressR.Graphics
{
    public interface IGraphicsManager
    {
        IGraphicObject RegisterGraphicObject(IGraphicObject graphicObject);

        bool UnregisterGraphicObject(object key);

        bool TryGetGraphicObject(object key, out IGraphicObject graphicObject);

        IReadOnlyDictionary<object, IGraphicObject> GetAllGraphicObjects();

        Guid ApplyTween<TValue>(
            object targetKey,
            Func<TValue> getter,
            Action<TValue> setter,
            TValue endValue,
            float duration,
            string propertyId,
            EasingFunction easing = null,
            Action onComplete = null
        );

        bool KillTween(Guid tweenKey);

        bool CompleteTween(Guid tweenKey);

        bool TryGetTween(Guid tweenKey, out ITween tween);

        void KillAllTweensForTarget(object targetKey);

        void UpdateTweens();

        void UpdateGraphicObjects();

        void RenderGraphicObjects();

        void Clear();
    }
}
