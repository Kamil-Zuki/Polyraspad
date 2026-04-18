# React + Next.js Through This Project

This guide teaches the frontend stack of `polyraspad-frontend` by following the real app structure.

## 1. Mental model

Think of the frontend as 4 layers:

1. Routes in `src/app`
2. UI components in `src/components`
3. State and data hooks in `src/contexts` and `src/lib/react-query`
4. HTTP and server adapters in `src/lib/api` and `src/app/api`

If you understand those 4 layers, you understand most of this frontend.

## 2. Where Next.js starts

Start with `src/app/layout.tsx`.

This is the root layout for the whole app. In Next.js App Router:

- `layout.tsx` wraps every page below it
- `page.tsx` is the route itself
- folders define URLs
- `[id]` means a dynamic route segment

In this project, the root layout does 3 important jobs:

1. Imports global CSS
2. Defines metadata and viewport
3. Wraps the app with providers

The provider nesting here is important:

- `ReactQueryProvider`
- `AuthProvider`
- `ProjectProvider`
- `AppLayout`

That means most pages and components can access:

- server data caching via React Query
- authenticated user state
- selected project state
- global shell UI

## 3. Your first Next.js route

Open `src/app/page.tsx`.

It immediately redirects to `/projects`.

That shows a key Next.js idea: routes are just React components, but Next.js also gives you navigation helpers like `redirect()` for routing behavior.

Then open `src/app/projects/page.tsx`.

This is a real feature page. It shows several core ideas at once:

- it is a client component because it uses hooks
- it reads data with custom hooks like `useProjects()`
- it computes derived UI state with `useMemo()`
- it renders composed UI with `ProjectsListModern`

This is a very good file to study because it reads like a page controller.

## 4. React basics in your codebase

React here is mostly built from these ideas:

### Components

A component is a function returning JSX.

Example places:

- `src/components/projects/projects-list-modern.tsx`
- `src/components/projects/project-details-view.tsx`
- `src/components/editor/editor-form.tsx`

The usual pattern is:

1. get data from hooks
2. hold small UI state with `useState`
3. branch on loading/error/empty/success
4. render JSX

### Props

Components receive inputs through props.

Examples:

- `ProjectDetailsView({ project })`
- `AppLayout({ children })`
- `EditorForm({ selectedDeckId, onSelectedDeckIdChange })`

Rule of thumb:

- props = data from parent
- state = data owned locally by the component

### State

Local UI state is everywhere in this project:

- dialog open/closed state
- form fields
- selected deck
- loading flags for actions
- reveal state in study mode

Examples:

- `projects-list-modern.tsx` uses `useState` for create-dialog visibility
- `editor-form.tsx` uses many `useState` calls for form UX
- `study/[deckId]/session/page.tsx` uses state for the whole study session flow

### Effects

`useEffect` is used when the component must synchronize with something outside plain rendering.

Examples in this project:

- reading `localStorage`
- focusing an input after a card changes
- adding keyboard listeners
- reacting to auth changes

Look at:

- `src/contexts/project-context.tsx`
- `src/components/auth/protected-route.tsx`
- `src/components/editor/editor-form.tsx`
- `src/app/study/[deckId]/session/page.tsx`

Good instinct:

- if something can be computed during render, do not use `useEffect`
- if you are syncing with browser APIs, timers, events, or async flows, `useEffect` is often appropriate

## 5. Client components vs server components

Next.js App Router defaults to server components.

But in this project, many pages start with `"use client"`.

Why?

Because they use:

- `useState`
- `useEffect`
- `useRouter`
- `useParams`
- browser APIs like `localStorage`, `navigator.clipboard`, `window`

Examples:

- `src/components/layout/app-layout.tsx`
- `src/contexts/auth-context.tsx`
- `src/app/projects/[id]/page.tsx`
- `src/components/editor/editor-form.tsx`

Simple rule:

- if a component needs interactivity or browser APIs, it must be a client component
- if it only renders data and does not use client-only hooks, it can stay server-side

This project currently leans heavily client-side, which is a totally normal stage for an app like this.

## 6. Routing in this project

Next.js routing is filesystem-based here:

- `src/app/projects/page.tsx` -> `/projects`
- `src/app/projects/[id]/page.tsx` -> `/projects/:id`
- `src/app/study/[deckId]/session/page.tsx` -> `/study/:deckId/session`
- `src/app/api/ollama/generate/route.ts` -> `/api/ollama/generate`

Two key navigation patterns appear in your code:

### Declarative navigation

Use `<Link />` when rendering links in UI.

See:

- `src/app/study/[deckId]/session/page.tsx`

### Imperative navigation

Use `useRouter()` when navigation happens after an event.

See:

- `src/components/auth/protected-route.tsx`
- `src/components/editor/editor-form.tsx`

### Reading dynamic params

Use `useParams()` inside client components.

See:

- `src/app/projects/[id]/page.tsx`
- `src/app/study/[deckId]/session/page.tsx`

## 7. Layouts and shells

`src/components/layout/app-layout.tsx` teaches an important separation:

- route = what page we are on
- layout = how the shell is wrapped

This component uses `usePathname()` to switch between:

- fullscreen pages like auth and study
- normal app shell with sidebar and header

That is a strong real-world React pattern: keep route-specific content separate from app chrome.

## 8. Context: app-level client state

This repo uses React Context for app state that many components need.

### Auth context

Open `src/contexts/auth-context.tsx`.

This context exposes:

- `user`
- `isLoading`
- `isAuthenticated`
- `login`
- `register`
- `logout`

Important detail:

This context does not manually own all data itself. It delegates server state to React Query using `useQuery` and `useMutation`.

That is a good pattern:

- Context provides app-wide access
- React Query handles fetching, caching, invalidation

### Project context

Open `src/contexts/project-context.tsx`.

This one stores the currently selected project and syncs it to `localStorage`.

This is client state, not server state.

That distinction matters:

- current selected project = client/app state
- project list from backend = server state

## 9. React Query: server state in this app

If React is the UI engine, React Query is the server-state engine.

Start with:

- `src/lib/react-query/query-client.tsx`

This creates the shared `QueryClient` and sets default behavior like:

- stale times
- garbage collection time
- retry rules

Then look at:

- `src/lib/react-query/project-queries.ts`

That file teaches the pattern used across the app:

- `useQuery` for reads
- `useMutation` for writes
- `invalidateQueries` after writes

Example flow:

1. page calls `useProjects()`
2. hook calls `apiClient.projects.getProjects()`
3. data is cached under a query key
4. `useCreateProject()` invalidates project queries after success
5. UI refreshes with fresh data

This is one of the most important frontend patterns in the whole project.

## 10. API client layer

Open:

- `src/lib/api/base-api-client.ts`
- `src/lib/api/project-client.ts`

The API layer is organized like this:

- `BaseApiClient` knows how to make authenticated requests
- feature clients like `ProjectClient` expose semantic methods

That means UI code can say:

- `apiClient.projects.getProjects()`
- `apiClient.projects.updateProject(id, data)`

instead of repeating `fetch()` logic everywhere.

This separation is excellent for learning because it keeps responsibilities clean:

- components render UI
- query hooks manage async cache lifecycle
- API clients talk HTTP

## 11. Next.js route handlers

Open:

- `src/app/api/ollama/generate/route.ts`

This is a Next.js route handler, which runs on the server side.

That file teaches a different responsibility from React components:

- parse request body
- validate input
- choose provider
- call server-only integrations
- return JSON with `NextResponse`

This is not a UI file. It is backend-for-frontend code living inside the Next app.

So inside one Next.js project, you currently have both:

- frontend UI routes
- server API routes

That is one reason Next.js feels powerful.

## 12. A big real React component: study session

Study:

- `src/app/study/[deckId]/session/page.tsx`

This file is worth revisiting many times.

It combines:

- route params
- React Query reads
- local UI state
- refs
- effects
- async event handlers
- conditional rendering

This is a realistic “state machine in a component”:

- start session
- fetch next card
- reveal answer
- submit rating
- update progress
- finish session

When you want to practice advanced React, this is one of the best files in the repo.

## 13. A big form component: editor

Study:

- `src/components/editor/editor-form.tsx`

This is your best example for frontend-heavy React work:

- controlled inputs
- form submission
- keyboard shortcuts
- file/image handling
- async actions
- composing helper utilities

Concepts to notice:

- form values are controlled by state/context
- submit handler assembles one DTO object
- UI-only concerns stay local
- API side effects happen in one place

If you want to become strong at frontend React, understanding this file will help a lot.

## 14. How data flows in this codebase

A common flow looks like this:

1. user opens a route in `src/app`
2. page renders feature components
3. component calls a React Query hook
4. hook uses an API client
5. backend responds
6. component re-renders with data
7. mutation invalidates cache after user actions

Example:

1. `/projects` route renders `ProjectsHubPage`
2. page calls `useProjects()`
3. `useProjects()` calls `apiClient.projects.getProjects()`
4. response is cached
5. `ProjectsListModern` renders cards
6. after creating/updating a project, query invalidation refreshes the list

## 15. What to study first

Best learning order for this repo:

1. `src/app/layout.tsx`
2. `src/components/layout/app-layout.tsx`
3. `src/app/projects/page.tsx`
4. `src/components/projects/projects-list-modern.tsx`
5. `src/lib/react-query/project-queries.ts`
6. `src/lib/api/project-client.ts`
7. `src/contexts/auth-context.tsx`
8. `src/app/projects/[id]/page.tsx`
9. `src/components/projects/project-details-view.tsx`
10. `src/components/editor/editor-form.tsx`
11. `src/app/study/[deckId]/session/page.tsx`
12. `src/app/api/ollama/generate/route.ts`

## 16. Practical exercises on your own code

These are good beginner-to-intermediate exercises in this project:

1. Add a new stat card to `src/app/projects/page.tsx`
2. Add a new field to the editor form and include it in the card DTO
3. Add a loading skeleton to a page that does not have one yet
4. Add a filter toggle to `ProjectsListModern`
5. Add a new React Query hook by copying the `useProjects` pattern
6. Add a new route under `src/app`
7. Add a small helper to the API client layer and consume it from a component

## 17. The most important distinction to master

When working in this codebase, keep asking:

- Is this local UI state?
- Is this shared app state?
- Is this server state?
- Is this server-only logic?

In this repo, the answers usually map like this:

- local UI state -> `useState`
- shared app state -> Context
- server state -> React Query
- server-only logic -> route handlers in `src/app/api`

Once that clicks, the project becomes much easier to reason about.

## 18. If you want to level up fast

Pick one feature and trace it end-to-end.

Recommended first trace:

1. `src/app/projects/page.tsx`
2. `src/components/projects/projects-list-modern.tsx`
3. `src/lib/react-query/project-queries.ts`
4. `src/lib/api/project-client.ts`

Recommended second trace:

1. `src/app/projects/[id]/page.tsx`
2. `src/components/projects/project-details-view.tsx`
3. `src/lib/react-query/project-queries.ts`

Recommended third trace:

1. `src/components/editor/editor-form.tsx`
2. editor context
3. card mutation hook
4. card API client

That is the fastest way to turn abstract React and Next.js concepts into instincts.
