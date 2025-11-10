using System.Collections.Generic;
using Verse;

namespace Microtools.Features.DirectHaulStorageMode
{
    public class State
    {
        public Map CurrentMap { get; internal set; }
        public List<Thing> SelectedThings { get; } = [];

        public void Clear()
        {
            CurrentMap = null;
            SelectedThings.Clear();
        }
    }
}
