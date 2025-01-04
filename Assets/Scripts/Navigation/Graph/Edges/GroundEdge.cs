
namespace Game.Navigation
{
    public class GroundEdge : Edge
    {
        public GroundEdge(GraphNode startNode, GraphNode endNode) : base(startNode, endNode)
        {
            edgeType = EdgeType.Ground;
        }
    }
}
