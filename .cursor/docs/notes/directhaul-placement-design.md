# DirectHaulPlacement Design: Placement Shape Transformation

## Goal

Implement an algorithm for finding and selecting cells (`IntVec3`) for placing dragged items (`Thing`s) when using `DirectHaul`. This algorithm should provide a **smooth and intuitive transition** from a circular placement shape (on click without drag) to an elongated linear shape (during drag). The algorithm must address issues from previous iterations, such as visual discontinuities and unnatural item distribution ("conga line").

## Requirements

1.  **Idle State (No Drag / `focus1 == focus2`):**
    *   **Shape:** Circle or an area close to a circle, centered at `focus1`.
    *   **Algorithm:** Uses a BFS algorithm ("Fluid Fill") that results in a circular-like fill.
    *   ```ASCII
        [`focus1` (Center)] ----> [Circular BFS Area]
        ```

2.  **Drag Start and Process (`focus1 != focus2`):**
    *   **Transformation:** The placement shape must **smoothly** transform from the initial circle to a line.
    *   **Orientation:** The line is formed between the drag start point (`focus1`) and the current cursor position (`focus2`).
    *   **Distribution:** Items should aim to distribute *evenly along* the `focus1`-`focus2` line.
    *   **Smoothness:** The transition must be **seamless**, without disappearing or abrupt jumps of placement ghosts, especially at the moment drag begins.
    *   ```ASCII
        [`focus1`] -- drag --> [`focus2` (close)] ---> [Area smoothly shifts and starts elongating]
        [`focus1`] ----- drag -----> [`focus2` (far)] ---> [Items distributed along the line]
        ```

3.  **Placement:** The algorithm must return a list of `IntVec3` for `requiredCount` items.

4.  **Obstacle Handling:** The algorithm must correctly handle impassable cells and other obstacles.

5.  **Performance:** The algorithm must be fast enough for interactive dragging.

## Previous Approaches (Rejected/Problematic)

*   **Elliptical Methods (Hybrid BFS↔Ellipse, Pure Ellipse):** Encountered problems with symmetrical stretching, difficulty in parameter tuning, and challenges in finding enough cells within a thin ellipse on a discrete grid, leading to ghost disappearance.
*   **Weighted Metric (BFS and Line Interpolation):** While conceptually interesting, this approach showed unstable transformation (shape change speed) and issues with even distribution along the axis ("conga line").

## Final Approach: Hybrid (BFS ↔ Target Point Interpolation) with State Persistence

This approach combines two algorithms and a state persistence mechanism to achieve smoothness and reliability.

*   **Dispatcher Logic:**
    *   If `focus1 == focus2`: Use **BFS Fluid Fill Algorithm** (`FindPlacementCells_BFS`). The result (if valid) is saved.
    *   If `focus1 != focus2`: Use **Target Point Interpolation Algorithm** (`FindPlacementCells_Interpolated`).
        *   If the interpolation result is valid (found >= `requiredCount` cells), it is saved and returned.
        *   If the interpolation result is *invalid*, return the **last saved valid result**.

*   **BFS Fluid Fill Algorithm (`FindPlacementCells_BFS`)**
    *   Uses standard BFS with a `Queue`.
    *   Searches in layers starting from `center` (`focus1`).
    *   Cells in each layer are sorted by `DistanceToSquared(center)` to approximate a circular shape.
    *   Collects `requiredCount` valid cells (`IsValidCellForPlacement`).
    *   Uses `Walkable` for search expansion and `Standable` for final placement validation.

*   **Target Point Interpolation Algorithm (`FindPlacementCells_Interpolated`)**
    *   For each `i`-th item (from 0 to `requiredCount - 1`):
        1.  Calculate the "ideal" target point `p_line[i]` on the `focus1`-`focus2` segment (with even distribution).
        2.  Calculate the interpolation factor `interpolationFactor` (linear from 0 to 1) based on `distance(focus1, focus2)`.
        3.  Calculate the *actual* target point `p_target[i] = Lerp(focus1, p_line[i], interpolationFactor)`.
        4.  For `p_target[i]`, find the nearest valid and **not yet used** cell (`FindNearestAvailableValidCell`) within a limited radius.
    *   Collect the found cells.

*   **State Persistence Mechanism (`_lastValidPlacementCells`)**
    *   Stores the result of the last call (`FindPlacementCells_BFS` or `FindPlacementCells_Interpolated`) that returned `>= requiredCount` cells.
    *   Ensures that if `FindPlacementCells_Interpolated` fails (especially at the start of a drag), the previous valid result (usually from BFS) is returned, preventing ghost disappearance.

*   **Pros:**
    *   **Reliability:** Guaranteed to return a valid number of cells (except when impossible even with BFS), solving the disappearing ghost problem.
    *   **Smooth Transition:** Visual transition is achieved by returning the old state until the new algorithm can find a solution itself.
    *   **Clear Separation:** Explicit logic for idle (BFS) and drag (Interpolation).
    *   **Distribution:** Interpolation algorithm ensures even distribution along the line.
*   **Cons:**
    *   Slight code complexity due to state persistence.
    *   Theoretically possible for the preview to get "stuck" in BFS shape if the drag area is extremely unfavorable for interpolation (but this is better than disappearing).
*   **Diagram:**
    ```ASCII
    [focus1 == focus2] --> [`BFS Algorithm`] -- Saves (if successful) --> [`_lastValidPlacementCells`] --> [Return BFS Result]
        |
        | (focus1 != focus2)
        V
    [`Interpolation Algorithm`] --> [Interpolation Result]
        |                               |
        | (Found Enough?)               | (Found Insufficient)
        V                               V
    [Saves to `_lastValidPlacementCells`]     [Does Nothing]
        |                               |
        V                               V
    [Return New Result]             [Return `_lastValidPlacementCells`]
    ```

## Recommended Approach: Hybrid (BFS ↔ Target Point Interpolation) with State Persistence

This approach is chosen as the most balanced compromise, ensuring reliability (no disappearing ghosts) and acceptable visual smoothness in the transition from circle to line.
