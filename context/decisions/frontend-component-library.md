# Frontend Component Library

Date: 2026-05-05

## Context

`polyraspad-frontend` is a Next.js, React, TypeScript, and Tailwind CSS application.

The frontend already has `components.json` configured for shadcn/ui with:

- `style: new-york`;
- React Server Components enabled;
- Tailwind CSS variables enabled;
- `neutral` as the base color;
- `lucide` as the icon library;
- aliases for `@/components`, `@/components/ui`, `@/lib`, and `@/hooks`.

The installed dependencies already match the shadcn/ui stack: Tailwind CSS, `class-variance-authority`, `clsx`, `tailwind-merge`, `tailwindcss-animate`, and `lucide-react`.

The product direction makes the reader the primary learning surface. Reader UI needs custom learning states, compact interactions around selected terms, and domain-specific visual statuses:

- `NEW`: blue;
- `LINGQ` / `LEARNING`: yellow;
- `KNOWN`: normal reading text;
- `IGNORED`: muted and not counted.

## Decision

Use shadcn/ui as the primary component layer for `polyraspad-frontend`.

Use Radix UI primitives through shadcn/ui for accessible interactive components, Tailwind CSS for styling, and `lucide-react` for icons.

Add shadcn/ui components incrementally as needed instead of installing all components at once.

## Consequences

- Shared UI primitives should live under `polyraspad-frontend/src/components/ui`.
- Domain-specific components should continue to live under feature folders such as `src/components/library`, `src/components/study`, and `src/app/reader`.
- Components copied from shadcn/ui become project-owned code and may be adapted to Polyraspad's reader and learning workflows.
- New UI work should prefer existing shadcn/ui-compatible utilities such as `cn`, `class-variance-authority`, Tailwind CSS variables, and lucide icons.
- The frontend should avoid adopting another full component system unless this decision is superseded.

## Alternatives Considered

- Mantine: strong general-purpose React component library, but it introduces its own provider and styling conventions that would duplicate the current Tailwind/shadcn direction.
- Material UI: mature and comprehensive, but its Material Design defaults would impose a stronger visual language than the reader-first Polyraspad UI needs.
- Radix UI directly: appropriate for low-level primitives, but using it directly would require building the visual component layer manually. shadcn/ui provides that layer while preserving local ownership of the source code.

## Links

- Active reader plan: `context/plans/active/lingq-reader-implementation-plan.md`
- Frontend rules: `context/rules/frontend-rules.md`
- Project rules: `context/rules/project-rules.md`
