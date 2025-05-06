using System;
using PressR.Graphics;
using UnityEngine;
using Verse;

namespace PressR.Graphics.GraphicObjects.GraphicObjects
{
    public class ScreenDesaturatorGraphicObject : IGraphicObject, IHasSaturation, IHasAlpha
    {
        public GraphicObjectState State { get; set; } = GraphicObjectState.Active;
        public float Saturation { get; set; }
        public float Alpha { get; set; }
        public object Key => "ScreenDesaturator";

        public void OnRegistered() { }

        public void Update() { }

        public void Render() { }

        public void Dispose() { }
    }
}
