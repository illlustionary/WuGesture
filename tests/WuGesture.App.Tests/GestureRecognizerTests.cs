using System.Drawing;
using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class GestureRecognizerTests
{
    [Fact]
    public void Recognize_ReturnsEmptyForMovementBelowTheEffectiveDistance()
    {
        var recognizer = new GestureRecognizer();

        var result = recognizer.Recognize([new Point(0, 0), new Point(20, 0)]);

        Assert.Empty(result);
    }

    [Fact]
    public void Recognize_PreservesEightDirectionsForASingleStroke()
    {
        var recognizer = new GestureRecognizer();

        var result = recognizer.Recognize([new Point(0, 0), new Point(100, 100)]);

        Assert.Equal([GestureDirection.DownRight], result);
    }

    [Fact]
    public void Recognize_NormalizesTheFirstDiagonalOfAMultiStrokeGesture()
    {
        var recognizer = new GestureRecognizer();

        var result = recognizer.Recognize(
        [
            new Point(0, 0),
            new Point(100, -60),
            new Point(100, 100)
        ]);

        Assert.Equal([GestureDirection.Right, GestureDirection.Down], result);
    }

    [Fact]
    public void ApplySensitivity_MakesShortMovementsRecognizableAtTheRelaxedProfile()
    {
        var recognizer = new GestureRecognizer();
        recognizer.ApplySensitivity(200);

        var result = recognizer.Recognize([new Point(0, 0), new Point(20, 0)]);

        Assert.Equal([GestureDirection.Right], result);
    }
}
