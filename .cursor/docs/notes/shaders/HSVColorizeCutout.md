# TabLens Overlay Shader Discussion

## Goal

Visually represent the status of `Thing`s on the map (e.g., allowed/disallowed in `ITab_Storage`) using a color overlay (typically green for allowed, red for disallowed) as part of the `TabLens` functionality. Provide a clear and consistent color signal regardless of the item's original color, while preserving texture detail.

## Final Implementation (`HSVColorizeCutout.shader`)

1.  **Method:** Renders a duplicate mesh and texture of the original `Thing` directly on top of the original `Thing`. This duplicate uses a custom material with the `HSVColorizeCutout.shader`.
2.  **Control:** The `TabLensThingOverlayGraphicObject` class is responsible for creating, updating, and rendering this overlay duplicate.
3.  **Color Logic (HSV):**
    *   Operates in the HSV color space to separate Hue, Saturation, and Value.
    *   The original pixel color for calculations (`pixelOrigRGB`) is obtained by multiplying the texture color (`texColor.rgb`), vertex color (`i.color.rgb`), and the `_OriginalBaseColor.rgb` property.
    *   **Hue:** Is completely **replaced** by the hue of the target tint color (from the `_Color` property), ensuring consistency (always a red or green hue).
    *   **Saturation:** Is **blended** (`lerp`) between the saturation of `pixelOrigRGB` and the saturation of the target tint color, using the `_SaturationBlendFactor` (default 0.5). This preserves some of the original material's character.
    *   **Value (Brightness):** Is **blended** between the brightness of the original pixel color and the brightness of the target tint color, using the `_BrightnessBlendFactor`. If `_BrightnessBlendFactor` is 0, the brightness of the original pixel color is used, preserving details, shadows, and highlights. With a non-zero value, blending occurs, the intensity of which depends on `_BrightnessBlendFactor` and the difference between the original and target brightness.
4.  **Smooth Fade-In Effect (via Color):**
    *   To simulate a smooth appearance *without changing actual transparency* (to avoid rendering artifacts), color blending is used.
    *   The shader has an `_EffectBlendFactor` property (`Range(0.0, 1.0)`).
    *   In the fragment shader, the final color is calculated as `lerp(original_RGB, HSV_tinted_RGB, _EffectBlendFactor)`.
    *   The `TabLensThingOverlayGraphicObject` class implements `IHasAlpha`. Its `Alpha` property (animated by `FadeInEffect` from 0 to 1) is passed to the shader as the value of `_EffectBlendFactor`.
    *   **Result:** At `Alpha = 0`, the overlay looks like the original. At `Alpha = 1`, the overlay is fully HSV-tinted. Intermediate values provide a smooth *color* transition.
5.  **Visibility/Transparency (Cutout):**
    *   Uses a strict `cutout` approach to determine pixel visibility.
    *   The fragment shader uses `clip(texColor.a - _Cutoff)` based on the alpha channel of the *original* texture and the `_Cutoff` property (default 0.5).
    *   The actual transparency of pixels does not change; a pixel is either fully visible (with the calculated color) or completely discarded.
    *   Uses `Blend Off` as blending is not required.
6.  **Configuration:** Shader properties (`_Color`, `_OriginalBaseColor`, `_SaturationBlendFactor`, `_BrightnessBlendFactor`, `_Cutoff`, `_EffectBlendFactor`) are set via `MaterialPropertyBlock` using `MpbConfigurators.HSVColorizeCutoutConfigurator`.
7.  **Rationale for Using a Duplicate:** Remains the same—modifying the original `Thing`'s material is problematic.

### Shader Workflow Diagram

```ASCII
[Fragment Shader]
      |
      v
[Get Texel (tx, ty)] --> [Read `texColor`]
      |
      v
[Check `texColor.a` >= `_Cutoff`?] --(No)--> [clip()]
      |
      | (Yes)
      v
[Calculate `pixelOrigRGB` = `_OriginalBaseColor`.rgb * `texColor`.rgb * `i.color`.rgb]
      |
      v
[Convert `pixelOrigRGB` to `pixelOrigHSV`]
      |
      v
[Convert `_Color`.rgb to `tintHSV`]
      |
      v
[Calculate `finalS` = lerp(`pixelOrigHSV`.y, `tintHSV`.y, `_SaturationBlendFactor`)]
      |
      v
[Calculate `finalV` (blend brightness based on `_BrightnessBlendFactor` and brightness difference, or `pixelOrigHSV`.z if `_BrightnessBlendFactor` = 0)]
      |
      v
[Assemble `finalHSV` = (`tintHSV`.x, `finalS`, `finalV`)]
      |
      v
[Convert `finalHSV` to `hsvTintedRGB`]
      |
      v
[Calculate `finalRGB` = lerp(`pixelOrigRGB`, `hsvTintedRGB`, `_EffectBlendFactor`)]
      |
      v
[Return `fixed4(finalRGB, 1.0)`]
```

## Removed Shaders

During development, `CutoutTint.shader` and `OutlineCutoutTint.shader` were removed as obsolete.

## Result

The final shader provides:
*   Consistent and unambiguous coloring (red/green hue).
*   Preservation of the original texture's brightness details (when `_BrightnessBlendFactor = 0`).
*   Partial preservation of the original texture's saturation for a more natural look.
*   Smooth fade-in effect through color change, not transparency.
*   High performance due to the `cutout` approach.
*   Additional control over brightness blending via `_BrightnessBlendFactor`.
