using PressR.Graphics;
using UnityEngine;
using Verse;

namespace PressR.Features.DirectHaulStorageMode
{
    public sealed class GraphicsController_StockpileRect(
        IGraphicsManager graphicsManager,
        ActionDispatcher actionDispatcher,
        Input input
    ) : IGraphicsController
    {
        private readonly IGraphicsManager _graphicsManager = graphicsManager;
        private readonly ActionDispatcher _actionDispatcher = actionDispatcher;
        private readonly Input _input = input;
        private GraphicObject_StockpileRect _rectGraphicObject;

        private static object Key => GraphicObject_StockpileRect.GraphicObjectId;

        public void Update()
        {
            bool shouldBeVisible =
                _input.IsDragging
                && _input.StartDragCell.IsValid
                && _input.CurrentDragCell.IsValid
                && PressRMod.Settings.directHaulSettings.enableStorageCreationPreview;

            if (_rectGraphicObject == null)
            {
                if (_graphicsManager.TryGetGraphicObject(Key, out var graphicObject))
                {
                    _rectGraphicObject = graphicObject as GraphicObject_StockpileRect;
                }
            }

            if (shouldBeVisible)
            {
                IntVec3 startCell = _input.StartDragCell;
                IntVec3 currentCell = _input.CurrentDragCell;
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
                var startZone = _actionDispatcher.FindStockpileAt(startCell);
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
                    && foundObject is GraphicObject_StockpileRect foundRect
                )
                {
                    _rectGraphicObject = foundRect;
                }
            }

            if (_rectGraphicObject == null)
            {
                _rectGraphicObject = new GraphicObject_StockpileRect(startCell, currentCell)
                {
                    Color = edgeColor,
                };
                _rectGraphicObject =
                    _graphicsManager.RegisterGraphicObject(_rectGraphicObject)
                    as GraphicObject_StockpileRect;
                if (_rectGraphicObject == null)
                {
                    Log.Error($"[PressR] Failed to register {nameof(GraphicObject_StockpileRect)}");
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
                    $"[PressR] {nameof(GraphicObject_StockpileRect)} found in unexpected state: {_rectGraphicObject.State}. Forcing Active."
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
