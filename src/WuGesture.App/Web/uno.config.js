import { defineConfig, presetUno } from 'unocss'

export default defineConfig({
  presets: [presetUno()],
  theme: {
    colors: {
      text: 'var(--text)',
      muted: 'var(--muted)',
      accent: 'var(--accent)',
      accentStrong: 'var(--accent-strong)',
      danger: 'var(--danger)',
      border: 'var(--border)'
    }
  },
  shortcuts: {
    'page-stack': 'grid gap-0',
    'section-head': 'flex items-start justify-between gap-10px mb-16px',
    'section-title': 'text-16px font-700',
    'section-desc': 'mt-4px text-13px text-muted',
    'section-actions': 'flex flex-wrap justify-end gap-8px',
    'form-grid': 'grid grid-cols-2 gap-14px',
    'form-grid-one': 'grid grid-cols-1 gap-14px',
    'field-card': 'grid gap-8px border border-[rgba(18,30,42,0.08)] m-b-0',
    'field-label': 'text-13px text-muted',
    'field-note': 'text-12px text-muted',
    'list-card': 'border border-[rgba(18,30,42,0.08)] bg-[rgba(248,251,255,0.96)]',
    'card-hover':
      'hover:bg-[rgba(242,247,255,1)] hover:border-[rgba(0,122,255,0.16)] hover:shadow-[0_14px_28px_rgba(18,30,42,0.06)] hover:-translate-y-1px',
    'text-truncate': 'min-w-0 overflow-hidden whitespace-nowrap text-ellipsis'
  }
})
