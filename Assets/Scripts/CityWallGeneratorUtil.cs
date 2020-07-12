using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityWallGeneratorUtil {

    /// <summary>
    /// Node base class
    /// </summary>
    public class Node {
        public Vector3 position;
        public int vertexIndex = -1;
        public Node(Vector3 _pos) {
            position = _pos;
        }
    }
    /// <summary>
    /// Sample Node that lay on an actual pixel
    /// </summary>
    public class SampleNode : Node {

        public bool active;
        public Node above, right;

        public SampleNode(Vector3 _pos, bool _active, float squareSize) : base(_pos) {
            active = _active;
            above = new Node(position + Vector3.forward * squareSize / 2f);
            right = new Node(position + Vector3.right * squareSize / 2f);
        }

    }

    /// <summary>
    /// A marching square containing 4 sample nodes and 4 in-between nodes
    /// </summary>
    public class Square {

        public SampleNode topLeft, topRight, bottomRight, bottomLeft; // sample node
        public Node centreTop, centreRight, centreBottom, centreLeft; // in-between node
        public int config = 0; // marching square config

        public Square(SampleNode _topLeft, SampleNode _topRight, SampleNode _bottomRight, SampleNode _bottomLeft) {

            topLeft = _topLeft;
            topRight = _topRight;
            bottomRight = _bottomRight;
            bottomLeft = _bottomLeft;

            centreTop = topLeft.right;
            centreRight = bottomRight.above;
            centreBottom = bottomLeft.right;
            centreLeft = bottomLeft.above;

            if (topLeft.active)
                config += 8;
            if (topRight.active)
                config += 4;
            if (bottomRight.active)
                config += 2;
            if (bottomLeft.active)
                config += 1;
        }

    }

    /// <summary>
    /// Marching squares
    /// </summary>
    public class SquareGrid {

        public Square[,] squares;

        public SquareGrid(bool[,] map, float squareSize) {
            int nodeCountX = map.GetLength(0);
            int nodeCountY = map.GetLength(1);
            float mapWidth = nodeCountX * squareSize;
            float mapHeight = nodeCountY * squareSize;

            SampleNode[,] controlNodes = new SampleNode[nodeCountX, nodeCountY];

            for (int x = 0; x < nodeCountX; x++) {
                for (int y = 0; y < nodeCountY; y++) {
                    Vector3 pos = new Vector3(-mapWidth / 2 + x * squareSize + squareSize / 2, 0, -mapHeight / 2 + y * squareSize + squareSize / 2);
                    controlNodes[x, y] = new SampleNode(pos, map[x, y], squareSize);
                }
            }

            squares = new Square[nodeCountX - 1, nodeCountY - 1];
            for (int x = 0; x < nodeCountX - 1; x++) {
                for (int y = 0; y < nodeCountY - 1; y++) {
                    squares[x, y] = new Square(controlNodes[x, y + 1], controlNodes[x + 1, y + 1], controlNodes[x + 1, y], controlNodes[x, y]);
                }
            }

        }
    }

    struct Triangle {
        public int vertexIndexA;
        public int vertexIndexB;
        public int vertexIndexC;

        public Triangle(int a, int b, int c) {
            vertexIndexA = a;
            vertexIndexB = b;
            vertexIndexC = c;
        }

        public int this[int i] {
            get {
                if (i == 0) {
                    return vertexIndexA;
                } else if (i == 1) {
                    return vertexIndexB;
                } else if (i == 2) {
                    return vertexIndexC;
                } else {
                    throw new System.Exception("Index out of range.");
                }
            }
        }


        public bool Contains(int vertexIndex) {
            return vertexIndex == vertexIndexA || vertexIndex == vertexIndexB || vertexIndex == vertexIndexC;
        }
    }

}
