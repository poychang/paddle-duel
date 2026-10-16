using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace PaddleDuel.Tests;

public sealed class AppResourceTests
{
    private static readonly string ResourceDirectory = Path.Combine(AppContext.BaseDirectory, "AppResources");

    [Fact]
    public void GameplayInputUsesVisibleSurfaceAndPreviewRoutingInsteadOfInvisibleButton()
    {
        var document = XDocument.Load(Path.Combine(ResourceDirectory, "MainWindow.xaml"));
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        Assert.DoesNotContain(document.Descendants(),
            element => element.Attribute(xaml + "Name")?.Value == "InputSink");
        var surface = Assert.Single(document.Descendants(),
            element => element.Attribute(xaml + "Name")?.Value == "GameRoot");
        Assert.Equal("UserControl", surface.Name.LocalName);
        Assert.Equal("True", surface.Attribute("UseSystemFocusVisuals")?.Value);
        var root = Assert.Single(document.Descendants(),
            element => element.Attribute(xaml + "Name")?.Value == "WindowRoot");
        Assert.Equal("WindowRoot_PreviewKeyDown", root.Attribute("PreviewKeyDown")?.Value);
    }

    private static Dictionary<string, string> Load(string language)
    {
        return XDocument.Load(Path.Combine(ResourceDirectory, language, "Resources.resw"))
            .Root!.Elements("data")
            .ToDictionary(
                element => element.Attribute("name")!.Value,
                element => element.Element("value")!.Value,
                StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Languages_HaveIdenticalNonemptyKeysAndFormatArguments()
    {
        var english = Load("en-US");
        var chinese = Load("zh-TW");
        Assert.Equal(english.Keys.Order(), chinese.Keys.Order());
        foreach (var (key, value) in english)
        {
            Assert.False(string.IsNullOrWhiteSpace(value), key);
            Assert.False(string.IsNullOrWhiteSpace(chinese[key]), key);
            var arguments = Regex.Matches(value, @"\{\d+(?:[^}]*)\}").Select(match => match.Value).Order();
            var translatedArguments = Regex.Matches(chinese[key], @"\{\d+(?:[^}]*)\}").Select(match => match.Value).Order();
            Assert.Equal(arguments, translatedArguments);
            _ = string.Format(CultureInfo.InvariantCulture, value, 1, 3);
            _ = string.Format(CultureInfo.InvariantCulture, chinese[key], 1, 3);
        }
    }

    [Fact]
    public void Xaml_LocalizablePropertiesHaveValidResourceReferences()
    {
        var resources = Load("en-US");
        var document = XDocument.Load(Path.Combine(ResourceDirectory, "MainWindow.xaml"));
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        foreach (var element in document.Descendants())
        {
            foreach (var attribute in element.Attributes().Where(attribute =>
                attribute.Name.LocalName is "Text" or "Content" or "AutomationProperties.Name" or "ToolTipService.ToolTip"))
            {
                Assert.True(attribute.Value == "0" || attribute.Value.StartsWith('{'),
                    $"Unlocalized {element.Name.LocalName}.{attribute.Name}: {attribute.Value}");
            }

            if (element.Attribute(xaml + "Uid") is not { } uid)
            {
                continue;
            }

            var keys = resources.Keys.Where(key => key.StartsWith(uid.Value + ".", StringComparison.Ordinal)).ToArray();
            Assert.NotEmpty(keys);
            foreach (var key in keys)
            {
                var property = key[(uid.Value.Length + 1)..];
                Assert.True(property switch
                {
                    "Text" => element.Name.LocalName == "TextBlock",
                    "Content" => element.Name.LocalName == "Button",
                    "[using:Microsoft.UI.Xaml.Automation]AutomationProperties.Name" => true,
                    "[using:Microsoft.UI.Xaml.Controls]ToolTipService.ToolTip" => true,
                    _ => false,
                }, $"Invalid localized property: {key}");
            }
        }
    }

    [Fact]
    public void DynamicMessagesAndDifficultyLabels_ExistInBothLanguages()
    {
        var source = File.ReadAllText(Path.Combine(ResourceDirectory, "MainWindow.xaml.cs"));
        var keys = Regex.Matches(source, @"AppStrings\.(?:Get|Format)\(""([^""]+)""")
            .Select(match => match.Groups[1].Value)
            .Concat(["DifficultyEasy", "DifficultyMedium", "DifficultyHard"]);
        foreach (var language in new[] { "en-US", "zh-TW" })
        {
            var resources = Load(language);
            foreach (var key in keys)
            {
                Assert.True(resources.ContainsKey(key), $"{language}: {key}");
            }
            Assert.Equal("Paddle Duel", resources["AppDisplayName"]);
        }
    }
}
