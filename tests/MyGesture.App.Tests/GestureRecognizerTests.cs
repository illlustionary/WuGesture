using MyGesture.App.GestureEngine;
using System.Drawing;

namespace MyGesture.App.Tests;

public sealed class GestureRecognizerTests
{
    private readonly GestureRecognizer recognizer = new();

    [Fact]
    public void Recognize_IgnoresHorizontalJitter()
    {
        var pattern = recognizer.Recognize(
        [
            new Point(0, 0),
            new Point(20, 3),
            new Point(42, -4),
            new Point(68, 2),
            new Point(96, -3),
            new Point(132, 0)
        ]);

        Assert.Equal([GestureDirection.Right], pattern);
    }

    [Fact]
    public void Recognize_CompressesNoisyDownRightTurn()
    {
        var pattern = recognizer.Recognize(
        [
            new Point(0, 0),
            new Point(2, 24),
            new Point(-3, 55),
            new Point(4, 88),
            new Point(24, 102),
            new Point(58, 105),
            new Point(94, 101)
        ]);

        Assert.Equal([GestureDirection.Down, GestureDirection.Right], pattern);
    }

    [Fact]
    public void Recognize_DropsShortBacktrackAtEnd()
    {
        var pattern = recognizer.Recognize(
        [
            new Point(0, 0),
            new Point(36, 2),
            new Point(72, -2),
            new Point(98, 0),
            new Point(84, 1)
        ]);

        Assert.Equal([GestureDirection.Right], pattern);
    }

    [Fact]
    public void Recognize_SupportsDiagonalDirections()
    {
        var pattern = recognizer.Recognize(
        [
            new Point(0, 0),
            new Point(24, 24),
            new Point(52, 50),
            new Point(84, 82)
        ]);

        Assert.Equal([GestureDirection.DownRight], pattern);
    }
}
