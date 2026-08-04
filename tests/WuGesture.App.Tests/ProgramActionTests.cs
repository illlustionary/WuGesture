using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class ProgramActionTests
{
    [Fact]
    public void Normalize_TrimsAndFiltersProgramArguments()
    {
        var config = GestureConfigNormalizer.Normalize(new GestureConfig
        {
            Rules =
            [
                new GestureRuleConfig
                {
                    Action = new GestureActionConfig
                    {
                        Type = GestureConfigContract.ActionTypes.Program,
                        Path = "  C:\\Tools\\App.exe  ",
                        Arguments = ["  --first  ", " ", "--second"]
                    }
                }
            ]
        });

        var action = Assert.IsType<ProgramAction>(GestureConfigMapper.ToAction(config.Rules[0].Action));

        Assert.Equal("C:\\Tools\\App.exe", action.Path);
        Assert.Equal(["--first", "--second"], action.Arguments);
    }

    [Fact]
    public void ToAction_ReturnsNullForProgramWithoutPath()
    {
        var action = GestureConfigMapper.ToAction(new GestureActionConfig
        {
            Type = GestureConfigContract.ActionTypes.Program,
            Arguments = ["--profile"]
        });

        Assert.Null(action);
    }

    [Fact]
    public void CreateProgramStartInfo_UsesLiteralArgumentsWithoutShell()
    {
        var startInfo = ActionExecutor.CreateProgramStartInfo(new ProgramAction(
            "C:\\Tools\\App.exe",
            ["--profile-directory=Default", "https://example.com/a b"]));

        Assert.False(startInfo.UseShellExecute);
        Assert.Equal("C:\\Tools\\App.exe", startInfo.FileName);
        Assert.Equal("C:\\Tools", startInfo.WorkingDirectory);
        Assert.Equal(["--profile-directory=Default", "https://example.com/a b"], startInfo.ArgumentList);
    }
}
