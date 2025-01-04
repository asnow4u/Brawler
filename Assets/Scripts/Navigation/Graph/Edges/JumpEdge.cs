
namespace Game.Navigation
{
    public class JumpEdge : Edge
    {
        private float initialVelocity;
        private float jumpXInfluence;
        private float jumpYInfluence;


        //Getters
        public float InitialVelocity => initialVelocity;
        public float JumpXInfluence => jumpXInfluence;
        public float JumpYInfluence => jumpYInfluence;



        public JumpEdge(GraphNode startNode, GraphNode endNode, float initialVelocity, float jumpXInfluence, float jumpYInfluence) : base(startNode, endNode)
        {
            edgeType = EdgeType.Jump;
            this.initialVelocity = initialVelocity;
            this.jumpXInfluence = jumpXInfluence;
            this.jumpYInfluence = jumpYInfluence;
        }
    }
}
