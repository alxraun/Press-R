using System.Collections.Generic;
using PressR.Graphics.Utils.Replicator2.Interfaces;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator2.Core
{
    public class RenderContext
    {
        public Thing Thing { get; }
        public Pawn Pawn { get; }
        public Rot4 BaseRot { get; }

        public Vector3 CurrentPos { get; set; }
        public Quaternion CurrentRot { get; set; }
        public Vector3 CurrentScale { get; set; }
        public Material CurrentMaterial { get; set; }
        public Mesh CurrentMesh { get; set; }

#if DEBUG

        public List<string> AppliedDecoratorNames { get; }
#endif

        public RenderContext(
            Thing thing,
            Rot4 baseRot,
            Vector3 basePos,
            Quaternion baseRotQuat,
            Vector3 baseScale,
            Material baseMat,
            Mesh baseMesh
        )
        {
            Thing = thing;
            Pawn = thing as Pawn ?? (thing as Corpse)?.InnerPawn;
            BaseRot = baseRot;
            CurrentPos = basePos;
            CurrentRot = baseRotQuat;
            CurrentScale = baseScale;
            CurrentMaterial = baseMat;
            CurrentMesh = baseMesh;

#if DEBUG

            AppliedDecoratorNames = new List<string>();
#endif
        }

#if DEBUG
        public void RecordAppliedDecorator(IRenderDataDecorator decorator)
        {
            AppliedDecoratorNames?.Add(decorator.GetType().Name);
        }
#endif
    }
}
