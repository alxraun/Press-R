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
        private Sustainer _dragSustainer;
        private float _lastDragRealTime = -1000f;
        private int _lastDragCellCount;
        private IntVec3 _previousDragCell = IntVec3.Invalid;

        public void UpdateDragSound(
            DirectHaulDragState dragState,
            DirectHaulMode mode,
            IReadOnlyList<IntVec3> placementCells,
            Map map
        )
        {
            if (!dragState.IsDragging)
            {
                EndDragSustainer();
                return;
            }

            bool playChangedSound = false;

            if (mode == DirectHaulMode.Storage)
            {
                var rect = CellRect
                    .FromLimits(dragState.StartDragCell, dragState.CurrentDragCell)
                    .ClipInsideMap(map);
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
                    dragState.CurrentDragCell != _previousDragCell
                    && dragState.CurrentDragCell != dragState.StartDragCell
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

            _previousDragCell = dragState.CurrentDragCell;
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
