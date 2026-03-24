using KnowledgeMap.Backend.Models;
using KnowledgeMap.Backend.Services;

namespace mindmap_back.Tests.Services;

public class TypeScopeMapperTests
{
    [Fact]
    public void GetNodeTypeIds_ReturnsSystemOrCustomIdDependingOnScope()
    {
        var systemType = new NodeType { Id = 1, IsSystem = true };
        var customType = new NodeType { Id = 2, IsSystem = false };

        Assert.Equal(1, TypeScopeMapper.GetSystemNodeTypeId(systemType));
        Assert.Null(TypeScopeMapper.GetCustomNodeTypeId(systemType));
        Assert.Null(TypeScopeMapper.GetSystemNodeTypeId(customType));
        Assert.Equal(2, TypeScopeMapper.GetCustomNodeTypeId(customType));
    }

    [Fact]
    public void GetEdgeTypeIds_ReturnsSystemOrCustomIdDependingOnScope()
    {
        var systemType = new EdgeType { Id = 1, IsSystem = true };
        var customType = new EdgeType { Id = 2, IsSystem = false };

        Assert.Equal(1, TypeScopeMapper.GetSystemEdgeTypeId(systemType));
        Assert.Null(TypeScopeMapper.GetCustomEdgeTypeId(systemType));
        Assert.Null(TypeScopeMapper.GetSystemEdgeTypeId(customType));
        Assert.Equal(2, TypeScopeMapper.GetCustomEdgeTypeId(customType));
    }

    [Fact]
    public void NullTypes_ReturnNullForAllScopeQueries()
    {
        Assert.Null(TypeScopeMapper.GetSystemNodeTypeId(null));
        Assert.Null(TypeScopeMapper.GetCustomNodeTypeId(null));
        Assert.Null(TypeScopeMapper.GetSystemEdgeTypeId(null));
        Assert.Null(TypeScopeMapper.GetCustomEdgeTypeId(null));
    }
}
