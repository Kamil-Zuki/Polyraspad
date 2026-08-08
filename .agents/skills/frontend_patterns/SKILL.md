---
name: Frontend Patterns
description: Explains patterns, frameworks, and tools used for frontend development (Next.js App Router, Tailwind v4, TanStack Query, Radix). Triggers when building or editing frontend components.
---

# Frontend Skill (Next.js + React)

This skill provides the architectural foundation for frontend development in Polyraspad. Always follow these rules when editing or creating code inside `polyraspad-frontend/`.

## 1. Architecture & Framework
- **Next.js 16 (App Router):** All pages must live under `src/app/`. Do not use the `pages/` router. Use Server Components by default.
- **Client Components (`'use client'`):** Only use this directive when interactivity (hooks, state, browser APIs) is required.
- **Data Fetching (Server):** Use standard `fetch` with `cache: 'no-store'` (default), `force-cache`, or `next.revalidate` natively.
- **Data Fetching (Client):** Use **TanStack Query** (`@tanstack/react-query`). Ensure query keys are consistent and placed in `src/lib/react-query/`.

## 2. Styling (Tailwind CSS v4)
- Polyraspad uses CSS-first Tailwind (`@import "tailwindcss"` in `globals.css`).
- Use utility classes over custom CSS. Do not use CSS Modules.
- Use established design tokens:
  - Backgrounds: `bg-app-bg`, `bg-app-surface`, `bg-app-hover`
  - Text: `text-brand-primary`, `text-brand-secondary`
- Word status colors:
  - `NEW`: Blue (standard)
  - `SAVED` / `LINGQ`: Yellow
  - `KNOWN`: White/Transparent
  - `IGNORED`: Muted

## 3. UI Components (Radix + Lucide)
- Build upon accessible primitives using **Radix UI**.
- Use **lucide-react** for all icons.
- Prefer `cn(...)` utility (from `src/lib/utils/cn.ts`) for conditional class joining.

## 4. Key Business Logic: Reader Mode
- Do NOT use lemmas as the basis for knowledge status, duplicate checks, statistics, or card creation.
- Duplicate detection uses exact normalized term/phrase (`trim + lowercase`).
- Actions taken on words within the Reader must NOT force navigation away from the text.
- Phrase highlighting always takes visual priority over individual word highlights.
