using PressR.Graphics.Utils.Replicator2.Core;
using PressR.Graphics.Utils.Replicator2.Interfaces;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator2.Strategies
{
    public class PawnStrategy2 : BaseRenderStrategy
    {
        private const float HumanlikeBaseYOffset = -0.06f;

        public override bool CanHandle(Thing thing)
        {
            return thing is Pawn || thing is Corpse;
        }

        public override Mesh GetBaseMesh(RenderContext context)
        {
            Pawn pawn = context.Pawn;
            if (pawn == null)
                return base.GetBaseMesh(context);

            if (pawn.RaceProps.Humanlike)
            {
                return HumanlikeMeshPoolUtility
                    .GetHumanlikeBodySetForPawn(pawn)
                    .MeshAt(context.BaseRot);
            }
            else
            {
                return pawn.Graphic?.MeshAt(context.BaseRot) ?? base.GetBaseMesh(context);
            }
        }

        public override Material GetBaseMaterial(RenderContext context)
        {
            Pawn pawn = context.Pawn;
            if (pawn == null)
                return base.GetBaseMaterial(context);

            if (pawn.RaceProps.Humanlike && pawn.Drawer?.renderer?.renderTree != null)
            {
                Graphic bodyGraphic = pawn.Drawer.renderer.BodyGraphic;
                if (bodyGraphic != null)
                {
                    return bodyGraphic.MatSingleFor(pawn);
                }
            }

            return pawn.Graphic?.MatSingleFor(pawn) ?? base.GetBaseMaterial(context);
        }

        public override Vector3 GetBaseOffset(RenderContext context)
        {
            Vector3 offset = base.GetBaseOffset(context);

            if (context.Pawn != null)
            {
                offset.y += context.Pawn.Drawer.SeededYOffset;

                if (context.Pawn.RaceProps.Humanlike)
                {
                    offset.y += HumanlikeBaseYOffset;
                }
            }

            return offset;
        }

        public override Quaternion GetBaseRotation(RenderContext context)
        {
            return base.GetBaseRotation(context);
        }

        public override Vector3 GetBaseScale(RenderContext context)
        {
            Vector3 baseScale = base.GetBaseScale(context);

            if (context.Pawn?.RaceProps.Humanlike ?? false)
            {
                Vector2 bodyScale = context.Pawn.story?.bodyType?.bodyGraphicScale ?? Vector2.one;

                baseScale.x *= bodyScale.x;
                baseScale.z *= bodyScale.y;
            }

            return baseScale;
        }

        private Rot4 GetFacing(Thing thing, Pawn pawn, Rot4 rot)
        {
            if (thing is Corpse || pawn.Downed)
            {
                return pawn.Drawer?.renderer?.LayingFacing() ?? rot;
            }

            if (pawn.GetPosture() != PawnPosture.Standing)
            {
                return pawn.Drawer?.renderer?.LayingFacing() ?? rot;
            }

            return rot;
        }
    }
}
