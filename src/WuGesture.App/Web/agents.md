# Project Guide

## Overview

This repository is a Vite + Vue 3 web frontend for the WuGesture application. It is the UI layer for editing gesture rules and related scope/application mappings.

## Root Structure

- `index.html`: Vite entry HTML.
- `package.json`: project metadata, scripts, and dependencies (`dev`, `build`, `test`, `typecheck`, `format`).
- `pnpm-lock.yaml`: locked dependency tree.
- `pnpm-workspace.yaml`: workspace configuration.
- `tsconfig.json`: TypeScript / Vue type-check configuration. `allowJs` is on and `checkJs` is off, so existing `.js` files keep working; only `.ts`, `.d.ts` and `.vue` are checked.
- `vite.config.js`: Vite build and dev server configuration, including `vite-svg-loader` and the `@`/`@resources` path aliases.
- `src/`: application source code.

## Source Structure

- `src/main.js`: app bootstrap, router setup, and global style import.
- `src/App.vue`: top-level shell with the main navigation, status bar, and global dialogs.
- `src/env.d.ts`: Vite client types plus `*.svg` / `*.svg?component` module declarations for TypeScript imports.
- `src/styles.scss`: shared visual styling entry point for the entire app.
- `src/styles/tokens/`: MD3 design tokens split into color, typescale, shape, spacing, elevation, motion, state and legacy alias partials. Colors ship with a static `#529EF1` baseline and will be overridden by the runtime theme engine in stage 6.
- `src/theme/`: TypeScript contracts for design tokens (`tokens.ts`) and the eight preset seed palettes (`palettes.ts`).
- `src/gestureEditor/`: gesture editor domain module, including context, narrow stores, and internal workflow modules for rule editing, scope selection, persistence, and WebView message handling.
- `src/components/`: reusable UI pieces grouped by responsibility: `sidebar/`, `layout/`, `dialog/`, `form/`, `ui/`, `gesture/`, `scope/`, and `rules/`.
- `src/pages/*/components/`: page-private components that are only used by their owning route, such as category application items and edge/settings form sections.
- `src/assets/`: SVG assets used by the UI, including add, close, and delete icons. These are imported as Vue components.
- `src/pages/`: route-level views.
  - `global/index.vue`: global rule scope page.
  - `category/index.vue`: category rule scope page.
  - `app/index.vue`: application rule scope page.
  - `dev/md3/index.vue`: MD3 design-token preview page, registered only when `import.meta.env.DEV` is true; it never ships in production builds.

## Functional Areas

- Routing is hash-based and currently exposes `global`, `category`, and `app` rule scopes.
- `src/gestureEditor/context/gestureEditorContext.js` is the main coordination layer for:
  - loading and saving rules,
  - tracking selected scope and application/category state,
  - opening and closing gesture editors,
  - handling hotkey and gesture recording,
  - communicating with the host WebView when present.
- The UI is designed to work both in browser preview mode and in the desktop host environment.

## Maintenance Rules

- When a page, route, layout, or reusable UI component changes, check whether this document needs to be updated.
- When UI icon assets or shared button components change, update this document if the source tree or responsibilities changed.
- If the change affects project structure, startup flow, configuration, build/release behavior, WebView message flow, core module responsibilities, or test structure, update this document in the same change.
- Write new modules and new components in TypeScript; do not migrate existing `.js` files unless they are being rewritten (see `docs/design/md3-redesign-plan.md`, Appendix D).
- Keep this guide aligned with the actual source tree, especially when new pages or components are added.
