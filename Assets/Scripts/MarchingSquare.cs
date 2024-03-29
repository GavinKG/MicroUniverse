﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    public class MarchingSquare {

        // Cover mesh:
        public Mesh CoverMesh {
            get {
                if (coverMesh == null) {
                    coverMesh = new Mesh();
                    coverMesh.hideFlags = HideFlags.HideAndDontSave;
                    coverMesh.vertices = CoverVertices.ToArray();
                    coverMesh.triangles = CoverIndices.ToArray();
                    coverMesh.RecalculateNormals();
                }
                return coverMesh;
            }
            private set {
                coverMesh = value;
            }
        }
        public List<Vector3> CoverVertices { get; private set; }
        public List<int> CoverIndices { get; private set; }
        private Mesh coverMesh;

        // Wall mesh:
        public Mesh WallMesh {
            get {
                if (wallMesh == null) {
                    WallMesh = new Mesh();
                    WallMesh.hideFlags = HideFlags.HideAndDontSave;
                    WallMesh.vertices = WallVertices.ToArray();
                    WallMesh.triangles = WallIndices.ToArray();
                }
                return wallMesh;
            }
            private set {
                wallMesh = value;
            }
        }
        public List<Vector3> WallVertices { get; private set; }
        public List<int> WallIndices { get; private set; }
        private Mesh wallMesh;

        // Helper:
        HashSet<int> checkedVertices; // indices that are checked in outline finding process.
        List<List<int>> outlineIndices; // outline indices (there might be multiple outlines)
        Dictionary<int, List<Triangle>> triHashMap; // index -> triangles containing this index


        /// <summary>
        /// Main function to convert a map to the "wall" like mesh. Meshes are stored in public property "CoverMesh" and "WallMesh"
        /// You can also get vertex positions and indices by getting from "xxxVertices" and "xxxIndices"
        /// One have to call this function first before call mesh getter.
        /// </summary>
        /// <param name="map">true: occupied, false: empty</param>
        /// <param name="squareSize">Square resolution.</param>
        /// <param name="wallHeight">The height (+Y) of the wall mesh.</param>
        /// <param name="wallFaceOutside">Whether wall's facing is towards empty area.</param>
        /// <param name="smoothRatio">how much should a vertex care about neighbouring vertices, from 0 to 1!</param>
        public void GenerateMesh(bool[,] map, float squareSize, float wallHeight, int smoothCount, float smoothRatio, bool wallFaceOutside = true) {

            Clear();

            // Main grid
            SquareGrid squareGrid = new SquareGrid(map, squareSize);

            // Cover generation
            CoverVertices = new List<Vector3>();
            CoverIndices = new List<int>();
            for (int x = 0; x < squareGrid.squares.GetLength(0); x++) {
                for (int y = 0; y < squareGrid.squares.GetLength(1); y++) {
                    TriangulateSquare(squareGrid.squares[x, y]);
                }
            }

            // Outline generation & smoothen (3-pass)
            CalculateMeshOutlines();
            for (int i = 0; i < smoothCount; ++i) {
                SmoothOutline(smoothRatio);
            }


            // Wall Generation
            WallVertices = new List<Vector3>();
            WallIndices = new List<int>();
            foreach (List<int> outline in outlineIndices) {
                for (int i = 0; i < outline.Count - 1; i++) {
                    int startIndex = WallVertices.Count;
                    WallVertices.Add(CoverVertices[outline[i]]); // left
                    WallVertices.Add(CoverVertices[outline[i + 1]]); // right
                    WallVertices.Add(CoverVertices[outline[i]] - Vector3.up * wallHeight); // bottom left
                    WallVertices.Add(CoverVertices[outline[i + 1]] - Vector3.up * wallHeight); // bottom right

                    if (wallFaceOutside) {
                        WallIndices.Add(startIndex + 0);
                        WallIndices.Add(startIndex + 2);
                        WallIndices.Add(startIndex + 3);

                        WallIndices.Add(startIndex + 3);
                        WallIndices.Add(startIndex + 1);
                        WallIndices.Add(startIndex + 0);

                    } else {

                        WallIndices.Add(startIndex + 0);
                        WallIndices.Add(startIndex + 1);
                        WallIndices.Add(startIndex + 3);

                        WallIndices.Add(startIndex + 3);
                        WallIndices.Add(startIndex + 2);
                        WallIndices.Add(startIndex + 0);
                    }


                }
            }
        }

        /// <summary>
        /// Reset all variables to give it a fresh start.
        /// </summary>
        void Clear() {
            CoverMesh = null;
            CoverIndices = null;
            CoverVertices = null;

            WallMesh = null;
            WallVertices = null;
            CoverIndices = null;

            checkedVertices = null;
            outlineIndices = null;
            triHashMap = null;

            checkedVertices = new HashSet<int>(); // indices that are checked in outline finding process.
            outlineIndices = new List<List<int>>(); // outline indices (there might be multiple outlines)
            triHashMap = new Dictionary<int, List<Triangle>>(); // index -> triangles containing this index
        }



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



        /// <summary>
        /// Interpolate vertices in coverVertices for all outline lists to form a smooth outline.
        /// </summary>
        void SmoothOutline(float ratio) {
            List<Vector3> smoothedCoverVertices = new List<Vector3>(CoverVertices); // "deep copy"
            foreach (List<int> outlineList in outlineIndices) {

                for (int i = 0; i < outlineList.Count; ++i) {
                    Vector3 lhs;
                    if (i == 0) {
                        lhs = CoverVertices[outlineList[outlineList.Count - 1]];
                    } else {
                        lhs = CoverVertices[outlineList[i - 1]];
                    }
                    Vector3 rhs;
                    if (i == outlineList.Count-1) {
                        rhs = CoverVertices[outlineList[0]];
                    } else {
                        rhs = CoverVertices[outlineList[i + 1]];
                    }
                    
                    Vector3 midpoint = (lhs + rhs) / 2;
                    Vector3 curr = CoverVertices[outlineList[i]];
                    curr += (midpoint - curr) * ratio;
                    smoothedCoverVertices[outlineList[i]] = curr;
                }
            }
            CoverVertices = smoothedCoverVertices;
        }


        /// <summary>
        /// For given square, write mesh vertices / indices to class member.
        /// </summary>
        /// <param name="square"></param>
        void TriangulateSquare(Square square) {
            switch (square.config) {
                case 0:
                    break;

                // 1 points:
                case 1:
                    GenAndAddMeshFromPoints(square.centreLeft, square.centreBottom, square.bottomLeft);
                    break;
                case 2:
                    GenAndAddMeshFromPoints(square.bottomRight, square.centreBottom, square.centreRight);
                    break;
                case 4:
                    GenAndAddMeshFromPoints(square.topRight, square.centreRight, square.centreTop);
                    break;
                case 8:
                    GenAndAddMeshFromPoints(square.topLeft, square.centreTop, square.centreLeft);
                    break;

                // 2 points:
                case 3:
                    GenAndAddMeshFromPoints(square.centreRight, square.bottomRight, square.bottomLeft, square.centreLeft);
                    break;
                case 6:
                    GenAndAddMeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.centreBottom);
                    break;
                case 9:
                    GenAndAddMeshFromPoints(square.topLeft, square.centreTop, square.centreBottom, square.bottomLeft);
                    break;
                case 12:
                    GenAndAddMeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreLeft);
                    break;
                case 5:
                    GenAndAddMeshFromPoints(square.centreTop, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft, square.centreLeft);
                    break;
                case 10:
                    GenAndAddMeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.centreBottom, square.centreLeft);
                    break;

                // 3 point:
                case 7:
                    GenAndAddMeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.bottomLeft, square.centreLeft);
                    break;
                case 11:
                    GenAndAddMeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.bottomLeft);
                    break;
                case 13:
                    GenAndAddMeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft);
                    break;
                case 14:
                    GenAndAddMeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centreBottom, square.centreLeft);
                    break;

                // 4 point:
                case 15:
                    GenAndAddMeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);

                    // no need to check these vertices.
                    /*
                    checkedVertices.Add(square.topLeft.vertexIndex);
                    checkedVertices.Add(square.topRight.vertexIndex);
                    checkedVertices.Add(square.bottomRight.vertexIndex);
                    checkedVertices.Add(square.bottomLeft.vertexIndex);
                    */
                    break;
            }

        }

        /// <summary>
        /// Generate outline from cover vertices and store their indices to outlineIndices.
        /// </summary>
        void CalculateMeshOutlines() {

            // check all coverVertices
            for (int vertexIndex = 0; vertexIndex < CoverVertices.Count; vertexIndex++) {

                // skip checked vertices
                if (checkedVertices.Contains(vertexIndex)) {
                    continue;
                }

                int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
                if (newOutlineVertex != -1) {
                    checkedVertices.Add(vertexIndex);

                    // create a new outline list and trace the whole outline.
                    List<int> newOutline = new List<int>();
                    newOutline.Add(vertexIndex);
                    outlineIndices.Add(newOutline);
                    FollowOutline(newOutlineVertex, outlineIndices.Count - 1);
                    outlineIndices[outlineIndices.Count - 1].Add(vertexIndex);
                }

            }
        }

        /*
        void FollowOutline(int vertexIndex, int outlineIndex) {
            outlineIndices[outlineIndex].Add(vertexIndex);
            checkedVertices.Add(vertexIndex);
            int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);

            if (nextVertexIndex != -1) {
                FollowOutline(nextVertexIndex, outlineIndex);
            }
        }
        */

        /// <summary>
        /// Follow outline vertexIndex and register this entire outline loop to outlineIndices[outlineIndex]
        /// </summary>
        void FollowOutline(int vertexIndex, int outlineListIndex) {
            do {
                outlineIndices[outlineListIndex].Add(vertexIndex);
                checkedVertices.Add(vertexIndex);
                vertexIndex = GetConnectedOutlineVertex(vertexIndex); // outline goes on and on...
            } while (vertexIndex != -1); // ...until it reaches the starting point.
        }

        /// <summary>
        /// return another vertex that can form an outline with given vertex.
        /// </summary>
        int GetConnectedOutlineVertex(int vertexIndex) {
            List<Triangle> tris = triHashMap[vertexIndex]; // triangles containing this vertex

            for (int i = 0; i < tris.Count; i++) {

                // for each triangle's vertex
                for (int j = 0; j < 3; j++) {
                    int vertexB = tris[i][j];
                    if (vertexB != vertexIndex && !checkedVertices.Contains(vertexB)) {
                        // not self && not checked
                        if (IsOutlineEdge(vertexIndex, vertexB)) {
                            return vertexB;
                        }
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// if two vertices can form an outline, they must share ONLY ONE triangle.
        /// </summary>
        bool IsOutlineEdge(int vertexA, int vertexB) {
            List<Triangle> trianglesContainingVertexA = triHashMap[vertexA];
            int sharedTriangleCount = 0;

            for (int i = 0; i < trianglesContainingVertexA.Count; i++) {
                if (trianglesContainingVertexA[i].Contains(vertexB)) {
                    sharedTriangleCount++;
                    if (sharedTriangleCount > 1) {
                        break;
                    }
                }
            }
            return sharedTriangleCount == 1;
        }

        /// <summary>
        /// Generate and add mesh from points!
        /// </summary>
        /// <param name="nodes"></param>
        void GenAndAddMeshFromPoints(params Node[] nodes) {

            // assign and register vertex index if new vertex occured.
            for (int i = 0; i < nodes.Length; i++) {
                if (nodes[i].vertexIndex == -1) {
                    nodes[i].vertexIndex = CoverVertices.Count;
                    CoverVertices.Add(nodes[i].position);
                }
            }

            // Assemble triangle and record them.
            if (nodes.Length >= 3)
                CreateTriangle(nodes[0], nodes[1], nodes[2]);
            if (nodes.Length >= 4)
                CreateTriangle(nodes[0], nodes[2], nodes[3]);
            if (nodes.Length >= 5)
                CreateTriangle(nodes[0], nodes[3], nodes[4]);
            if (nodes.Length >= 6)
                CreateTriangle(nodes[0], nodes[4], nodes[5]);

        }


        void CreateTriangle(Node a, Node b, Node c) {
            CoverIndices.Add(a.vertexIndex);
            CoverIndices.Add(b.vertexIndex);
            CoverIndices.Add(c.vertexIndex);

            // register triangle
            Triangle triangle = new Triangle(a.vertexIndex, b.vertexIndex, c.vertexIndex);
            AddTriangleToDictionary(triangle.vertexIndexA, triangle);
            AddTriangleToDictionary(triangle.vertexIndexB, triangle);
            AddTriangleToDictionary(triangle.vertexIndexC, triangle);
        }

        /// <summary>
        /// Add a generated triangle to the index -> triangles HashMap for fast lookup in contour gen.
        /// </summary>
        void AddTriangleToDictionary(int vertexIndex, Triangle triangle) {
            if (triHashMap.ContainsKey(vertexIndex)) {
                triHashMap[vertexIndex].Add(triangle);
            } else {
                List<Triangle> triangleList = new List<Triangle>();
                triangleList.Add(triangle);
                triHashMap.Add(vertexIndex, triangleList);
            }
        }


    }

}

