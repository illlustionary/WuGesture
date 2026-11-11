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
- `src/types/`: shared TypeScript contracts for base controls (`ui.ts` holds `SelectOption`, `SelectOptionValue`, `ButtonVariant`, `ButtonSize`, `ChipVariant`).
- `src/gestureEditor/`: gesture editor domain module, including context, narrow stores, and internal workflow modules for rule editing, scope selection, persistence, and WebView message handling.
- `src/components/`: reusable UI pieces grouped by responsibility: `sidebar/`, `layout/`, `dialog/`, `form/`, `ui/`, `gesture/`, `scope/`, and `rules/`.

### Base Controls (MD3)

Stage 2 introduced a self-built control kit; new code should use these components instead of the global classes.

- `src/components/ui/AppButton.vue`: filled/tonal/outlined/text/danger button, `md` (40) and `sm` (32) sizes, `leading` slot for an 18px icon.
- `src/components/ui/AppChip.vue`: 32-high, 8-radius chip with `selected` / `disabled` / `removable` (emits `remove`).
- `src/components/ui/AppDivider.vue`: 1px `outlineVariant` divider with `inset` / `vertical`.
- `src/components/ui/IconActionButton.vue`: 36×36 icon button (`icon` / `label` / `tone` / `color` / `showTooltip`); `inheritAttrs: false` so class, `disabled` and `@click` land on the inner `<button>`.
- `src/components/ui/HoverBubble.vue`: MD3 plain tooltip (`inverseSurface`, 4 radius, no arrow), 500ms delay, also shown on keyboard `focus-visible`; root node is always `.hover-bubble-trigger`.
- `src/components/form/BaseInput.vue`: Filled Text Field (48 high, 8 top radius, 2px primary bottom border on focus), still a single `<input>`; `type=color/range` keep the native styling.
- `src/components/form/CustomSelect.vue`: Exposed Dropdown, 48-high trigger, 4-radius menu, 48-high items, selected item uses `secondaryContainer` plus a check icon; keeps `modelValue` / `value`, keyboard navigation and Teleport.
- `src/components/form/ToggleCheckbox.vue`: MD3 Switch (52×32 track, 24/16 thumb), whole row clickable with the switch at the row end; keeps `label` / `note` and the `change` event.
- `src/components/form/BaseRange.vue`: native range wrapper that keeps the `--range-progress` contract and `$attrs` passthrough (`disabled` relies on it).
- `src/components/form/AppCheckbox.vue`: 18×18 MD3 checkbox with `indeterminate` support.
- The global `.primary-button` / `.secondary-button` / `.ghost-button` / `.danger-button` / `.icon-button` classes in `styles/_components.scss` still exist and are now MD3-styled, so untouched legacy markup gets the new look.
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
