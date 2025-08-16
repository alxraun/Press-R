using RimWorld;
using UnityEngine;
using Verse;

namespace PressR
{
    public class Input
    {
        private enum State
        {
            Idle,
            Pressed,
            Dragging,
            Released,
        }

        private State _state = State.Idle;

        public bool IsModifierIncrement10xKeyDown => KeyBindingDefOf.ModifierIncrement_10x.IsDown;
        public bool IsModifierIncrement100xKeyDown => KeyBindingDefOf.ModifierIncrement_100x.IsDown;
        public bool IsPressRModifierKeyPressed => PressRDefOf.PressR_ModifierKey.IsDown;

        public bool IsClick { get; private set; }
        public bool IsMouseDown { get; private set; }
        public bool IsDrag { get; private set; }
        public bool IsDragStart { get; private set; }
        public bool IsDragEnd { get; private set; }
        public bool IsDragChanged { get; private set; }

        public bool IsDragging => _state == State.Dragging;
        public IntVec3 StartDragCell => _startDragCell;
        public IntVec3 CurrentDragCell => _currentDragCell;
        public CellRect CurrentDragRect => CellRect.FromLimits(_startDragCell, _currentDragCell);

        private IntVec3 _startDragCell;
        private IntVec3 _currentDragCell;
        private IntVec3 _lastReportedDragCell;

        public void OnGUI()
        {
            var e = Event.current;

            if (_state == State.Released)
            {
                _state = State.Idle;
                Reset();
            }

            IsClick = false;
            IsMouseDown = false;
            IsDrag = false;
            IsDragStart = false;
            IsDragEnd = false;
            IsDragChanged = false;

            if (_state == State.Idle && e.type == EventType.MouseDown && e.button == 0)
            {
                _state = State.Pressed;
                IsMouseDown = true;
                _startDragCell = Verse.UI.MouseCell();
                _currentDragCell = _startDragCell;
                _lastReportedDragCell = _currentDragCell;
                e.Use();
                return;
            }

            if (_state == State.Pressed || _state == State.Dragging)
            {
                _currentDragCell = Verse.UI.MouseCell();
                if (_state == State.Pressed && _startDragCell != _currentDragCell)
                {
                    _state = State.Dragging;
                    IsDragStart = true;
                    _lastReportedDragCell = _currentDragCell;
                }
                else if (_state == State.Dragging)
                {
                    if (_currentDragCell != _lastReportedDragCell)
                    {
                        IsDragChanged = true;
                        _lastReportedDragCell = _currentDragCell;
                    }
                }

                if (e.type == EventType.MouseUp && e.button == 0)
                {
                    IsClick = _state == State.Pressed;
                    IsDrag = _state == State.Dragging;
                    IsDragEnd = _state == State.Dragging;

                    _state = State.Released;
                    e.Use();
                    return;
                }

                if (
                    (e.type == EventType.MouseDown && e.button == 1)
                    || KeyBindingDefOf.Cancel.KeyDownEvent
                )
                {
                    if (_state == State.Dragging)
                    {
                        IsDragEnd = true;
                    }
                    _state = State.Released;
                    e.Use();
                    return;
                }
            }
        }

        public void Reset()
        {
            _state = State.Idle;

            _startDragCell = IntVec3.Invalid;
            _currentDragCell = IntVec3.Invalid;
            _lastReportedDragCell = IntVec3.Invalid;

            IsClick = false;
            IsMouseDown = false;
            IsDrag = false;
            IsDragStart = false;
            IsDragEnd = false;
            IsDragChanged = false;
        }
    }
}
