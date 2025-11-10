using UnityEngine;
using Verse;
using Verse.Sound;

namespace Microtools
{
    public class DragSound(SoundDef soundDragSustain, SoundDef soundDragChanged)
    {
        private readonly SoundDef _soundDragSustain = soundDragSustain;
        private readonly SoundDef _soundDragChanged = soundDragChanged;

        private Sustainer _sustainer;
        private float _lastDragRealTime = -1000f;

        private const string TimeSinceDragParam = "TimeSinceDrag";

        public void Start()
        {
            _lastDragRealTime = Time.realtimeSinceStartup;
            EnsureSustainer();
        }

        public void Update(bool hasChanged)
        {
            if (hasChanged && _soundDragChanged != null)
            {
                var info = SoundInfo.OnCamera();
                info.SetParameter(
                    TimeSinceDragParam,
                    Time.realtimeSinceStartup - _lastDragRealTime
                );
                _soundDragChanged.PlayOneShot(info);
                _lastDragRealTime = Time.realtimeSinceStartup;
            }

            if (_sustainer == null || _sustainer.Ended)
            {
                EnsureSustainer();
            }
            else
            {
                _sustainer.externalParams[TimeSinceDragParam] =
                    Time.realtimeSinceStartup - _lastDragRealTime;
                _sustainer.Maintain();
            }
        }

        public void End()
        {
            if (_sustainer != null)
            {
                _sustainer.End();
                _sustainer = null;
            }
        }

        private void EnsureSustainer()
        {
            if (_soundDragSustain == null)
            {
                return;
            }
            _sustainer = _soundDragSustain.TrySpawnSustainer(
                SoundInfo.OnCamera(MaintenanceType.PerFrame)
            );
            _lastDragRealTime = Time.realtimeSinceStartup;
        }
    }
}
