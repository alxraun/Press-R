**Task:** Perform a comprehensive code refactoring. Your goal is not just a superficial cleanup, but to achieve the **highest possible level of code quality, design clarity, flexibility, maintainability, reusability, and self-documentation**.

**Definition of "Refactoring":**

Refactoring here means a **deep reorganization and transformation of the code**, which may include (but is not limited to):

- Local improvements in code clarity.
- Structural changes within classes and modules.
- Changing public APIs (methods, properties, contracts) of classes if it *significantly* improves the overall structure, flexibility, or reduces coupling.
- Reorganizing interactions between classes/modules.
- Moving responsibilities and logic between components.
- Applying or changing design patterns used.
- Complete rewriting of the code while preserving its functional purpose, but with a fundamentally new, clean design.

**Spectrum of Possible Actions:**

You are allowed to apply refactoring at different levels of depth to achieve the best result:

- Local cleanup: fixing code "smells", improving names, removing duplication, simplifying local logic, formatting.
- Structural refactoring within a class/module: applying SOLID (especially SRP), increasing cohesion, reorganizing properties/methods, encapsulation, extracting private helpers.
- API and interaction refactoring: reducing coupling, improving/changing APIs, critiquing and modifying dependency APIs (adapting calling code), introducing abstractions (interfaces), moving responsibilities between classes.
- Deep transformation / redesign: radical reorganization, changing approaches/patterns, potential complete rewrite in case of fundamental flaws.

**Core Principles and Goals:**

- Design Clarity: strive for maximum clarity, simplicity (KISS), and understandability of structure and logic.
- SOLID: ensure adherence to SOLID principles, especially the Single Responsibility Principle (SRP) and the Open/Closed Principle (OCP).
- DRY (Don't Repeat Yourself): eliminate code and logic duplication at all levels.
- Low Coupling: minimize dependencies between components.
- High Cohesion: group related logic and data together.
- Self-documenting code: achieve clarity through code structure and meaningful naming. Avoid comments explaining *what* the code does; the code should speak for itself. Comments are only acceptable to explain complex *why* (if it cannot be eliminated by refactoring) or for TODO/FIXME.

**Examples of Possible Transformations:**

- **Naming:** Improving names (variables, methods, classes, fields, constants).
- **Structure:** Decomposing methods and classes. Reorganizing properties/fields. Extracting private helpers. Grouping code logically.
- **Logic:** Simplifying conditional constructs. Applying appropriate patterns. Optimizing inefficient algorithms.
- **API and Contracts:** Changing signatures, return types. Adding/removing/renaming public members. Introducing interfaces/abstractions. Using DTOs.
- **Dependencies:** Changing dependency APIs, adapting all consuming code.
- **Eliminating Flaws:** Eradicating hacks, "crutches", temporary solutions.
- **Formalizing the Implicit:** Analyzing code for implicit concepts, states, processes, rules, dependencies, values, or patterns. Making the implicit explicit by formalizing hidden semantics.
