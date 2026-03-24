using KnowledgeMap.Backend.Models;

namespace KnowledgeMap.Backend.Services
{
    public static class TypeScopeMapper
    {
        public static int? GetSystemNodeTypeId(NodeType? type)
        {
            return type?.IsSystem == true ? type.Id : null;
        }

        public static int? GetCustomNodeTypeId(NodeType? type)
        {
            return type?.IsSystem == false ? type.Id : null;
        }

        public static int? GetSystemEdgeTypeId(EdgeType? type)
        {
            return type?.IsSystem == true ? type.Id : null;
        }

        public static int? GetCustomEdgeTypeId(EdgeType? type)
        {
            return type?.IsSystem == false ? type.Id : null;
        }
    }
}
