using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCityWallGen : MonoBehaviour {

    public Texture2D cityTex;

    public GameObject coverGO;
    public GameObject wallGO;

    public float squareSize = 0.2f;
    public float wallHeight = 2f;

    public void Generate() {

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        bool[,] map = Texture2BoolMap.Convert(cityTex);

        CityWallGenerator cityWallGenerator = new CityWallGenerator();
        cityWallGenerator.GenerateMesh(map, squareSize, wallHeight);

        coverGO.transform.position = Vector3.zero;
        coverGO.transform.rotation = Quaternion.identity;
        wallGO.transform.position = Vector3.zero;
        wallGO.transform.rotation = Quaternion.identity;

        coverGO.GetComponent<MeshFilter>().mesh = cityWallGenerator.CoverMesh;
        wallGO.GetComponent<MeshFilter>().mesh = cityWallGenerator.WallMesh;

    }

}
