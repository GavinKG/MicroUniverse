using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    public class TestMST : MonoBehaviour {
        public List<Transform> pointTransforms;

        public void DoMST() {

            List<IGraphNode> nodes = new List<IGraphNode>(pointTransforms.Count);

            foreach (Transform t in pointTransforms) {
                SimpleGraphNode node = new SimpleGraphNode(new Vector2Int(Mathf.RoundToInt(t.position.x), Mathf.RoundToInt(t.position.z)));
                node.name = t.name;
                nodes.Add(node as IGraphNode);
            }

            SimpleGraphNode rootNode = MST.Run(nodes, registerBidirectional: false) as SimpleGraphNode;

            print("Done Kruskal.");

        }
    
    }
}