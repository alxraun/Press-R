using System.Collections.Generic;
using PressR.Features;
using PressR.Features.DirectHaul;
using PressR.Features.TabLens;
using PressR.Graphics;
using UnityEngine;
using Verse;
using static PressR.PressRInput;

namespace PressR
{
    public static class PressRMain
    {
        private static readonly List<IPressRFeature> _features = new List<IPressRFeature>();
        private static readonly IGraphicsManager _graphicsManager;

        static PressRMain()
        {
            _graphicsManager = new GraphicsManager();
            RegisterFeature(new TabLensFeature(_graphicsManager));
            RegisterFeature(new DirectHaulFeature(_graphicsManager));
        }

        public static IGraphicsManager GraphicsManager => _graphicsManager;

        public static IEnumerable<IPressRFeature> Features => _features;

        public static void RegisterFeature(IPressRFeature feature)
        {
            if (feature == null || _features.Contains(feature))
            {
                return;
            }

            _features.Add(feature);
        }

        public static void MainUpdateLoop()
        {
            if (Current.ProgramState == ProgramState.Playing)
            {
                foreach (var feature in Features)
                {
                    feature.ConstantUpdate();
                }
            }

            if (!IsPressRModifierKeyPressed || Current.ProgramState != ProgramState.Playing)
            {
                foreach (var feature in Features)
                {
                    if (feature.IsActive)
                    {
                        feature.Deactivate();
                    }
                }
                return;
            }

            foreach (var feature in Features)
            {
                if (!feature.IsActive)
                {
                    feature.TryActivate();
                }
                else
                {
                    feature.Update();

                    if (!feature.IsActive)
                    {
                        feature.Deactivate();
                    }
                }
            }
        }
    }
}
