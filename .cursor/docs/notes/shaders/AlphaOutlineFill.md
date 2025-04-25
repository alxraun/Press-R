# Shader for "Ghosts" in DirectHaul

## Problem

Using the standard `ShaderTypeDefOf.EdgeDetect.Shader` for rendering "ghosts" in `DirectHaul` causes issues with certain objects, especially those whose textures contain transparent areas within an opaque outline (e.g., `ThingWithComps` of type "Food" with a plate image).

`EdgeDetect` is likely based on geometry or depth buffer analysis, which allows it to outline the external silhouette of the mesh (often rectangular for sprites) well. However, it cannot correctly process "visual" edges defined by the texture's alpha channel itself. As a result, details inside the sprite (like the plate) are not outlined; only the external rectangle of the texture is, which looks incorrect.

## Chosen Solution: Shader Based on Sobel Filter and Luminance

To address this issue, a custom shader was created that operates by analyzing texture luminance using a Sobel filter to detect edges.

### How it Works

1.  **Input Data:** The shader takes the object's main texture (`_MainTex`), fill color (`_FillColor`), outline color (`_OutlineColor`), alpha cutoff threshold (`_Cutoff`), overall effect transparency (`_EffectAlpha`), and edge detection sensitivity (`_EdgeSensitivity`).
2.  **Pixel (Fragment) Analysis:**
    *   Reads the color and alpha value (`texColor`) of the current pixel from `_MainTex`.
    *   Calculates the cutoff threshold `cutoffThreshold = _Cutoff * 0.1`.
    *   If `texColor.a < cutoffThreshold`, the pixel is fully transparent and discarded (returns `fixed4(0, 0, 0, 0)`).
    *   If `texColor.a >= cutoffThreshold`, the pixel is processed further.
3.  **Edge Detection (Sobel Filter):**
    *   Calculates the **luminance** for the current pixel and its 8 neighbors (considering `cutoffThreshold` - pixels below the threshold have 0 luminance).
    *   Applies the **Sobel filter** to calculate luminance gradients along the X (`Gx`) and Y (`Gy`) axes.
    *   Calculates the **edge strength (`edgeStrength`)** as `saturate((abs(Gx) + abs(Gy)) * _EdgeSensitivity)`. This value ranges from 0 to 1, indicating the likelihood that the pixel is part of an edge.
4.  **Color Determination:**
    *   Calculates the base fill (`baseFillColor`) and outline (`baseOutlineColor`) colors, multiplied by the vertex color (`i.color`) and with the alpha channel multiplied by `_EffectAlpha`.
    *   The final pixel color (`finalColor`) is calculated by **linear interpolation (`lerp`)** between `baseFillColor` and `baseOutlineColor` based on `edgeStrength`: `lerp(baseFillColor, baseOutlineColor, edgeStrength)`.
    *   Pixels identified as edges (`edgeStrength` close to 1) will be colored closer to `baseOutlineColor`, while pixels within the fill (`edgeStrength` close to 0) will be closer to `baseFillColor`.
5.  **Blending:** Uses standard alpha blending (`Blend SrcAlpha OneMinusSrcAlpha`).

### Shader Workflow Diagram

```ASCII
[Fragment Shader]
      |
      v
[Get Texel (tx, ty)]
      |
      v
[Read alpha `texColor.a` at (tx, ty)]
      |
      v
[If `texColor.a` < (`_Cutoff` * 0.1)] -----> [Return (0,0,0,0) - transparent]
      |
      | (If `texColor.a` >= (`_Cutoff` * 0.1))
      v
[Read neighbor luminance, considering `cutoffThreshold`]
      |
      v
[Apply Sobel filter (Gx, Gy)] -----> [Calculate `edgeStrength` = saturate((abs(Gx) + abs(Gy)) * `_EdgeSensitivity`)]
      |
      v
[Calculate `baseFillColor` = `_FillColor` * `i.color`, alpha *= `_EffectAlpha`]
      |
      v
[Calculate `baseOutlineColor` = `_OutlineColor` * `i.color`, alpha *= `_EffectAlpha`]
      |
      v
[Calculate `finalColor` = lerp(`baseFillColor`, `baseOutlineColor`, `edgeStrength`)]
      |
      v
[Return `finalColor`] (will be blended with background via Blend SrcAlpha OneMinusSrcAlpha)
```

### Advantages for RimWorld

*   **Correct Sprite Outlining:** Detects visual edges based on luminance changes, not just the alpha channel, which can yield more accurate results for textures with internal details.
*   **Smooth Transition:** Using `lerp` ensures a smooth transition between the fill and outline colors, rather than a sharp border.
*   **Integration:** Easily integrates into the existing `DirectHaul` rendering system.
*   **Flexibility:** Allows customization of outline color, fill color/transparency, overall effect transparency, and edge sensitivity.

### Next Steps (Likely Already Completed)

1.  Created a `.shader` file with the described logic.
2.  Added the shader to an `AssetBundle`.
3.  Updated `ShaderManager` to load the shader.
4.  Created an `IMpbConfigurator` (`MpbConfigurators.AlphaOutlineFillConfigurator`) to set properties (`_FillColor`, `_OutlineColor`, `_Cutoff`, `_EffectAlpha`, `_EdgeSensitivity`).
5.  Updated `DirectHaulGhostGraphics` to use this shader and configurator.
6.  Updated `DirectHaulPreviewGhostGraphicObject` and `DirectHaulPendingGhostGraphicObject` to pass the necessary parameters in the configurator's `Payload`.
