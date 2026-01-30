# Project Status

## TDD Progress

| Section | Status | Notes |
|---------|--------|-------|
| 1. Core Functionality | Complete | All key systems documented |
| 2. Architecture - Guiding Principles | Complete | 6 principles established |
| 2. Architecture - Detailed Design | Not started | Topics listed in HTML comment; fill out through discussion before implementation |
| 3. Constraints & Ground Rules | Complete | 9 constraints from spec + tool constraint |
| 4. Tech Package Choices | Complete | Input System, UI Toolkit, UGUI (provided popups), Addressables (SO data), Cinemachine dropped |
| 5. Data Configuration Strategy | Not started | Considerations listed in HTML comment; define SO structures before Story 2 |
| 6. Provided Assets Reference | Complete | All prefabs, scene, terrain, materials cataloged |
| 7. Deliverables - User Stories | Complete | Stories 1-10 with acceptance criteria |

## Implementation Progress

No stories have been started. Story 1 (Project Foundation) is next.

## Key Decisions Made

- UI Toolkit for new UI only; provided UGUI popups (WinPopup, LosePopup) used as-is
- Cinemachine dropped (no camera work needed)
- Addressables used for ScriptableObject tuning data (extensibility); visual prefabs via direct serialized field references
- Coding style and testing strategy in `.claude/CLAUDE.md` (reusable across projects)
- Architectural preferences also in `.claude/CLAUDE.md`; TDD section 2 captures project-specific guiding principles
- No LINQ in runtime code (global rule)
- Custom state machine (no Stateless library)

## Open Questions

- Detailed design topics (section 2) need discussion before implementation begins
- Data configuration strategy (section 5) needs to define specific SO structures
