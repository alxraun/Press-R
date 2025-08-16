using PressR.Graphics;
using RimWorld;
using Verse;

namespace PressR.Features.StorageLens
{
    public class StorageLens : IPressRFeature
    {
        private readonly IGraphicsManager _graphicsManager;
        private readonly ThingsProvider _thingsProvider;
        private readonly AllowanceProvider _allowanceProvider;
        private readonly Input _input;
        private readonly InputHandler _inputHandler;
        private readonly UIManager _uiManager;
        private readonly ActionDispatcher _action;
        private readonly GraphicsController_ThingOverlay _graphicsController_ThingOverlay;
        private readonly Throttler _graphicsUpdateThrottler;

        private const int GraphicsUpdateIntervalTicks = 1;

        public State State { get; } = new State();
        public bool IsActive { get; private set; }

        public StorageLens(IGraphicsManager graphicsManager, Input input)
        {
            _graphicsManager = graphicsManager;
            _thingsProvider = new ThingsProvider(State);
            _allowanceProvider = new AllowanceProvider(State);
            _action = new ActionDispatcher(State);
            _input = input;
            _inputHandler = new InputHandler(State, _action, _input);
            _uiManager = new UIManager(State);
            _graphicsController_ThingOverlay = new GraphicsController_ThingOverlay(
                _graphicsManager,
                State
            );
            _graphicsUpdateThrottler = new Throttler(GraphicsUpdateIntervalTicks);
        }

        public void Update()
        {
            _thingsProvider.Update();
            _allowanceProvider.Update();
            _inputHandler.ProcessInputEvents();
            if (_graphicsUpdateThrottler.ShouldExecute())
            {
                _graphicsController_ThingOverlay.Update();
            }
        }

        public bool CanActivate()
        {
            if (!PressRMod.Settings.storageLensSettings.enableStorageLens)
            {
                if (IsActive)
                {
                    Deactivate();
                }
                return false;
            }

            if (IsActive)
            {
                var mapChanged = State.CurrentMap != Find.CurrentMap;
                var selectionChanged = State.SelectedStorage != Find.Selector.SingleSelectedObject;

                if (mapChanged || selectionChanged)
                {
                    return false;
                }

                return true;
            }

            if (Find.Selector.SingleSelectedObject is IStoreSettingsParent selectedStorage)
            {
                return true;
            }

            return false;
        }

        public void Activate()
        {
            IsActive = true;

            State.CurrentMap = Find.CurrentMap;
            State.SelectedStorage = Find.Selector.SingleSelectedObject as IStoreSettingsParent;

            _uiManager.CaptureUIState();
        }

        public void Deactivate()
        {
            IsActive = false;

            if (PressRMod.Settings.storageLensSettings.restoreUIStateOnDeactivate)
            {
                _action.SetSelection(State.UISnapshot_SelectedObject);
                _action.SetOpenTab(State.UISnapshot_OpenTabType);
                _action.SetSearchText(State.UISnapshot_StorageTabSearchText);
                _action.SetStorageTabScrollPosition(State.UISnapshot_StorageTabScrollPosition);
            }

            _graphicsController_ThingOverlay.Clear();
            State.Clear();
        }

        public void ExposeData() { }
    }
}
