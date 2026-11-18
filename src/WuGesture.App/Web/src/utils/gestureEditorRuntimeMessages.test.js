import { describe, expect, it } from 'vitest'
import { createMessageDeduplicator, formatActionFailureMessage } from './gestureEditorRuntimeMessages'

describe('formatActionFailureMessage', () => {
  it('includes the action name and the error reason', () => {
    expect(formatActionFailureMessage('音量加', '设备未就绪')).toBe('动作“音量加”执行失败：设备未就绪')
  })

  it('falls back to a generic subject and reason', () => {
    expect(formatActionFailureMessage('', '  ')).toBe('动作执行失败，请查看日志了解详情。')
  })
})

describe('createMessageDeduplicator', () => {
  it('drops repeats inside the window and accepts them after it', () => {
    let current = 1000
    const shouldShow = createMessageDeduplicator({ windowMs: 5000, now: () => current })

    expect(shouldShow('same')).toBe(true)
    current += 1000
    expect(shouldShow('same')).toBe(false)
    expect(shouldShow('other')).toBe(true)
    current += 5000
    expect(shouldShow('same')).toBe(true)
  })
})
