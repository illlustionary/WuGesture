// 运行时动作失败提示：宿主在执行手势/边缘动作出错时只发一条 WebView 消息，
// 这里负责把消息整理成用户能看懂的文案，并做短时间去重，避免连续拖动时刷屏。

export function formatActionFailureMessage(action, error) {
  const actionName = String(action ?? '').trim()
  const reason = String(error ?? '').trim()
  const subject = actionName ? `动作“${actionName}”` : '动作'

  if (reason) {
    return `${subject}执行失败：${reason}`
  }

  return `${subject}执行失败，请查看日志了解详情。`
}

export function createMessageDeduplicator({ windowMs = 5000, now = () => Date.now() } = {}) {
  let lastKey = ''
  let lastAt = 0

  return function shouldShow(key) {
    const normalized = String(key ?? '')
    const timestamp = now()
    if (normalized === lastKey && timestamp - lastAt < windowMs) {
      return false
    }

    lastKey = normalized
    lastAt = timestamp
    return true
  }
}
