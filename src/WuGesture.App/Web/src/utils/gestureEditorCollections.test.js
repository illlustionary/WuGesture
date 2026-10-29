import { describe, expect, it } from 'vitest'
import { collectAppItems, collectCategoryItems } from './gestureEditorCollections'

const applications = [
  { name: 'code', displayName: 'VS Code', categories: ['dev', 'editor'], icon: 'code-icon' },
  { name: 'chrome', displayName: '', categories: ['browser', 'dev'], icon: 'chrome-icon' }
]

describe('collectCategoryItems', () => {
  it('统计规则、程序和空分类，并带上首个程序信息', () => {
    const items = collectCategoryItems(
      [
        { scopeKind: 'category', scopeName: 'dev' },
        { scopeKind: 'category', scopeName: 'dev' },
        { scopeKind: 'category', scopeName: 'empty-rules' }
      ],
      applications,
      ['dev', 'only-empty']
    )

    const byName = new Map(items.map(item => [item.name, item]))

    expect(byName.get('dev')).toMatchObject({
      count: 2,
      displayName: 'VS Code',
      icon: 'code-icon'
    })
    expect(byName.get('browser')).toMatchObject({ count: 0, displayName: 'browser', icon: 'chrome-icon' })
    expect(byName.get('editor')).toMatchObject({ count: 0, displayName: 'VS Code' })
    expect(byName.get('empty-rules')).toMatchObject({ count: 1 })
    expect(byName.get('only-empty')).toMatchObject({ count: 0 })
  })

  it('忽略空分类并按名称排序', () => {
    const items = collectCategoryItems([], [{ name: 'a', categories: ['b', 'a'] }], ['  ', 'c'])

    expect(items.map(item => item.name)).toEqual(['a', 'b', 'c'])
  })
})

describe('collectAppItems', () => {
  it('合并规则与程序条目并统计规则数量', () => {
    const items = collectAppItems(
      [
        { scopeKind: 'app', scopeName: 'code' },
        { scopeKind: 'app', scopeName: 'code' },
        { scopeKind: 'app', scopeName: 'notepad' }
      ],
      applications
    )

    const byName = new Map(items.map(item => [item.name, item]))

    expect(byName.get('code')).toMatchObject({ count: 2, displayName: 'VS Code', icon: 'code-icon' })
    expect(byName.get('chrome')).toMatchObject({ count: 0, displayName: 'chrome', icon: 'chrome-icon' })
    expect(byName.get('notepad')).toMatchObject({ count: 1, displayName: 'notepad' })
    expect(items.map(item => item.name)).toEqual(['chrome', 'code', 'notepad'])
  })

  it('忽略没有名称的程序条目', () => {
    expect(collectAppItems([], [{ name: ' ', displayName: '空' }])).toEqual([])
  })
})
