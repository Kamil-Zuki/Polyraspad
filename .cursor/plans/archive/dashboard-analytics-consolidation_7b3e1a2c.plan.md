---
name: dashboard-analytics-consolidation
overview: Merge overlapping Dashboard and Analytics into one home surface — action-focused "Today" plus a single "Progress" section; redirect /analytics and fix deep links.
todos:
  - id: product-ia
    content: Lock IA — Dashboard = Today + Progress; Analytics route redirects; Vocabulary links go to /vocabulary.
    status: completed
  - id: frontend-merge
    content: Consolidate widgets on /dashboard, remove duplicate heatmap/hero stats, redirect /analytics.
    status: completed
  - id: update-deep-links
    content: Update library, study, reader links from /analytics to /dashboard#progress or /vocabulary.
    status: completed
  - id: verify-frontend
    content: Run frontend tests/build for affected routes.
    status: completed
  - id: reviewer-check
    content: Review for navigation regressions and duplicate metrics.
    status: completed
isProject: false
---

# Dashboard + Analytics Consolidation

## Goal
Remove the feeling of two nearly identical pages by making `/dashboard` the single learning home with clear sections, and retiring standalone `/analytics` as a duplicate surface.

## Verification
- `npm test --run` — 110/110 passed
- `/analytics` → redirect `/dashboard#progress`

## Cleanup
- [x] Archived
