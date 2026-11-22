using System.Windows.Forms;
using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class HotkeyDeliveryPolicyTests
{
    [Theory]
    [InlineData(Keys.Escape)]
    [InlineData(Keys.Enter)]
    [InlineData(Keys.Space)]
    [InlineData(Keys.Left)]
    public void CanPostToTarget_AllowsSingleNonModifierKey(Keys key)
    {
        Assert.True(HotkeyDeliveryPolicy.CanPostToTarget(new HotkeyAction([key])));
    }

    [Theory]
    [InlineData(Keys.ControlKey)]
    [InlineData(Keys.LControlKey)]
    [InlineData(Keys.ShiftKey)]
    [InlineData(Keys.Menu)]
    [InlineData(Keys.LWin)]
    [InlineData(Keys.RWin)]
    [InlineData(Keys.None)]
    public void CanPostToTarget_RejectsModifierOrEmptyKey(Keys key)
    {
        Assert.False(HotkeyDeliveryPolicy.CanPostToTarget(new HotkeyAction([key])));
    }

    [Fact]
    public void CanPostToTarget_RejectsCombinations()
    {
        Assert.False(HotkeyDeliveryPolicy.CanPostToTarget(new HotkeyAction([Keys.ControlKey, Keys.C])));
        Assert.False(HotkeyDeliveryPolicy.CanPostToTarget(new HotkeyAction([Keys.ControlKey, Keys.ShiftKey, Keys.Escape])));
        Assert.False(HotkeyDeliveryPolicy.CanPostToTarget(new HotkeyAction([Keys.Control | Keys.C])));
        Assert.False(HotkeyDeliveryPolicy.CanPostToTarget(new HotkeyAction([])));
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    public void IsForegroundUnrecoverable_OnlyWhenMinimizedOrShellUi(
        bool minimized,
        bool shellUi,
        bool expected)
    {
        Assert.Equal(expected, HotkeyDeliveryPolicy.IsForegroundUnrecoverable(minimized, shellUi));
    }

    [Fact]
    public void BuildKeyMessageLParam_PlainKeyDown()
    {
        var lParam = HotkeyDeliveryPolicy.BuildKeyMessageLParam(0x01, isExtended: false, keyUp: false);

        Assert.Equal(0x00010001, lParam);
    }

    [Fact]
    public void BuildKeyMessageLParam_PlainKeyUpSetsPreviousStateAndTransition()
    {
        var lParam = HotkeyDeliveryPolicy.BuildKeyMessageLParam(0x01, isExtended: false, keyUp: true);

        Assert.Equal(unchecked((int)0xC0010001), lParam);
    }

    [Fact]
    public void BuildKeyMessageLParam_ExtendedKeySetsExtendedBit()
    {
        var down = HotkeyDeliveryPolicy.BuildKeyMessageLParam(0x4B, isExtended: true, keyUp: false);
        var up = HotkeyDeliveryPolicy.BuildKeyMessageLParam(0x4B, isExtended: true, keyUp: true);

        Assert.Equal(0x014B0001, down);
        Assert.Equal(unchecked((int)0xC14B0001), up);
    }

    [Theory]
    [InlineData((int)HotkeyForegroundOutcome.NotApplicable, "target-not-application-window")]
    [InlineData((int)HotkeyForegroundOutcome.AlreadyForeground, "already-foreground")]
    [InlineData((int)HotkeyForegroundOutcome.Activated, "activated")]
    [InlineData((int)HotkeyForegroundOutcome.ForegroundUnrecoverable, "foreground-minimized-or-shell-ui")]
    [InlineData((int)HotkeyForegroundOutcome.ActivationDenied, "activation-denied")]
    public void Describe_ReturnsStableLogToken(int outcome, string expected)
    {
        Assert.Equal(expected, HotkeyDeliveryPolicy.Describe((HotkeyForegroundOutcome)outcome));
    }
}
