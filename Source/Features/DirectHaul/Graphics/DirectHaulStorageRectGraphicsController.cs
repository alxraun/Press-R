using System;
using PressR.Features.DirectHaul.Core;
using PressR.Graphics;
using PressR.Graphics.Controllers;
using PressR.Graphics.GraphicObjects;
using UnityEngine;
using Verse;

namespace PressR.Features.DirectHaul.Graphics
{
    public class DirectHaulStorageRectGraphicsController : IGraphicsController
    {
        private readonly IGraphicsManager _graphicsManager;
        private readonly DirectHaulStorage _directHaulStorage;
        private readonly DirectHaulState _state;
        private DirectHaulStorageRectGraphicObject _rectGraphicObject;

        private static object Key => DirectHaulStorageRectGraphicObject.GraphicObjectId;

        public DirectHaulStorageRectGraphicsController(
            IGraphicsManager graphicsManager,
            DirectHaulStorage directHaulStorage,
            DirectHaulState state
        )
        {
            _graphicsManager =
                graphicsManager ?? throw new ArgumentNullException(nameof(graphicsManager));
            _directHaulStorage =
                directHaulStorage ?? throw new ArgumentNullException(nameof(directHaulStorage));
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void Update()
        {
            bool shouldBeVisible =
                _state.Mode == DirectHaulMode.Storage
                && _state.IsDragging
                && _state.StartDragCell.IsValid
                && _state.CurrentDragCell.IsValid
                && PressRMod.Settings.directHaulSettings.enableStorageCreationPreview;

            if (_rectGraphicObject == null)
            {
                if (_graphicsManager.TryGetGraphicObject(Key, out var graphicObject))
                {
                    _rectGraphicObject = graphicObject as DirectHaulStorageRectGraphicObject;
                }
            }

            if (shouldBeVisible)
            {
                IntVec3 startCell = _state.StartDragCell;
                IntVec3 currentCell = _state.CurrentDragCell;
                Color edgeTargetColor = GetEdgeColor(startCell);

                EnsureGraphicObjectExistsAndActive(startCell, currentCell, edgeTargetColor);

                if (_rectGraphicObject != null)
                {
                    _rectGraphicObject.Color = edgeTargetColor;
                    _rectGraphicObject.StartCell = startCell;
                    _rectGraphicObject.EndCell = currentCell;
                }
            }
            else
            {
                RequestGraphicObjectRemoval();
            }
        }

        private Color GetEdgeColor(IntVec3 startCell)
        {
            Color edgeTargetColor = Color.white;
            if (startCell.IsValid)
            {
                var startZone = _directHaulStorage.FindStockpileAt(startCell);
                if (startZone != null)
                {
                    edgeTargetColor = startZone.color;
                }
            }
            edgeTargetColor.a = 1f;
            return edgeTargetColor;
        }

        private void EnsureGraphicObjectExistsAndActive(
            IntVec3 startCell,
            IntVec3 currentCell,
            Color edgeColor
        )
        {
            if (_rectGraphicObject == null)
            {
                if (
                    _graphicsManager.TryGetGraphicObject(Key, out var foundObject)
                    && foundObject is DirectHaulStorageRectGraphicObject foundRect
                )
                {
                    _rectGraphicObject = foundRect;
                }
            }

            if (_rectGraphicObject == null)
            {
                _rectGraphicObject = new DirectHaulStorageRectGraphicObject(startCell, currentCell)
                {
                    Color = edgeColor,
                };
                _rectGraphicObject =
                    _graphicsManager.RegisterGraphicObject(_rectGraphicObject)
                    as DirectHaulStorageRectGraphicObject;
                if (_rectGraphicObject == null)
                {
                    Log.Error(
                        $"[PressR] Failed to register {nameof(DirectHaulStorageRectGraphicObject)}"
                    );
                }
            }
            else if (_rectGraphicObject.State == GraphicObjectState.PendingRemoval)
            {
                _graphicsManager.RegisterGraphicObject(_rectGraphicObject);
                _rectGraphicObject.Color = edgeColor;
            }
            else if (_rectGraphicObject.State != GraphicObjectState.Active)
            {
                Log.Warning(
                    $"[PressR] {nameof(DirectHaulStorageRectGraphicObject)} found in unexpected state: {_rectGraphicObject.State}. Forcing Active."
                );
                _rectGraphicObject.State = GraphicObjectState.Active;
                _graphicsManager.RegisterGraphicObject(_rectGraphicObject);
                _rectGraphicObject.Color = edgeColor;
            }
        }

        private void RequestGraphicObjectRemoval()
        {
            if (_rectGraphicObject != null && _rectGraphicObject.State == GraphicObjectState.Active)
            {
                _graphicsManager.UnregisterGraphicObject(Key);
            }
            else if (_rectGraphicObject == null)
            {
                if (
                    _graphicsManager.TryGetGraphicObject(Key, out var obj)
                    && obj.State == GraphicObjectState.Active
                )
                {
                    _graphicsManager.UnregisterGraphicObject(Key);
                }
            }
        }

        public void Clear()
        {
            RequestGraphicObjectRemoval();
            _rectGraphicObject = null;
        }
    }
}
