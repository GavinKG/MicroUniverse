using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CityWallGeneratorUtil;

public class CityWallGenerator {

    // Cover mesh:
    public Mesh CoverMesh { get; private set; }
    List<Vector3> coverVertices;
    List<int> coverIndices;

    // Wall mesh:
    public Mesh WallMesh { get; private set; }
    List<Vector3> wallVertices;
    List<int> wallTriangles;

    // Helper:
    HashSet<int> checkedVertices = new HashSet<int>(); // indices that are checked in outline finding process.
    List<List<int>> outlineIndices = new List<List<int>>(); // outline indices (there might be multiple outlines)
    Dictionary<int, List<Triangle>> triHashMap = new Dictionary<int, List<Triangle>>(); // index -> triangles containing this index


    /// <summary>
    /// Main function to convert a map to the "wall" like mesh.
    /// One have to call this function first before call mesh getter.
    /// </summary>
    /// <param name="map">true: occupied, false: empty</param>
    /// <param name="squareSize">Square resolution.</param>
    /// <param name="wallHeight">The height (+Y) of the wall mesh.</param>
    public void GenerateMesh(bool[,] map, float squareSize, float wallHeight) {

        // Main grid
        SquareGrid squareGrid = new SquareGrid(map, squareSize);

        // Cover mesh generation
        coverVertices = new List<Vector3>();
        coverIndices = new List<int>();
        for (int x = 0; x < squareGrid.squares.GetLength(0); x++) {
            for (int y = 0; y < squareGrid.squares.GetLength(1); y++) {
                TriangulateSquare(squareGrid.squares[x, y]);
            }
        }

        CoverMesh = new Mesh();
        CoverMesh.vertices = coverVertices.ToArray();
        CoverMesh.triangles = coverIndices.ToArray();
        CoverMesh.RecalculateNormals();

        // Wall mesh generation
        CalculateMeshOutlines();
        wallVertices = new List<Vector3>();
        wallTriangles = new List<int>();
        foreach (List<int> outline in outlineIndices) {
            for (int i = 0; i < outline.Count - 1; i++) {
                int startIndex = wallVertices.Count;
                wallVertices.Add(coverVertices[outline[i]]); // left
                wallVertices.Add(coverVertices[outline[i + 1]]); // right
                wallVertices.Add(coverVertices[outline[i]] - Vector3.up * wallHeight); // bottom left
                wallVertices.Add(coverVertices[outline[i + 1]] - Vector3.up * wallHeight); // bottom right

                wallTriangles.Add(startIndex + 0);
                wallTriangles.Add(startIndex + 2);
                wallTriangles.Add(startIndex + 3);

                wallTriangles.Add(startIndex + 3);
                wallTriangles.Add(startIndex + 1);
                wallTriangles.Add(startIndex + 0);
            }
        }

        WallMesh = new Mesh();
        WallMesh.vertices = wallVertices.ToArray();
        WallMesh.triangles = wallTriangles.ToArray();
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
                checkedVertices.Add(square.topLeft.vertexIndex);
                checkedVertices.Add(square.topRight.vertexIndex);
                checkedVertices.Add(square.bottomRight.vertexIndex);
                checkedVertices.Add(square.bottomLeft.vertexIndex);
                break;
        }

    }

    /// <summary>
    /// Generate outline from cover vertices and store their indices to outlineIndices.
    /// </summary>
    void CalculateMeshOutlines() {

        // check all coverVertices
        for (int vertexIndex = 0; vertexIndex < coverVertices.Count; vertexIndex++) {

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
                nodes[i].vertexIndex = coverVertices.Count;
                coverVertices.Add(nodes[i].position);
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
        coverIndices.Add(a.vertexIndex);
        coverIndices.Add(b.vertexIndex);
        coverIndices.Add(c.vertexIndex);

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
