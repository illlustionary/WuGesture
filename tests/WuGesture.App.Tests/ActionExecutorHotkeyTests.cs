using System.Windows.Forms;
using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class ActionExecutorHotkeyTests
{
    [Theory]
    [InlineData(Keys.A)]
    [InlineData(Keys.C)]
    [InlineData(Keys.V)]
    [InlineData(Keys.ControlKey)]
    [InlineData(Keys.ShiftKey)]
    public void ResolveScanCode_ReturnsNonZeroScanCodeForCommonKeys(Keys key)
    {
        var scanCode = ActionExecutor.ResolveScanCode(key, IntPtr.Zero);

        Assert.NotEqual<ushort>(0, scanCode.Code);
        Assert.False(scanCode.IsExtended);
    }

    [Theory]
    [InlineData(Keys.Left, 0x4B)]
    [InlineData(Keys.Right, 0x4D)]
    [InlineData(Keys.Up, 0x48)]
    [InlineData(Keys.Down, 0x50)]
    [InlineData(Keys.RControlKey, 0x1D)]
    [InlineData(Keys.RMenu, 0x38)]
    public void ResolveScanCode_MarksExtendedKeys(Keys key, int expectedScanCode)
    {
        var scanCode = ActionExecutor.ResolveScanCode(key, IntPtr.Zero);

        Assert.Equal((ushort)expectedScanCode, scanCode.Code);
        Assert.True(scanCode.IsExtended);
    }

    [Fact]
    public void ResolveScanCode_GivesDistinctScanCodesToDistinctLetters()
    {
        var c = ActionExecutor.ResolveScanCode(Keys.C, IntPtr.Zero);
        var v = ActionExecutor.ResolveScanCode(Keys.V, IntPtr.Zero);

        Assert.NotEqual(c.Code, v.Code);
        Assert.NotEqual<ushort>(0, c.Code);
        Assert.NotEqual<ushort>(0, v.Code);
    }

    [Fact]
    public void InterpretScanCode_PreservesPlainScanCode()
    {
        var result = ActionExecutor.InterpretScanCode(0x001D, knownExtended: false);

        Assert.Equal<ushort>(0x1D, result.Code);
        Assert.False(result.IsExtended);
    }

    [Fact]
    public void InterpretScanCode_DetectsExtendedPrefix()
    {
        var e0 = ActionExecutor.InterpretScanCode(0xE02F, knownExtended: false);
        var e1 = ActionExecutor.InterpretScanCode(0xE11D, knownExtended: false);

        Assert.Equal<ushort>(0x2F, e0.Code);
        Assert.True(e0.IsExtended);
        Assert.Equal<ushort>(0x1D, e1.Code);
        Assert.True(e1.IsExtended);
    }

    [Fact]
    public void InterpretScanCode_KeepsExtendedFlagWhenTranslationFallsBack()
    {
        var result = ActionExecutor.InterpretScanCode(0, knownExtended: true);

        Assert.Equal<ushort>(0, result.Code);
        Assert.True(result.IsExtended);
    }

    [Fact]
    public void ResolveKeyboardLayout_ReturnsCurrentLayoutForMissingWindow()
    {
        Assert.NotEqual(IntPtr.Zero, ActionExecutor.ResolveKeyboardLayout(IntPtr.Zero));
    }
}
