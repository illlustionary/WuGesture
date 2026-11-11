/** 基础控件的共享类型契约。 */

export type SelectOptionValue = string | number

export interface SelectOption {
  value: SelectOptionValue
  label: string
  disabled?: boolean
}

export type ButtonVariant = 'filled' | 'tonal' | 'outlined' | 'text' | 'danger'

export type ButtonSize = 'md' | 'sm'

export type ChipVariant = 'assist' | 'filter' | 'input'
