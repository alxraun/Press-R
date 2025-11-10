using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Microtools.Features.DirectHaul
{
    public class PersistentData
    {
        private List<Thing> _thingKeys;
        private List<TrackedThingData> _dataValues;

        private class TrackedThingData : IExposable
        {
            public DirectHaulStatus Status;
            public LocalTargetInfo TargetCell;

            public TrackedThingData() { }

            public TrackedThingData(DirectHaulStatus status, LocalTargetInfo targetCell)
            {
                Status = status;
                TargetCell = targetCell;
            }

            public void ExposeData()
            {
                Scribe_Values.Look(ref Status, "status", DirectHaulStatus.None);
                Scribe_TargetInfo.Look(ref TargetCell, "targetCell");
            }
        }

        public void ExposeData(State state)
        {
            var trackedThings = state.TrackedThings;

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                _thingKeys = trackedThings.Keys.ToList();
                _dataValues = trackedThings
                    .Values.Select(v => new TrackedThingData(v.Status, v.TargetCell))
                    .ToList();
            }

            Scribe_Collections.Look(ref _thingKeys, "thingKeys", LookMode.Reference);
            Scribe_Collections.Look(ref _dataValues, "dataValues", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                trackedThings.Clear();
                if (_thingKeys != null && _dataValues != null)
                {
                    for (var i = 0; i < _thingKeys.Count; i++)
                    {
                        var thing = _thingKeys[i];
                        var data = _dataValues[i];
                        if (thing != null && data != null)
                        {
                            trackedThings[thing] = (data.Status, data.TargetCell);
                        }
                    }
                }

                _thingKeys = null;
                _dataValues = null;
            }
        }
    }
}
