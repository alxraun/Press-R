using System.Collections.Generic;
using PressR.Features.DirectHaul.Core;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PressR.Features.DirectHaul
{
    public class DirectHaulSoundPlayer
    {
        private readonly DirectHaulState _state;
        private Sustainer _dragSustainer;
        private float _lastDragRealTime = -1000f;
        private int _lastDragCellCount;
        private IntVec3 _previousDragCell = IntVec3.Invalid;

        public DirectHaulSoundPlayer(DirectHaulState state)
        {
            _state = state ?? throw new System.ArgumentNullException(nameof(state));
        }

        public void UpdateSound()
        {
            if (!_state.IsDragging)
            {
                EndDragSustainer();
                return;
            }

            bool playChangedSound = false;

            if (_state.Mode == DirectHaulMode.Storage)
            {
                var rect = CellRect
                    .FromLimits(_state.StartDragCell, _state.CurrentDragCell)
                    .ClipInsideMap(_state.Map);
                int currentCount = rect.IsEmpty ? 0 : rect.Area;
                if (currentCount != _lastDragCellCount)
                {
                    playChangedSound = true;
                    _lastDragCellCount = currentCount;
                }
            }
            else
            {
                if (
                    _state.CurrentDragCell != _previousDragCell
                    && _state.CurrentDragCell != _state.StartDragCell
                )
                {
                    playChangedSound = true;
                }
            }

            if (playChangedSound)
            {
                var info = SoundInfo.OnCamera();
                info.SetParameter("TimeSinceDrag", Time.realtimeSinceStartup - _lastDragRealTime);
                SoundDefOf.Designate_DragZone_Changed.PlayOneShot(info);
                _lastDragRealTime = Time.realtimeSinceStartup;
            }

            if (_dragSustainer == null || _dragSustainer.Ended)
            {
                _dragSustainer = SoundDefOf.Designate_DragAreaAdd.TrySpawnSustainer(
                    SoundInfo.OnCamera(MaintenanceType.PerFrame)
                );
            }
            else
            {
                _dragSustainer.info.SetParameter(
                    "TimeSinceDrag",
                    Time.realtimeSinceStartup - _lastDragRealTime
                );
                _dragSustainer.Maintain();
            }

            _previousDragCell = _state.CurrentDragCell;
        }

        public void EndDragSustainer()
        {
            _dragSustainer?.End();
            _dragSustainer = null;
            _lastDragCellCount = 0;
            _lastDragRealTime = -1000f;
            _previousDragCell = IntVec3.Invalid;
        }
    }
}
