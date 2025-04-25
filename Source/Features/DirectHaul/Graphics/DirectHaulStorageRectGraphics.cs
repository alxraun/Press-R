using PressR.Features.DirectHaul.Graphics.GraphicObjects;
using PressR.Graphics.Interfaces;
using UnityEngine;
using Verse;

namespace PressR.Features.DirectHaul.Graphics
{
    public class DirectHaulStorageRectGraphics
    {
        private readonly IGraphicsManager _graphicsManager;
        private readonly DirectHaulStorage _directHaulStorage;
        private DirectHaulStorageRectGraphicObject _rectGraphicObject;

        public DirectHaulStorageRectGraphics(
            IGraphicsManager graphicsManager,
            DirectHaulStorage directHaulStorage
        )
        {
            _graphicsManager =
                graphicsManager ?? throw new System.ArgumentNullException(nameof(graphicsManager));
            _directHaulStorage =
                directHaulStorage
                ?? throw new System.ArgumentNullException(nameof(directHaulStorage));
        }

        public void UpdateStorageRect(
            bool isDragging,
            IntVec3 startCell,
            IntVec3 currentCell,
            Map map
        )
        {
            bool shouldBeVisible = isDragging && startCell.IsValid && currentCell.IsValid;
            object key = DirectHaulStorageRectGraphicObject.GraphicObjectId;

            if (_rectGraphicObject == null)
            {
                _graphicsManager.TryGetGraphicObject(key, out var graphicObject);
                _rectGraphicObject = graphicObject as DirectHaulStorageRectGraphicObject;
            }

            if (shouldBeVisible)
            {
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
                RequestGraphicObjectRemoval(key);
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
                _rectGraphicObject = new DirectHaulStorageRectGraphicObject(startCell, currentCell)
                {
                    Color = edgeColor,
                };
                if (!_graphicsManager.RegisterGraphicObject(_rectGraphicObject))
                {
                    _rectGraphicObject = null;
                }
            }
            else if (_rectGraphicObject.State == GraphicObjectState.PendingRemoval)
            {
                _graphicsManager.RegisterGraphicObject(_rectGraphicObject);
                _rectGraphicObject.State = GraphicObjectState.Active;
            }
            else if (_rectGraphicObject.State != GraphicObjectState.Active)
            {
                _rectGraphicObject.State = GraphicObjectState.Active;
            }
        }

        private void RequestGraphicObjectRemoval(object key)
        {
            if (_rectGraphicObject != null && _rectGraphicObject.State == GraphicObjectState.Active)
            {
                _graphicsManager.UnregisterGraphicObject(key, force: false);
            }
        }

        public void Clear()
        {
            _graphicsManager.UnregisterGraphicObject(
                DirectHaulStorageRectGraphicObject.GraphicObjectId,
                force: true
            );
            _rectGraphicObject = null;
        }
    }
}
