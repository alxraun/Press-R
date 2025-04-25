using System;
using System.Collections.Generic;
using PressR;
using PressR.Features.TabLens.Lenses.StorageLens;
using PressR.Graphics.Interfaces;
using PressR.Interfaces;

namespace PressR.Features.TabLens
{
    public class TabLensFeature : IPressRFeature
    {
        public string FeatureId => "TabLens";
        public string Label => "Tab Lens";

        public bool IsActive { get; private set; }

        private ILens _activeLens;
        private readonly Dictionary<string, ILens> _lenses = new Dictionary<string, ILens>();
        private readonly IGraphicsManager _graphicsManager;

        private bool IsFeatureEnabled => PressRMod.Settings.enableTabLens;

        public TabLensFeature(IGraphicsManager graphicsManager)
        {
            _graphicsManager =
                graphicsManager ?? throw new ArgumentNullException(nameof(graphicsManager));
            RegisterLens(new StorageLens(_graphicsManager));
        }

        public ILens ActiveLens => _activeLens;

        public bool TryActivate()
        {
            if (!IsFeatureEnabled)
                return false;

            foreach (var lens in _lenses.Values)
            {
                if (lens.TryActivate())
                {
                    _activeLens = lens;
                    IsActive = true;
                    return true;
                }
            }

            _activeLens = null;
            IsActive = false;
            return false;
        }

        public void Deactivate()
        {
            if (_activeLens != null)
            {
                _activeLens.Deactivate();
            }

            _activeLens = null;
            IsActive = false;
        }

        public void Update()
        {
            if (!IsActive || _activeLens == null || !IsFeatureEnabled)
            {
                if (!IsFeatureEnabled && IsActive)
                    Deactivate();
                return;
            }

            _activeLens.Update();

            if (!_activeLens.IsActive)
            {
                Deactivate();
            }
        }

        public void ConstantUpdate()
        {
            if (!IsFeatureEnabled)
                return;
        }

        private void RegisterLens(ILens lens)
        {
            if (lens != null && !_lenses.ContainsKey(lens.LensId))
            {
                _lenses.Add(lens.LensId, lens);
            }
        }
    }
}
