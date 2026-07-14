export function getRangeProgress(value, min, max) {
  const numericValue = Number(value)
  const numericMin = Number(min)
  const numericMax = Number(max)

  if (
    !Number.isFinite(numericValue) ||
    !Number.isFinite(numericMin) ||
    !Number.isFinite(numericMax) ||
    numericMax <= numericMin
  ) {
    return '0%'
  }

  const progress = ((numericValue - numericMin) / (numericMax - numericMin)) * 100
  return `${Math.max(0, Math.min(100, progress))}%`
}
