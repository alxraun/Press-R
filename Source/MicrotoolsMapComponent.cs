using System.Collections.Generic;
using Microtools.Features;
using Microtools.Features.DirectHaul;
using Microtools.Features.DirectHaulStorageMode;
using Microtools.Features.StorageLens;
using Microtools.Graphics;
using RimWorld.Planet;
using Verse;

namespace Microtools
{
    public class MicrotoolsMapComponent : MapComponent
    {
        public IGraphicsManager GraphicsManager { get; }
        public DirectHaul DirectHaul { get; private set; }
        public DirectHaulStorageMode DirectHaulStorageMode { get; private set; }
        public StorageLens StorageLens { get; private set; }

        public bool IsActive { get; private set; }

        private readonly List<IMicrotoolsFeature> _features = [];
        private IMicrotoolsFeature _activeFeature;
        private readonly Input _input;
        private bool _shouldCaptureInput;

        public MicrotoolsMapComponent(Map map)
            : base(map)
        {
            GraphicsManager = new GraphicsManager();
            _input = new Input();

            DirectHaul = new DirectHaul(GraphicsManager, _input);
            DirectHaulStorageMode = new DirectHaulStorageMode(GraphicsManager, _input);
            StorageLens = new StorageLens(GraphicsManager, _input);

            _features.Add(StorageLens);
            _features.Add(DirectHaulStorageMode);
            _features.Add(DirectHaul);
        }

        public override void MapComponentOnGUI()
        {
            if (!IsActive)
            {
                return;
            }

            if (_input.IsMicrotoolsModifierKeyPressed && _shouldCaptureInput)
            {
                _input.OnGUI();
            }
        }

        public override void MapComponentUpdate()
        {
            bool shouldBeActive = Find.CurrentMap == map && !WorldRendererUtility.WorldSelected;

            if (!shouldBeActive)
            {
                if (IsActive)
                {
                    DeactivateActiveFeature();
                    foreach (var feature in _features)
                    {
                        feature.ConstantClear();
                    }
                    IsActive = false;
                }
                _shouldCaptureInput = false;
                return;
            }

            if (!IsActive)
            {
                IsActive = true;
            }

            foreach (var feature in _features)
            {
                feature.ConstantUpdate();
            }

            _shouldCaptureInput = false;
            bool isMicrotoolsPressed = _input.IsMicrotoolsModifierKeyPressed;

            if (!isMicrotoolsPressed)
            {
                DeactivateActiveFeature();
            }
            else
            {
                if (_activeFeature == null)
                {
                    var featureToActivate = FindFirstActivatableFeature();
                    if (featureToActivate != null)
                    {
                        _activeFeature = featureToActivate;
                        _activeFeature.Activate();
                    }
                }

                if (_activeFeature != null)
                {
                    if (_activeFeature.CanActivate())
                    {
                        _activeFeature.Update();
                        _shouldCaptureInput = true;
                    }
                    else
                    {
                        DeactivateActiveFeature();
                    }
                }
            }

            GraphicsManager?.UpdateTweens();
            GraphicsManager?.UpdateGraphicObjects();
            GraphicsManager?.RenderGraphicObjects();
        }

        private void DeactivateActiveFeature()
        {
            if (_activeFeature == null)
            {
                return;
            }
            _activeFeature.Deactivate();
            _activeFeature = null;
            _input.Reset();
        }

        private IMicrotoolsFeature FindFirstActivatableFeature()
        {
            foreach (var feature in _features)
            {
                if (feature.CanActivate())
                {
                    return feature;
                }
            }
            return null;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe.EnterNode("DirectHaul");
            DirectHaul.ExposeData();
            Scribe.ExitNode();
        }
    }
}
