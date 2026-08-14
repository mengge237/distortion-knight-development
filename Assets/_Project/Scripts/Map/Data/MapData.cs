using System.Collections.Generic;

namespace MutationChess.Map
{
    public class MapData
    {
        public List<List<MapNode>> layers = new List<List<MapNode>>();
        public MapNode bossNode;
        public List<MapNode> startNodes = new List<MapNode>();

        public MapData()
        {
            layers = new List<List<MapNode>>();
            startNodes = new List<MapNode>();
        }

        public List<MapNode> GetLayer(int index)
        {
            if (index >= 0 && index < layers.Count)
                return layers[index];
            return null;
        }

        public IEnumerable<MapNode> GetAllNodes()
        {
            foreach (var layer in layers)
                foreach (var node in layer)
                    yield return node;
        }
    }
}