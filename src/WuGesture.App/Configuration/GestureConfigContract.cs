namespace WuGesture.App.GestureEngine;

public static class GestureConfigContract
{
    public static class Schema
    {
        public const int CurrentVersion = 1;
    }

    public static class Scopes
    {
        public const string Global = "global";
        public const string App = "app";
        public const string Category = "category";
    }

    public static class MouseButtons
    {
        public const string Right = "right";
        public const string Middle = "middle";
    }

    public static class ActionTypes
    {
        public const string Hotkey = "hotkey";
        public const string Window = "window";
        public const string Volume = "volume";
        public const string Brightness = "brightness";
        public const string Program = "program";
    }

    public static class Operations
    {
        public const string ToggleTopMost = "toggle-topmost";
        public const string ToggleMaximize = "toggle-maximize";
        public const string Minimize = "minimize";
        public const string Close = "close";
        public const string Increase = "increase";
        public const string Decrease = "decrease";
        public const string Mute = "mute";
    }

    public static class EdgeTriggerTypes
    {
        public const string Corner = "corner";
        public const string Friction = "friction";
        public const string Wheel = "wheel";
    }

    public static class EdgeLocations
    {
        public const string TopLeft = "top-left";
        public const string TopRight = "top-right";
        public const string BottomLeft = "bottom-left";
        public const string BottomRight = "bottom-right";
        public const string Left = "left";
        public const string Right = "right";
        public const string Top = "top";
        public const string Bottom = "bottom";
    }

    public static class WheelDirections
    {
        public const string Up = "up";
        public const string Down = "down";
    }

    public static class CloseButtonBehaviors
    {
        public const string MinimizeToTray = "minimize-to-tray";
        public const string MinimizeToTaskbar = "minimize-to-taskbar";
        public const string Exit = "exit";
    }

    public static class WindowTargetModes
    {
        public const string StartWindow = "start-window";
        public const string CurrentWindow = "current-window";
    }

    public static class AppearanceThemes
    {
        public const string System = "system";
        public const string Light = "light";
        public const string Dark = "dark";
    }

    public static class LevelOsdPositions
    {
        public const string Center = "center";
        public const string TopCenter = "top-center";
        public const string BottomCenter = "bottom-center";
        public const string TopLeft = "top-left";
        public const string TopRight = "top-right";
        public const string BottomLeft = "bottom-left";
        public const string BottomRight = "bottom-right";
    }

}
