export function collectCategoryItems(rules, applications, categories = []) {
  const counts = new Map()

  for (const category of categories) {
    const name = toName(category)
    if (name) {
      counts.set(name, 0)
    }
  }

  for (const rule of rules) {
    const name = toName(rule.scopeName)
    if (name) {
      counts.set(name, (counts.get(name) ?? 0) + 1)
    }
  }

  for (const application of applications) {
    for (const category of application.categories ?? []) {
      const name = toName(category)
      if (name) {
        counts.set(name, counts.get(name) ?? 0)
      }
    }
  }

  return [...counts.entries()]
    .map(([name, count]) => {
      const application = applications.find(item => (item.categories ?? []).some(category => toName(category) === name))
      return {
        name,
        count,
        displayName: toName(application?.displayName) || name,
        icon: application?.icon || ''
      }
    })
    .sort((left, right) => left.name.localeCompare(right.name, 'zh-Hans-CN'))
}

export function collectAppItems(rules, applications) {
  const counts = new Map()

  for (const rule of rules) {
    const name = toName(rule.scopeName)
    if (name) {
      counts.set(name, (counts.get(name) ?? 0) + 1)
    }
  }

  for (const application of applications) {
    const name = toName(application.name)
    if (name) {
      counts.set(name, counts.get(name) ?? 0)
    }
  }

  return [...counts.entries()]
    .map(([name, count]) => {
      const application = applications.find(item => toName(item.name) === name)
      return {
        name,
        count,
        displayName: toName(application?.displayName) || name,
        icon: application?.icon || ''
      }
    })
    .sort((left, right) => left.name.localeCompare(right.name, 'zh-Hans-CN'))
}

function toName(value) {
  return String(value ?? '').trim()
}
