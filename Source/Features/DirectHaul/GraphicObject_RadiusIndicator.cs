using Microtools.Graphics;
using UnityEngine;
using Verse;
using static Verse.UI;

namespace Microtools.Features.DirectHaul
{
    public sealed class GraphicObject_RadiusIndicator(float initialRadius)
        : IGraphicObject,
            IHasPosition,
            IHasAlpha,
            IHasColor,
            IHasRadius
    {
        private const float DefaultAlpha = 0.3f;
        private static readonly Color DefaultColor = Color.white;
        private readonly Material _lineMaterialInstance = new(
            MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.MetaOverlay, Color.white)
        );
        private Color _cachedMaterialColor = Color.clear;

        public object Key => typeof(GraphicObject_RadiusIndicator);
        public GraphicObjectState State { get; set; } = GraphicObjectState.Active;
        public Vector3 Position { get; set; }
        public float Alpha { get; set; } = DefaultAlpha;
        public Color Color { get; set; } = DefaultColor;
        public float Radius { get; set; } = initialRadius;
        public float Altitude => AltitudeLayer.MetaOverlays.AltitudeFor();

        public void OnRegistered() { }

        public void Update()
        {
            Position = MouseMapPosition();
            Position = new Vector3(Position.x, Altitude, Position.z);

            Color finalColor = Color;
            finalColor.a = Alpha;

            if (finalColor != _cachedMaterialColor)
            {
                _lineMaterialInstance.color = finalColor;
                _cachedMaterialColor = finalColor;
            }
        }

        public void Render()
        {
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
