using PressR.Graphics.GraphicObjects;
using UnityEngine;
using Verse;
using static Verse.UI;

namespace PressR.Features.DirectHaul.Graphics
{
    public class DirectHaulRadiusIndicatorGraphicObject
        : IGraphicObject,
            IHasPosition,
            IHasAlpha,
            IHasColor,
            IHasRadius,
            IEffectTarget
    {
        private const float DefaultAlpha = 0.3f;
        private static readonly Color DefaultColor = Color.white;
        private readonly Material _lineMaterialInstance;

        public object Key => typeof(DirectHaulRadiusIndicatorGraphicObject);
        public GraphicObjectState State { get; set; } = GraphicObjectState.Active;
        public Vector3 Position { get; set; }
        public float Alpha { get; set; } = DefaultAlpha;
        public Color Color { get; set; } = DefaultColor;
        public float Radius { get; set; }
        public float Altitude => AltitudeLayer.MetaOverlays.AltitudeFor();

        public DirectHaulRadiusIndicatorGraphicObject(float initialRadius)
        {
            Radius = initialRadius;
            _lineMaterialInstance = new Material(
                MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.MetaOverlay, Color.white)
            );
        }

        public void Update()
        {
            Position = MouseMapPosition();
            Position = new Vector3(Position.x, Altitude, Position.z);
        }

        public void Render()
        {
            Color finalColor = Color;
            finalColor.a = Alpha;

            _lineMaterialInstance.color = finalColor;

            GenDraw.DrawCircleOutline(Position, Radius, _lineMaterialInstance);
        }

        public void Dispose()
        {
            if (_lineMaterialInstance != null)
            {
                UnityEngine.Object.Destroy(_lineMaterialInstance);
            }
        }
    }
}
