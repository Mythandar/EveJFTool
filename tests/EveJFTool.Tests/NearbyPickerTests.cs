using EveJFTool.App.ViewModels;

namespace EveJFTool.Tests;

public class NearbyPickerTests
{
    [Fact]
    public void FilteringIsCaseInsensitiveAndClearsHiddenSelection()
    {
        var model = new NearbySystemsViewModel { Heading = "Test" };
        model.SetResults([new("Alpha", 1, .1), new("Beta", 2, 0)]);
        model.Selected = model.Matches[0];
        model.Filter = " BET ";
        Assert.Equal("Beta", Assert.Single(model.Matches).Name);
        Assert.Null(model.Selected);
        model.Filter = "";
        Assert.Equal(2, model.Matches.Count);
    }
}
