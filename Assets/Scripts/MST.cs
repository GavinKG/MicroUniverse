using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    public class SimpleGraphNode : IGraphNode {
        public string name;
        public List<SimpleGraphNode> connected;
        public Vector2Int Center { get; set; }
        public void RegisterConnected(IGraphNode other) {
            connected.Add(other as SimpleGraphNode);
        }
        public SimpleGraphNode(Vector2Int center) {
            Center = center;
            connected = new List<SimpleGraphNode>();
        }
    }

    public class Edge : IComparable<Edge> {

        public int from, to;
        public float weight;

        public Edge(int _from, int _to, float _weight) {
            from = _from;
            to = _to;
            weight = _weight;
        }

        public Edge() { }

        public int CompareTo(Edge other) {
            return Mathf.RoundToInt(weight - other.weight);

        }

        public override string ToString() {
            return "Edge: " + from.ToString() + " -> " + to.ToString() + " with weight " + weight.ToString();
        }
    }

    class Graph {

        List<IGraphNode> nodes;
        List<Edge> edges;

        // A class to represent a subset for union-find  
        public class Subset {
            public int parent, rank;
        };

        public Graph(List<IGraphNode> _nodes) {
            nodes = _nodes;
            GenEdges();
        }

        void GenEdges() {
            edges = new List<Edge>();
            for (int i = 0; i < nodes.Count - 1; ++i) {
                for (int j = i + 1; j < nodes.Count; ++j) {
                    edges.Add(new Edge(i, j, Vector2Int.Distance(nodes[i].Center, nodes[j].Center)));
                }
            }
        }

        // A utility function to find set of an element i  
        // (uses path compression technique)  
        int Find(Subset[] subsets, int i) {
            // find root and make root as  
            // parent of i (path compression)  
            if (subsets[i].parent != i)
                subsets[i].parent = Find(subsets,
                                         subsets[i].parent);

            return subsets[i].parent;
        }

        void Union(Subset[] subsets, int x, int y) {
            int xroot = Find(subsets, x);
            int yroot = Find(subsets, y);

            // Attach smaller rank tree under root of 
            // high rank tree (Union by Rank)  
            if (subsets[xroot].rank < subsets[yroot].rank)
                subsets[xroot].parent = yroot;
            else if (subsets[xroot].rank > subsets[yroot].rank)
                subsets[yroot].parent = xroot;

            // If ranks are same, then make one as root  
            // and increment its rank by one  
            else {
                subsets[yroot].parent = xroot;
                subsets[xroot].rank++;
            }
        }

        public List<Edge> KruskalMST() {

            int V = nodes.Count;

            List<Edge> result = new List<Edge>(V-1); // This will store the resultant MST  
            int e = 0; // An index variable, used for result[]  
            int i = 0; // An index variable, used for sorted edges  
            for (i = 0; i < V-1; ++i) {
                result.Add(new Edge());
            }

            // Step 1: Sort all the edges in non-decreasing  
            // order of their weight. If we are not allowed  
            // to change the given graph, we can create 
            // a copy of array of edges  
            edges.Sort();

            // Allocate memory for creating V subsets  
            Subset[] subsets = new Subset[V];
            for (i = 0; i < V; ++i)
                subsets[i] = new Subset();

            // Create V subsets with single elements  
            for (int v = 0; v < V; ++v) {
                subsets[v].parent = v;
                subsets[v].rank = 0;
            }

            i = 0; // Index used to pick next edge  

            // Number of edges to be taken is equal to V-1  
            while (e < V - 1) {
                // Step 2: Pick the smallest edge. And increment  
                // the index for next iteration  
                Edge nextEdge = new Edge();
                nextEdge = edges[i++];

                int x = Find(subsets, nextEdge.from);
                int y = Find(subsets, nextEdge.to);

                // If including this edge does't cause cycle,  
                // include it in result and increment the index  
                // of result for next edge  
                if (x != y) {
                    result[e++] = nextEdge;
                    Union(subsets, x, y);
                }
                // Else discard the next_edge  
            }

            return result;
        }
    }

    /// <summary>
    /// Minimum Spanning Tree implementation using Kruskal's Algorithm with Union Find.
    /// Instead of passing in edges, all points in the graph are passed in. Edges are formed between all possible point pair combination with weight equals to their euler distance.
    /// </summary>
    public static class MST {

        /// <summary>
        /// Run the actual algorithm, return the tree root of MST.
        /// </summary>
        public static IGraphNode Run(List<IGraphNode> nodes, bool registerBidirectional = true) {

            Graph graph = new Graph(nodes);
            List<Edge> connected = graph.KruskalMST();

            // register connections
            foreach (Edge edge in connected) {
                IGraphNode from = nodes[edge.from];
                IGraphNode to = nodes[edge.to];
                from.RegisterConnected(to);
                if (registerBidirectional) {
                    to.RegisterConnected(from);
                }
            }

            // find tree root
            int treeRootIndex = -1;
            bool[] hasToNode = new bool[nodes.Count];
            foreach (Edge edge in connected) {
                hasToNode[edge.to] = true;
            }
            for (int i = 0; i < nodes.Count; ++i) {
                if (!hasToNode[i]) {
                    treeRootIndex = i;
                    break;
                }
            }

            Debug.Log("MST Finished.");

            return nodes[treeRootIndex];

        }

    }

}