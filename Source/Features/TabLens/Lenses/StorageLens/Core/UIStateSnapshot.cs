using System;
using RimWorld;
using UnityEngine;

namespace PressR.Features.TabLens.Lenses.StorageLens.Core
{
    public class UIStateSnapshot
    {
        public object SelectedObject { get; }
        public Type OpenTabType { get; }
        public string StorageTabSearchText { get; internal set; }
        public Vector2 StorageTabScrollPosition { get; }
        public MainTabWindow_Inspect Inspector { get; }
        public Selector Selector { get; }

        public UIStateSnapshot(
            object selectedObject,
            Type openTabType,
            string storageTabSearchText,
            Vector2 storageTabScrollPosition,
            MainTabWindow_Inspect inspector,
            Selector selector
        )
        {
            SelectedObject = selectedObject;
            OpenTabType = openTabType;
            StorageTabSearchText = storageTabSearchText;
            StorageTabScrollPosition = storageTabScrollPosition;
            Inspector = inspector;
            Selector = selector;
        }
    }
}
