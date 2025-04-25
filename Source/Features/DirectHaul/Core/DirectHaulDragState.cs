using System;
using UnityEngine;
using Verse;

namespace PressR.Features.DirectHaul.Core
{
    public class DirectHaulDragState
    {
        public enum DragStateKind
        {
            Idle,
            Dragging,
            Completed,
        }

        private DragStateKind _state = DragStateKind.Idle;
        private IntVec3 _startDragCell;
        private IntVec3 _currentDragCell;
        private float _dragDistance;
        private const float MinDragDistanceThreshold = 0.1f;

        public DragStateKind State => _state;
        public bool IsDragging => _state == DragStateKind.Dragging;
        public bool IsCompleted => _state == DragStateKind.Completed;
        public IntVec3 StartDragCell => _startDragCell;
        public IntVec3 CurrentDragCell => _currentDragCell;
        public float DragDistance => _dragDistance;

        public void StartDrag(IntVec3 cell)
        {
            _startDragCell = cell;
            _currentDragCell = cell;
            _dragDistance = 0f;
            _state = DragStateKind.Idle;
        }

        public void UpdateDrag(IntVec3 cell)
        {
            _currentDragCell = cell;
            _dragDistance = CalculateDragDistance(_startDragCell, _currentDragCell);

            if (_state == DragStateKind.Idle && _dragDistance >= MinDragDistanceThreshold)
                _state = DragStateKind.Dragging;
        }

        public void EndDrag()
        {
            if (_startDragCell.IsValid)
                _state = DragStateKind.Completed;
        }

        public void Reset()
        {
            _state = DragStateKind.Idle;
            _startDragCell = IntVec3.Invalid;
            _currentDragCell = IntVec3.Invalid;
            _dragDistance = 0f;
        }

        private float CalculateDragDistance(IntVec3 start, IntVec3 end)
        {
            if (!start.IsValid || !end.IsValid)
            {
                return 0f;
            }

            return Mathf.Sqrt(Mathf.Pow(end.x - start.x, 2) + Mathf.Pow(end.z - start.z, 2));
        }
    }
}
