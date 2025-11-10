using Microtools.Graphics;
using RimWorld;
using Verse;

namespace Microtools.Features.DirectHaulStorageMode
{
    public class DirectHaulStorageMode : IMicrotoolsFeature, IExposable
    {
        private readonly Input _input;
        private readonly InputHandler _inputHandler;
        private readonly ActionDispatcher _actionDispatcher;
        private readonly GraphicsController_StockpileRect _graphicsControllerStockpileRect;
        private readonly GraphicsController_StorageHighlight _graphicsControllerStorageHighlight;
        private readonly DragSound _dragAudio;

        public State State { get; } = new();
        public bool IsActive { get; private set; }

        public DirectHaulStorageMode(IGraphicsManager graphicsManager, Input input)
        {
            _actionDispatcher = new ActionDispatcher(State);
            _input = input;
            _inputHandler = new InputHandler(State, _actionDispatcher);
            _graphicsControllerStockpileRect = new GraphicsController_StockpileRect(
                graphicsManager,
                _actionDispatcher,
                _input
            );
            _graphicsControllerStorageHighlight = new GraphicsController_StorageHighlight(
                graphicsManager,
                State,
                _actionDispatcher,
                _input
            );
            _dragAudio = new DragSound(
                SoundDefOf.Designate_DragAreaAdd,
                SoundDefOf.Designate_DragZone_Changed
            );
        }

        public void Update()
        {
            _graphicsControllerStockpileRect.Update();
            _graphicsControllerStorageHighlight.Update();

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

            if (_input.IsClick)
            {
                _inputHandler.HandleClick(_input.StartDragCell);
            }
            else if (_input.IsDrag)
            {
                _inputHandler.HandleDrag(_input.StartDragCell, _input.CurrentDragCell);
            }
        }

        public bool CanActivate()
        {
            if (!MicrotoolsMod.Settings.enableDirectHaul)
            {
                return false;
            }

            if (!_input.IsModifierIncrement10xKeyDown)
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
            _dragAudio.End();
            State.Clear();
            _graphicsControllerStockpileRect.Clear();
            _graphicsControllerStorageHighlight.Clear();
        }

        public void ExposeData() { }
    }
}
