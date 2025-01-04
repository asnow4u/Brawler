
namespace Game.Navigation
{
    public class FlyingEdge : Edge
    {
        public FlyingEdge(GraphNode startNode, GraphNode endNode) : base(startNode, endNode)
        {
            edgeType = EdgeType.Fly;
        }
    }
}
