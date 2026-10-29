import { describe, expect, it } from 'vitest'
import {
  buildScope,
  clampFloat,
  clampInteger,
  isSameApplicationIdentity,
  normalizeActionType,
  normalizeAmount,
  normalizeAppearanceSettings,
  normalizeBrightnessOperation,
  normalizeEdgeAction,
  normalizeEdgeActions,
  normalizeExcludedApplications,
  normalizeFrictionCount,
  normalizeLevelOsdSettings,
  normalizeMouseButton,
  normalizeMouseTrailSettings,
  normalizeObjectKeys,
  normalizeUpdateSettings,
  normalizeVolumeOperation,
  normalizeWheelDirection,
  normalizeWindowOperation,
  parseKeys,
  parsePattern,
  parseScope
} from './gestureEditorNormalizers'

describe('normalizeObjectKeys', () => {
  it('把首字母大写的键转换为驼峰命名', () => {
    expect(normalizeObjectKeys({ CloseButtonBehavior: 'exit' })).toEqual({ closeButtonBehavior: 'exit' })
  })

  it('对非对象输入返回空对象', () => {
    expect(normalizeObjectKeys(null)).toEqual({})
    expect(normalizeObjectKeys('text')).toEqual({})
  })
})

describe('clampInteger / clampFloat', () => {
  it('把越界数值收敛到范围内', () => {
    expect(clampInteger(5, 10, 90, 10)).toBe(10)
    expect(clampInteger(120, 10, 90, 10)).toBe(90)
    expect(clampFloat(0.5, 1, 20, 3)).toBe(1)
    expect(clampFloat(25, 1, 20, 3)).toBe(20)
  })

  it('无法解析时使用回退值', () => {
    expect(clampInteger('abc', 0, 100, 7)).toBe(7)
    expect(clampFloat(undefined, 0, 1, 0.5)).toBe(0.5)
  })
})

describe('枚举与数值规范化', () => {
  it('鼠标按键只接受 middle，其余回退到 right', () => {
    expect(normalizeMouseButton('MIDDLE')).toBe('middle')
    expect(normalizeMouseButton('left')).toBe('right')
  })

  it('动作类型大小写不敏感，未知值回退到 hotkey', () => {
    expect(normalizeActionType('VOLUME')).toBe('volume')
    expect(normalizeActionType(undefined)).toBe('hotkey')
  })

  it('滚轮方向只接受 down，其余回退到 up', () => {
    expect(normalizeWheelDirection('DOWN')).toBe('down')
    expect(normalizeWheelDirection('sideways')).toBe('up')
  })

  it('步进值收敛到 1-100，非正数视为未设置', () => {
    expect(normalizeAmount(0)).toBe(5)
    expect(normalizeAmount(-3)).toBe(5)
    expect(normalizeAmount(1)).toBe(1)
    expect(normalizeAmount(200)).toBe(100)
    expect(normalizeAmount('x')).toBe(5)
  })

  it('摩擦次数收敛到 1-20', () => {
    expect(normalizeFrictionCount(0)).toBe(1)
    expect(normalizeFrictionCount(100)).toBe(20)
    expect(normalizeFrictionCount(null)).toBe(4)
  })

  it('动作操作大小写不敏感，未知值回退到默认操作', () => {
    expect(normalizeVolumeOperation(' DECREASE ')).toBe('decrease')
    expect(normalizeBrightnessOperation('Decrease')).toBe('decrease')
    expect(normalizeWindowOperation('CLOSE')).toBe('close')
    expect(normalizeWindowOperation('explode')).toBe('toggle-maximize')
  })
})

describe('作用域与手势文本', () => {
  it('解析全局、分类和程序作用域', () => {
    expect(parseScope('')).toEqual({ kind: 'global', name: '' })
    expect(parseScope('global')).toEqual({ kind: 'global', name: '' })
    expect(parseScope('Category:工具')).toEqual({ kind: 'category', name: '工具' })
    expect(parseScope('app:Code.exe')).toEqual({ kind: 'app', name: 'Code.exe' })
  })

  it('缺少名称或未知类型时回退到全局', () => {
    expect(parseScope('category:')).toEqual({ kind: 'global', name: '' })
    expect(parseScope('other:x')).toEqual({ kind: 'global', name: '' })
  })

  it('拼装作用域字符串', () => {
    expect(buildScope('category', ' 工具 ')).toBe('category:工具')
    expect(buildScope('app', '')).toBe('')
    expect(buildScope('global', 'ignored')).toBe('global')
  })

  it('按空白、逗号和中文逗号拆分手势方向', () => {
    expect(parsePattern('Down, Right 左，Up')).toEqual(['Down', 'Right', '左', 'Up'])
    expect(parsePattern('')).toEqual([])
  })

  it('按加号拆分快捷键', () => {
    expect(parseKeys('Control + W')).toEqual(['Control', 'W'])
    expect(parseKeys('')).toEqual([])
  })
})

describe('界面设置规范化', () => {
  it('轨迹颜色去掉不透明的 #FF 前缀', () => {
    const trail = normalizeMouseTrailSettings({ inactiveColor: '#ff112233', activeColor: '   ' })

    expect(trail.inactiveColor).toBe('#112233')
    expect(trail.activeColor).toBe('#87CEEB')
  })

  it('OSD 圆角不超过宽高较小值的一半', () => {
    expect(normalizeLevelOsdSettings({ width: 120, height: 100, cornerRadius: 210 }).cornerRadius).toBe(50)
    expect(normalizeLevelOsdSettings({ cornerRadius: -5 }).cornerRadius).toBe(0)
  })

  it('更新源大小写不敏感，未知值回退到 gitee', () => {
    expect(normalizeUpdateSettings({ source: 'GITHUB' }).source).toBe('github')
    expect(normalizeUpdateSettings({}).source).toBe('gitee')
  })

  it('主题大小写不敏感，未知值回退到默认主题', () => {
    expect(normalizeAppearanceSettings({ theme: 'DARK' }).theme).toBe('dark')
    expect(normalizeAppearanceSettings({ theme: 'neon' }).theme).toBe('dark')
  })

  it('排除项去除空白、忽略空项并按路径去重', () => {
    const result = normalizeExcludedApplications([
      { name: 'code.exe', path: ' C:\\Code\\Code.exe ' },
      { name: 'code.exe', path: 'c:\\code\\code.exe' },
      { name: '', path: '' }
    ])

    expect(result).toHaveLength(1)
    expect(result[0]).toMatchObject({ name: 'code', displayName: 'code', path: 'C:\\Code\\Code.exe' })
  })

  it('排除项按进程名归一化，扩展名和目录不参与身份比较', () => {
    const names = entries => normalizeExcludedApplications(entries).map(item => item.name)

    expect(names([{ name: 'C:\\Tools\\notepad.exe' }])).toEqual(['notepad'])
    expect(names([{ name: 'notepad.exe' }, { name: 'notepad' }])).toEqual(['notepad'])
    expect(isSameApplicationIdentity({ name: 'Code.EXE' }, { name: 'C:\\Code\\code' })).toBe(true)
    expect(isSameApplicationIdentity({ name: 'code.exe' }, { name: 'other.exe' })).toBe(false)
  })

  it('排除项缺少显示名时回退到名称', () => {
    expect(normalizeExcludedApplications([{ name: 'notepad' }])[0].displayName).toBe('notepad')
  })
})

describe('边缘操作规范化', () => {
  it('滚轮操作保留方向并收敛步进值', () => {
    const action = normalizeEdgeAction({
      triggerType: 'wheel',
      location: 'left',
      wheelDirection: 'DOWN',
      action: { type: 'volume', operation: 'decrease', amount: 0 }
    })

    expect(action).toMatchObject({
      triggerType: 'wheel',
      location: 'left',
      wheelDirection: 'down',
      actionType: 'volume',
      volumeOperation: 'decrease',
      amount: 5
    })
  })

  it('程序动作在边缘操作中退化为快捷键，非法位置回退到首个默认位置', () => {
    const action = normalizeEdgeAction({
      triggerType: 'corner',
      location: 'not-a-corner',
      action: { type: 'program', path: 'C:\\a.exe' }
    })

    expect(action).toMatchObject({ actionType: 'hotkey', location: 'top-left' })
  })

  it('合并默认边缘操作并覆盖相同触发位置的配置', () => {
    const defaults = normalizeEdgeActions([])
    const merged = normalizeEdgeActions([
      { triggerType: 'corner', location: 'top-left', enabled: true, keysText: 'Alt + Tab' }
    ])

    expect(defaults).toHaveLength(16)
    expect(merged).toHaveLength(16)
    expect(merged.find(action => action.triggerType === 'corner' && action.location === 'top-left').keysText).toBe(
      'Alt + Tab'
    )
  })
})
