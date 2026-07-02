# Project Guide

## Overview

This repository is a Vite + Vue 3 web frontend for the MyGesture application. It is the UI layer for editing gesture rules and related scope/application mappings.

## Root Structure

- `index.html`: Vite entry HTML.
- `package.json`: project metadata, scripts, and dependencies.
- `pnpm-lock.yaml`: locked dependency tree.
- `pnpm-workspace.yaml`: workspace configuration.
- `vite.config.js`: Vite build and dev server configuration.
- `src/`: application source code.

## Source Structure

- `src/main.js`: app bootstrap, router setup, and global style import.
- `src/App.vue`: top-level shell with the main navigation, status bar, and global dialogs.
- `src/styles.css`: shared visual styling for the entire app.
- `src/composables/gestureEditorStore.js`: central state and logic for rule editing, scope selection, persistence, and WebView message handling.
- `src/components/`: reusable UI pieces used by the shell and editors.
  - `AppShell.vue`: layout wrapper for the app experience.
  - `GestureRuleDialog.vue`: gesture rule creation/edit dialog.
  - `GestureRuleList.vue`: rule list and rule operations.
  - `ScopeCreateDialog.vue`: scope creation dialog.
  - `ScopeSidebar.vue`: left-side scope navigation and management.
- `src/pages/`: route-level views.
  - `GlobalRulesPage.vue`: global rule scope page.
  - `CategoryRulesPage.vue`: category rule scope page.
  - `AppRulesPage.vue`: application rule scope page.

## Functional Areas

- Routing is hash-based and currently exposes `global`, `category`, and `app` rule scopes.
- `gestureEditorStore.js` is the main coordination layer for:
  - loading and saving rules,
  - tracking selected scope and application/category state,
  - opening and closing gesture editors,
  - handling hotkey and gesture recording,
  - communicating with the host WebView when present.
- The UI is designed to work both in browser preview mode and in the desktop host environment.

## Maintenance Rules

- When a page, route, layout, or reusable UI component changes, check whether this document needs to be updated.
- If the change affects project structure, startup flow, configuration, build/release behavior, WebView message flow, core module responsibilities, or test structure, update this document in the same change.
- Keep this guide aligned with the actual source tree, especially when new pages or components are added.
