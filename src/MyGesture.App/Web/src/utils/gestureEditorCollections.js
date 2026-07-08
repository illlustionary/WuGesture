export function collectCategoryItems(rules, applications) {
  const counts = new Map();

  for (const rule of rules) {
    if (rule.scopeName) {
      counts.set(rule.scopeName, (counts.get(rule.scopeName) ?? 0) + 1);
    }
  }

  for (const application of applications) {
    if (application.category) {
      counts.set(application.category, counts.get(application.category) ?? 0);
    }
  }

  return [...counts.entries()]
    .map(([name, count]) => {
      const application = applications.find((item) => item.name === name);
      return {
        name,
        count,
        displayName: application?.displayName || name,
        icon: application?.icon || ""
      };
    })
    .sort((left, right) => left.name.localeCompare(right.name, "zh-Hans-CN"));
}

export function collectAppItems(rules, applications) {
  const counts = new Map();

  for (const rule of rules) {
    if (rule.scopeName) {
      counts.set(rule.scopeName, (counts.get(rule.scopeName) ?? 0) + 1);
    }
  }

  for (const application of applications) {
    if (application.name) {
      counts.set(application.name, counts.get(application.name) ?? 0);
    }
  }

  return [...counts.entries()]
    .map(([name, count]) => {
      const application = applications.find((item) => item.name === name);
      return {
        name,
        count,
        displayName: application?.displayName || name,
        icon: application?.icon || ""
      };
    })
    .sort((left, right) => left.name.localeCompare(right.name, "zh-Hans-CN"));
}
