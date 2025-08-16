using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PressR.Debug
{
    public static class Log
    {
        private static readonly HashSet<int> MessageOnceKeys = [];

        public static void MessageOnce(
            string text,
            int onceKey,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (MessageOnceKeys.Contains(onceKey))
            {
                return;
            }
            MessageOnceKeys.Add(onceKey);

            Verse.Log.Message($"[PressR] [{callerMemberName}] {text}");
        }
    }
}
