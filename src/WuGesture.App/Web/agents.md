# Project Guide

## Overview

This repository is a Vite + Vue 3 web frontend for the WuGesture application. It is the UI layer for editing gesture rules and related scope/application mappings.

## Root Structure

- `index.html`: Vite entry HTML.
- `package.json`: project metadata, scripts, and dependencies.
- `pnpm-lock.yaml`: locked dependency tree.
- `pnpm-workspace.yaml`: workspace configuration.
- `vite.config.js`: Vite build and dev server configuration, including `vite-svg-loader` for SVG-as-component imports.
- `src/`: application source code.

## Source Structure

- `src/main.js`: app bootstrap, router setup, and global style import.
- `src/App.vue`: top-level shell with the main navigation, status bar, and global dialogs.
- `src/styles.scss`: shared visual styling for the entire app.
- `src/gestureEditor/`: gesture editor domain module, including context, narrow stores, and internal workflow modules for rule editing, scope selection, persistence, and WebView message handling.
- `src/components/`: reusable UI pieces used by the shell and editors.
  - `AppShell.vue`: layout wrapper for the app experience.
  - `BaseDialog.vue`: shared dialog shell for modal behavior, optional close button, action footer, and transitions.
  - `BaseInput.vue` and `BaseRange.vue`: shared native input controls for text/number/color and range editing.
  - `IconActionButton.vue`: shared icon-only button for add/close/delete actions.
  - `GestureRuleDialog.vue`: gesture rule creation/edit dialog.
  - `GestureRuleList.vue`: rule list and rule operations.
  - `ScopeCreateDialog.vue`: scope creation dialog.
  - `ScopeSidebar.vue`: left-side scope navigation and management.
- `src/assets/`: SVG assets used by the UI, including add, close, and delete icons. These are imported as Vue components.
- `src/pages/`: route-level views.
  - `GlobalRulesPage.vue`: global rule scope page with the same split layout pattern as the other editor pages.
  - `CategoryRulesPage.vue`: category rule scope page.
  - `AppRulesPage.vue`: application rule scope page.

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
- Keep this guide aligned with the actual source tree, especially when new pages or components are added.
