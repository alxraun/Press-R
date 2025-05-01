using UnityEngine;

namespace PressR.Graphics.Tween
{
    public static class Equations
    {
        public static readonly EasingFunction Linear = progress => progress;

        public static readonly EasingFunction QuadEaseIn = progress => progress * progress;
        public static readonly EasingFunction QuadEaseOut = progress =>
            1f - (1f - progress) * (1f - progress);
        public static readonly EasingFunction QuadEaseInOut = progress =>
            progress < 0.5f
                ? 2f * progress * progress
                : 1f - Mathf.Pow(-2f * progress + 2f, 2f) / 2f;

        public static readonly EasingFunction CubicEaseIn = progress =>
            progress * progress * progress;
        public static readonly EasingFunction CubicEaseOut = progress =>
            1f - Mathf.Pow(1f - progress, 3f);
        public static readonly EasingFunction CubicEaseInOut = progress =>
            progress < 0.5f
                ? 4f * progress * progress * progress
                : 1f - Mathf.Pow(-2f * progress + 2f, 3f) / 2f;

        public static readonly EasingFunction SineEaseIn = progress =>
            1f - Mathf.Cos(progress * Mathf.PI / 2f);
        public static readonly EasingFunction SineEaseOut = progress =>
            Mathf.Sin(progress * Mathf.PI / 2f);
        public static readonly EasingFunction SineEaseInOut = progress =>
            -(Mathf.Cos(Mathf.PI * progress) - 1f) / 2f;
    }
}
