import { defineConfig, presetUno } from 'unocss'

export default defineConfig({
  presets: [presetUno()],
  theme: {
    colors: {
      // 语义别名（存量类名用）
      text: 'var(--md-sys-color-on-surface)',
      muted: 'var(--md-sys-color-on-surface-variant)',
      accent: 'var(--md-sys-color-primary)',
      accentStrong: 'var(--md-sys-color-primary)',
      danger: 'var(--md-sys-color-error)',
      border: 'var(--md-sys-color-outline-variant)',

      // MD3 颜色角色
      primary: 'var(--md-sys-color-primary)',
      'on-primary': 'var(--md-sys-color-on-primary)',
      'primary-container': 'var(--md-sys-color-primary-container)',
      'on-primary-container': 'var(--md-sys-color-on-primary-container)',
      secondary: 'var(--md-sys-color-secondary)',
      'on-secondary': 'var(--md-sys-color-on-secondary)',
      'secondary-container': 'var(--md-sys-color-secondary-container)',
      'on-secondary-container': 'var(--md-sys-color-on-secondary-container)',
      tertiary: 'var(--md-sys-color-tertiary)',
      'on-tertiary': 'var(--md-sys-color-on-tertiary)',
      'tertiary-container': 'var(--md-sys-color-tertiary-container)',
      'on-tertiary-container': 'var(--md-sys-color-on-tertiary-container)',
      error: 'var(--md-sys-color-error)',
      'on-error': 'var(--md-sys-color-on-error)',
      'error-container': 'var(--md-sys-color-error-container)',
      'on-error-container': 'var(--md-sys-color-on-error-container)',
      surface: 'var(--md-sys-color-surface)',
      'on-surface': 'var(--md-sys-color-on-surface)',
      'on-surface-variant': 'var(--md-sys-color-on-surface-variant)',
      'surface-container-lowest': 'var(--md-sys-color-surface-container-lowest)',
      'surface-container-low': 'var(--md-sys-color-surface-container-low)',
      'surface-container': 'var(--md-sys-color-surface-container)',
      'surface-container-high': 'var(--md-sys-color-surface-container-high)',
      'surface-container-highest': 'var(--md-sys-color-surface-container-highest)',
      outline: 'var(--md-sys-color-outline)',
      'outline-variant': 'var(--md-sys-color-outline-variant)',
      'inverse-surface': 'var(--md-sys-color-inverse-surface)',
      'inverse-on-surface': 'var(--md-sys-color-inverse-on-surface)'
    }
  },
  shortcuts: {
    'page-stack': 'grid gap-0',
    'section-head': 'flex items-start justify-between gap-10px mb-16px',
    'section-title': 'text-16px font-500',
    'section-desc': 'mt-4px text-13px text-muted',
    'section-actions': 'flex flex-wrap justify-end gap-8px',
    'form-grid': 'grid grid-cols-2 gap-14px',
    'form-grid-one': 'grid grid-cols-1 gap-14px',
    'field-card': 'grid gap-8px border border-outline-variant m-b-0',
    'field-label': 'text-13px text-muted',
    'field-note': 'text-12px text-muted',
    'list-card': 'border border-outline-variant bg-surface-container-low',
    'card-hover': 'hover:bg-surface-container-high hover:border-outline',
    'text-truncate': 'min-w-0 overflow-hidden whitespace-nowrap text-ellipsis'
  }
})
