using Microtools.Graphics;
using Verse;

namespace Microtools.Features.DirectHaul
{
    public class DirectHaul : IMicrotoolsFeature, IExposable
    {
        private readonly PlacementService _placementService;
        private readonly PlacementManager _placementManager;
        private readonly ActionDispatcher _actionDispatcher;
        private readonly GraphicsController_Ghost _graphicsController_Ghost;
        private readonly GraphicsController_RadiusIndicator _graphicsController_RadiusIndicator;
        private readonly GraphicsController_StatusOverlay _graphicsController_StatusOverlay;
        private readonly Input _input;
        private readonly DragSound _dragAudio;
        private readonly PersistentData _persistentData;

        public State State { get; } = new();
        public ThingStateManager ThingStateManager { get; }
        public bool IsActive { get; private set; }

        public DirectHaul(IGraphicsManager graphicsManager, Input input)
        {
            _input = input;
            _persistentData = new PersistentData();
            ThingStateManager = new ThingStateManager(State);
            _placementService = new PlacementService();
            _actionDispatcher = new ActionDispatcher(ThingStateManager, State);

            _placementManager = new PlacementManager(
                _placementService,
                ThingStateManager,
                State,
                _input
            );
            _graphicsController_Ghost = new GraphicsController_Ghost(
                graphicsManager,
                State,
                ThingStateManager
            );
            _graphicsController_RadiusIndicator = new GraphicsController_RadiusIndicator(
                graphicsManager,
                State,
                ThingStateManager,
                _input
            );
            _graphicsController_StatusOverlay = new GraphicsController_StatusOverlay(
                graphicsManager,
                State,
                ThingStateManager
            );
            _dragAudio = new DragSound(
                RimWorld.SoundDefOf.Designate_DragAreaAdd,
                RimWorld.SoundDefOf.Designate_DragZone_Changed
            );
        }

        public void Update()
        {
            _placementManager.Update();

            _graphicsController_Ghost.Update();
            _graphicsController_RadiusIndicator.Update();

            if (_input.IsDragStart)
            {
                _dragAudio.Start();
            }

            if (_input.IsDragging)
            {
                _dragAudio.Update(_input.IsDragChanged);
            }

            if (_input.IsDragEnd)
            {
                _dragAudio.End();
            }

            if (_input.IsClick || _input.IsDrag)
            {
                _actionDispatcher.PlaceItems();
            }
        }

        public void ConstantUpdate()
        {
            _graphicsController_StatusOverlay.Update();
        }

        public void ConstantClear()
        {
            _graphicsController_StatusOverlay.Clear();
        }

        public bool CanActivate()
        {
            if (!MicrotoolsMod.Settings.enableDirectHaul)
            {
                return false;
            }

            if (_input.IsModifierIncrement10xKeyDown)
            {
                return false;
            }

            if (IsActive && State.CurrentMap != Find.CurrentMap)
            {
                return false;
            }

            Utils.GetSelectedHaulableThings(State.SelectedThings);
            return State.SelectedThings.Any();
        }

        public void Activate()
        {
            IsActive = true;
            State.CurrentMap = Find.CurrentMap;
        }

        public void Deactivate()
        {
            IsActive = false;
            State.Clear();
            _placementManager.Reset();
            _graphicsController_Ghost.Clear();
            _graphicsController_RadiusIndicator.Clear();
            _dragAudio.End();
        }

        public void ExposeData()
        {
            _persistentData.ExposeData(State);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ThingStateManager.RecalculateCaches();
            }
        }
    }
}
