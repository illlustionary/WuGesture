# My Gesture

Windows mouse gesture prototype using C# WinForms, WebView2, and a native global mouse hook.

## Current State

- Desktop shell: `src/MyGesture.App`
- Frontend shell: `src/MyGesture.App/Web`
- Existing AutoHotkey experiment: `gesture.ahk`
- Gesture config: `%AppData%\MyGesture\gestures.json`

The first prototype has three built-in global gestures:

| Gesture | Action |
| --- | --- |
| Left | Alt + Left |
| Right | Alt + Right |
| Down, Right | Ctrl + W |

During gesture tracking, the original right-button down/up events are swallowed so other apps do not receive a right-button drag. If the movement is too small to count as a gesture, the app replays a normal right-click.

## Run

```powershell
dotnet run --project src\MyGesture.App\MyGesture.App.csproj
```

## Build

```powershell
dotnet build MyGesture.slnx
```

## Publish

```powershell
.\scripts\publish.ps1
```

The default publish output is ignored by Git:

```text
artifacts\publish\MyGesture
```

## Architecture

```text
MouseHook -> GestureService -> GestureRecognizer -> GestureMatcher -> Action execution
                                      |
                                      v
                                  WebView2 UI
```

The native side owns the global mouse hook, gesture recognition, and action execution.
The WebView UI is currently a static status and rule page. Later it should become the rule editor.

## Next Steps

1. Add a visual gesture trail overlay.
2. Add app detection and scope priority: app > category > global.
3. Add AutoHotkey as an optional action type.
4. Add config import/export once the rule editor stabilizes.
