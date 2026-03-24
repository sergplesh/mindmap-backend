using System.Text.Json;
using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using KnowledgeMap.Backend.Services;

namespace mindmap_back.Tests.Services;

public class NodeTypeFieldMapperTests
{
    [Fact]
    public void ToDtos_OrdersDefinitionsAndOptionsAndMapsMetadata()
    {
        var definitions = new[]
        {
            new NodeTypeFieldDefinition
            {
                Name = "Second",
                FieldType = "text",
                IsRequired = false,
                SortOrder = 1,
                Options =
                [
                    new NodeTypeFieldOption { Value = "B", SortOrder = 1 },
                    new NodeTypeFieldOption { Value = "A", SortOrder = 0 }
                ]
            },
            new NodeTypeFieldDefinition
            {
                Name = "First",
                FieldType = "number",
                IsRequired = true,
                DefaultValue = "10",
                Placeholder = "value",
                Validation = "min:1",
                SortOrder = 0
            }
        };

        var result = NodeTypeFieldMapper.ToDtos(definitions);

        Assert.Equal(2, result.Count);
        Assert.Equal("First", result[0].Name);
        Assert.Equal("number", result[0].Type);
        Assert.True(result[0].Required);
        Assert.Equal("10", result[0].DefaultValue);
        Assert.Equal("Second", result[1].Name);
        Assert.Equal(new[] { "A", "B" }, result[1].Options);
    }

    [Fact]
    public void ToDtos_ReturnsEmptyList_WhenDefinitionsAreNull()
    {
        var result = NodeTypeFieldMapper.ToDtos(null);

        Assert.Empty(result);
    }

    [Fact]
    public void ToValueDictionary_ParsesStoredNumbersBooleansAndStrings()
    {
        var values = new[]
        {
            new NodeFieldValue
            {
                Value = "42.5",
                FieldDefinition = new NodeTypeFieldDefinition
                {
                    Name = "Score",
                    FieldType = "number",
                    SortOrder = 1
                }
            },
            new NodeFieldValue
            {
                Value = "true",
                FieldDefinition = new NodeTypeFieldDefinition
                {
                    Name = "Enabled",
                    FieldType = "checkbox",
                    SortOrder = 0
                }
            },
            new NodeFieldValue
            {
                Value = null,
                FieldDefinition = new NodeTypeFieldDefinition
                {
                    Name = "Comment",
                    FieldType = "text",
                    SortOrder = 2
                }
            }
        };

        var result = NodeTypeFieldMapper.ToValueDictionary(values);

        Assert.NotNull(result);
        Assert.Equal(true, result!["Enabled"]);
        Assert.Equal(42.5d, result["Score"]);
        Assert.Equal(string.Empty, result["Comment"]);
    }

    [Fact]
    public void ToValueDictionary_ReturnsNull_WhenValuesAreNull()
    {
        Assert.Null(NodeTypeFieldMapper.ToValueDictionary(null));
    }

    [Fact]
    public void ToStorageString_ConvertsJsonElementsAndPrimitives()
    {
        using var json = JsonDocument.Parse("""
            {
              "text": "hello",
              "number": 12.5,
              "flag": true,
              "nothing": null
            }
            """);

        Assert.Equal("hello", NodeTypeFieldMapper.ToStorageString(json.RootElement.GetProperty("text")));
        Assert.Equal("12.5", NodeTypeFieldMapper.ToStorageString(json.RootElement.GetProperty("number")));
        Assert.Equal("true", NodeTypeFieldMapper.ToStorageString(json.RootElement.GetProperty("flag")));
        Assert.Null(NodeTypeFieldMapper.ToStorageString(json.RootElement.GetProperty("nothing")));
        Assert.Equal("15", NodeTypeFieldMapper.ToStorageString(15));
        Assert.Equal("false", NodeTypeFieldMapper.ToStorageString(false));
    }
}
