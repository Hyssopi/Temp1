using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class MapExtractor : MonoBehaviour
{
    public const int TILE_WIDTH_PIXEL = 16;
    public const int TILE_HEIGHT_PIXEL = 16;

    public const string RESOURCE_DIRECTORY_PATH = "C:/Users/Public/Documents/Unity Projects/Map Extractor/Assets/Resources";
    public const string MAP_INPUT_DIRECTORY = "Images";
    public const string OUTPUT_DIRECTORY_PATH = "C:/Users/t/Desktop/Map Tile Extractor/tiles";

    public enum Direction
    {
        UP,
        RIGHT,
        DOWN,
        LEFT
    };

    private Dictionary<string, Dictionary<Direction, List<string>>> allImageHashReference;


    private static MapExtractor instance;

    public static MapExtractor GetInstance()
    {
        return instance;
    }

    public void Awake()
    {
        instance = this;
        allImageHashReference = new Dictionary<string, Dictionary<Direction, List<string>>>();
    }

    public void Start()
    {
        Debug.Log("Map Extractor START");

        Util.ConfigureTextureImporterDirectory("Assets/Resources/Images");

        List<FileInfo> filePaths = Util.GetFileList(RESOURCE_DIRECTORY_PATH + "/" + MAP_INPUT_DIRECTORY);
        for (int i = 0; i < filePaths.Count; i++)
        {
            if (filePaths[i].Extension.Equals(".meta"))
            {
                continue;
            }

            Debug.Log(Path.GetFileNameWithoutExtension(filePaths[i].Name));


            // Duplicate the chapter map textures
            Texture2D mapImage = Util.DuplicateTexture(Resources.Load(MAP_INPUT_DIRECTORY + "/" + Path.GetFileNameWithoutExtension(filePaths[i].Name)) as Texture2D);


            Debug.Log("MapImage Height: " + mapImage.height);
            Debug.Log("MapImage Width: " + mapImage.width);

            UnityEngine.Assertions.Assert.IsTrue(mapImage.width % TILE_WIDTH_PIXEL == 0);
            UnityEngine.Assertions.Assert.IsTrue(mapImage.height % TILE_HEIGHT_PIXEL == 0);

            string[,] imageHashReference = new string[mapImage.height / TILE_HEIGHT_PIXEL, mapImage.width / TILE_WIDTH_PIXEL];

            for (int y = 0; y < mapImage.height / TILE_HEIGHT_PIXEL; y++)
            {
                for (int x = 0; x < mapImage.width / TILE_WIDTH_PIXEL; x++)
                {
                    //Debug.Log("(" + x + ", " + y + ")");
                    Texture2D subImage = Util.GetSubTexture(mapImage, x * TILE_WIDTH_PIXEL, y * TILE_HEIGHT_PIXEL, TILE_WIDTH_PIXEL, TILE_HEIGHT_PIXEL);

                    // TODO: Check if tiles folder exists
                    //Util.SaveTextureAsPNG(subImage, OUTPUT_DIRECTORY_PATH + "/" + i + "_" + x + "_" + y + ".png");
                    string imageHash = GetTextureHash(subImage);
                    Util.SaveTextureAsPNG(subImage, OUTPUT_DIRECTORY_PATH + "/" + imageHash + ".png");

                    imageHashReference[y, x] = imageHash;
                }
            }

            for (int y = 0; y < imageHashReference.GetLength(0); y++)
            {
                for (int x = 0; x < imageHashReference.GetLength(1); x++)
                {
                    Debug.Log("(" + x + ", " + y + ") = " + imageHashReference[y, x]);

                    if (!allImageHashReference.ContainsKey(imageHashReference[y, x]))
                    {
                        allImageHashReference[imageHashReference[y, x]] = new Dictionary<Direction, List<string>>();
                    }

                    Dictionary<Direction, List<string>> currentTileImageHashReference = allImageHashReference[imageHashReference[y, x]];

                    // TODO: Check if neighbor tiles already exists in list

                    // South node
                    if (y + 1 < imageHashReference.GetLength(0))
                    {
                        if (!currentTileImageHashReference.ContainsKey(Direction.DOWN))
                        {
                            currentTileImageHashReference[Direction.DOWN] = new List<string>();
                        }

                        string southTileHash = imageHashReference[y + 1, x];
                        currentTileImageHashReference[Direction.DOWN].Add(southTileHash);
                    }
                    // East node
                    if (x + 1 < imageHashReference.GetLength(1))
                    {
                        if (!currentTileImageHashReference.ContainsKey(Direction.RIGHT))
                        {
                            currentTileImageHashReference[Direction.RIGHT] = new List<string>();
                        }

                        string eastTileHash = imageHashReference[y, x + 1];
                        currentTileImageHashReference[Direction.RIGHT].Add(eastTileHash);
                    }
                    // North node
                    if (y - 1 >= 0)
                    {
                        if (!currentTileImageHashReference.ContainsKey(Direction.UP))
                        {
                            currentTileImageHashReference[Direction.UP] = new List<string>();
                        }

                        string northTileHash = imageHashReference[y - 1, x];
                        currentTileImageHashReference[Direction.UP].Add(northTileHash);
                    }
                    // West node
                    if (x - 1 >= 0)
                    {
                        if (!currentTileImageHashReference.ContainsKey(Direction.LEFT))
                        {
                            currentTileImageHashReference[Direction.LEFT] = new List<string>();
                        }

                        string westTileHash = imageHashReference[y, x - 1];
                        currentTileImageHashReference[Direction.LEFT].Add(westTileHash);
                    }
                }
            }
        }

        foreach (KeyValuePair<string, Dictionary<Direction, List<string>>> pair in allImageHashReference)
        {
            Debug.Log("Key: " + pair.Key + ", Value: " + pair.Value);
        }

        /*
        bool isInitialSetup = true;
        if (isInitialSetup)
        {
            // Initial loading of tile textures from chapter maps
            // Get directory of chapter maps
            // TODO: Temporarily commented out
            //Util.ConfigureTextureImporterDirectory("Assets/Resources/Images");

            // Duplicate the chapter map textures
            Texture2D mapImage = Util.DuplicateTexture(Resources.Load("Images/" + "Testing/Chapter1") as Texture2D);

            // Extract the chapter map into unique tile textures
            List<TileNodeData> uniqueTileDataList = MapEditorUtil.ExtractUniqueTilesFromTemplateMap(Properties.TILE_HEIGHT, Properties.TILE_WIDTH, mapImage);

            // Repeat with all chapter maps in the directory of chapter maps
            // Merge all the unique tile textures into one list of TileNodeData
            // Convert the list of TileNodeData into a Dictionary with hashes
            // Save each unique TileNodeData.Texture into an image file

            //tileTexturesMainDirectoryPath = "C:/Users/Public/Documents/Unity Projects/MapEditor/Assets/Resources";

            MapEditorUtil.SaveTileTexturesToFiles(uniqueTileDataList, tileTexturesMainDirectoryPath);
            // Save the list of unique TileNodeData into a file
            //MapEditorUtil.SaveTileNodeDataListToFile(tileNodeDataListFilePath, uniqueTileNodeDataList);
            MapEditorUtil.SaveTileNodeDataEdgesToFile(uniqueTileDataList, tileNodeDataListFilePath);
        }
        */
        /*
        Texture2D testTileImage1 = Util.DuplicateTexture(Resources.Load("Images/Testing/Tile4 9") as Texture2D);
        Texture2D testTileImage2 = Util.DuplicateTexture(Resources.Load("Images/Testing/Tile5 12") as Texture2D);
        Debug.Log("TEST IsEqualTexture=" + Util.IsEqualTexture(testTileImage1, testTileImage2));
        */


    }

    public void Update()
    {
    }


    /*
    public static List<TileNodeData> ExtractUniqueTilesFromTemplateMap(int tileHeight, int tileWidth, Texture2D mapImage)
    {
        Debug.Log("MapImage Height: " + mapImage.height);
        Debug.Log("MapImage Width: " + mapImage.width);

        UnityEngine.Assertions.Assert.IsTrue(mapImage.height % tileHeight == 0);
        UnityEngine.Assertions.Assert.IsTrue(mapImage.width % tileWidth == 0);

        List<List<TileNodeData>> mapTemplateData = new List<List<TileNodeData>>();
        List<TileNodeData> uniqueTileDataList = new List<TileNodeData>();

        // Pixels are left to right, bottom to top
        // Loop through the tiles in the map template. Save to a 2D array for each tile, and also to a separate list of unique tiles.
        for (int tileY = 0; tileY < mapImage.height/tileHeight; tileY++)
        {
            List<TileNodeData> mapTemplateDataRow = new List<TileNodeData>();
            for (int tileX = 0; tileX < mapImage.width/tileWidth; tileX++)
            {
                Debug.Log("tileY: " + tileY + ", tileX: " + tileX);

                Texture2D currentTileTexture = new Texture2D(tileWidth, tileHeight);
                Color[] currentTileColors = mapImage.GetPixels(tileX * tileWidth, tileY * tileHeight, tileWidth, tileHeight);
                currentTileTexture.SetPixels(currentTileColors);
                currentTileTexture.Apply();

                // If current tile is already in the unique list, then use the unique list's reference into the 2D array.
                TileNodeData equalTileData = null;
                foreach (TileNodeData uniqueTileData in uniqueTileDataList)
                {
                    if (Util.IsEqualTexture(uniqueTileData.Texture, currentTileTexture))
                    {
                        equalTileData = uniqueTileData;
                        break;
                    }
                }

                if (equalTileData != null)
                {
                    mapTemplateDataRow.Add(equalTileData);
                }
                else
                {
                    TileNodeData currentTileData = new TileNodeData(currentTileTexture, Properties.DEFAULT_TILE_TYPE);
                    mapTemplateDataRow.Add(currentTileData);
                    uniqueTileDataList.Add(currentTileData);
                }
            }
            mapTemplateData.Add(mapTemplateDataRow);
        }

        // Set neighbors
        for (int tileY = 0; tileY < mapTemplateData.Count; tileY++)
        {
            for (int tileX = 0; tileX < mapTemplateData[tileY].Count; tileX++)
            {
                //Dictionary<Direction, List<TileNodeData>> neighbors = new Dictionary<Direction, List<TileNodeData>>();
                // North node
                if (tileY + 1 < mapTemplateData.Count)
                {
                    TileNodeData northTileData = mapTemplateData[tileY + 1][tileX];

                    // Checking to see if tile already exists in the neighbor list
                    bool isNeighborTileExist = false;
                    List<TileNodeData> northNeighborsList = mapTemplateData[tileY][tileX].Neighbors[Direction.NORTH];
                    for (int i = 0; i < northNeighborsList.Count; i++)
                    {
                        if (Util.IsEqualTexture(northNeighborsList[i].Texture, northTileData.Texture))
                        {
                            isNeighborTileExist = true;
                            break;
                        }
                    }
                    if (!isNeighborTileExist)
                    {
                        northNeighborsList.Add(northTileData);
                    }
                }
                // East node
                if (tileX + 1 < mapTemplateData[tileY].Count)
                {
                    TileNodeData eastTileData = mapTemplateData[tileY][tileX + 1];

                    // Checking to see if tile already exists in the neighbor list
                    bool isNeighborTileExist = false;
                    List<TileNodeData> eastNeighborsList = mapTemplateData[tileY][tileX].Neighbors[Direction.EAST];
                    for (int i = 0; i < eastNeighborsList.Count; i++)
                    {
                        if (Util.IsEqualTexture(eastNeighborsList[i].Texture, eastTileData.Texture))
                        {
                            isNeighborTileExist = true;
                            break;
                        }
                    }
                    if (!isNeighborTileExist)
                    {
                        eastNeighborsList.Add(eastTileData);
                    }
                }
                // South node
                if (tileY - 1 >= 0)
                {
                    TileNodeData southTileData = mapTemplateData[tileY - 1][tileX];

                    // Checking to see if tile already exists in the neighbor list
                    bool isNeighborTileExist = false;
                    List<TileNodeData> southNeighborsList = mapTemplateData[tileY][tileX].Neighbors[Direction.SOUTH];
                    for (int i = 0; i < southNeighborsList.Count; i++)
                    {
                        if (Util.IsEqualTexture(southNeighborsList[i].Texture, southTileData.Texture))
                        {
                            isNeighborTileExist = true;
                            break;
                        }
                    }
                    if (!isNeighborTileExist)
                    {
                        southNeighborsList.Add(southTileData);
                    }
                }
                // West node
                if (tileX - 1 >= 0)
                {
                    TileNodeData westTileData = mapTemplateData[tileY][tileX - 1];

                    // Checking to see if tile already exists in the neighbor list
                    bool isNeighborTileExist = false;
                    List<TileNodeData> westNeighborsList = mapTemplateData[tileY][tileX].Neighbors[Direction.WEST];
                    for (int i = 0; i < westNeighborsList.Count; i++)
                    {
                        if (Util.IsEqualTexture(westNeighborsList[i].Texture, westTileData.Texture))
                        {
                            isNeighborTileExist = true;
                            break;
                        }
                    }
                    if (!isNeighborTileExist)
                    {
                        westNeighborsList.Add(westTileData);
                    }
                }
            }
        }
        return uniqueTileDataList;
    }
    */

    /*
    public static List<TileNodeData> MergeTileData(List<TileNodeData> tileData1, List<TileNodeData> tileData2)
    {

    }

    public static Dictionary<string, TileNodeData> HashTileDataList(List<TileNodeData> tileData)
    {
    }
    */

    /*
    public static void SaveTileTexturesToFiles(List<TileNodeData> tileDataList, string tileTexturesMainDirectoryPath)
    {
        // TODO: If folder doesn't exist, then have to make
        foreach (TileNodeData tileData in tileDataList)
        {
            string tileTypeFolder = tileData.TileType;
            Util.SaveTextureAsPNG(tileData.Texture, tileTexturesMainDirectoryPath + "/" + tileTypeFolder + "/" + MapEditorUtil.GetTextureHash(tileData.Texture) + ".png");
        }
    }

    public static void SaveTileNodeDataEdgesToFile(List<TileNodeData> tileDataList, string filePath)
    {
        File.WriteAllText(filePath, "");
        foreach (TileNodeData tileData in tileDataList)
        {
            //string appendText = GraphDataType.NODE.ToString() + MapEditor.COLUMN_DELIMITER + tileData.ExpectedFilePath + MapEditor.COLUMN_DELIMITER + tileData.TileType + Environment.NewLine;

            for (int i = 0; i < Enum.GetNames(typeof(Direction)).Length; i++)
            {
                List<TileNodeData> neighborsList = tileData.Neighbors[(Direction)i];
                for (int j = 0; j < neighborsList.Count; j++)
                {
                    const char COLUMN_DELIMITER = '~';



                    // TODO: If folder doesn't exist, then have to make

                    string appendText = MapEditorUtil.GetTextureHash(tileData.Texture) + COLUMN_DELIMITER + MapEditorUtil.GetTextureHash(neighborsList[j].Texture) + Environment.NewLine;
                    File.AppendAllText(filePath, appendText);
                }
            }



            //string appendText = MapEditorUtil.GetTextureHash(tileData.Texture) + "\n";
            //File.AppendAllText(filePath, appendText);
        }
    }
    */





















    /*
    public static List<TileNodeData> LoadTileDataListFromFile(string tileDataListFilePath)
    {
        List<TileNodeData> tileDataList = new List<TileNodeData>();
        //foreach (string tileDataText in File.ReadAllText(tileDataListFilePath))
        foreach (string tileDataText in Util.ReadTextFile(tileDataListFilePath))
        {
            string[] tileDataTextSplit = tileDataText.Split(MapEditor.COLUMN_DELIMITER);
            string graphDataType = tileDataTextSplit[(int)TileNodeData.Column.GRAPH_DATA_TYPE];
            if (graphDataType.Equals(GraphDataType.NODE.ToString()))
            {
                string expectedFilePath = tileDataTextSplit[(int)TileNodeData.Column.EXPECTED_FILE_PATH];
                Debug.Log("expectedFilePath=" + expectedFilePath);
                Texture2D tileTexture = Util.DuplicateTexture(Resources.Load(expectedFilePath) as Texture2D);
                string tileType = tileDataTextSplit[(int)TileNodeData.Column.TILE_TYPE];
                tileDataList.Add(new TileNodeData(tileTexture, expectedFilePath, tileType));
            }
        }
        return tileDataList;
    }
    */

    public static string GetTextureHash(Texture2D texture)
    {
        byte[] data = texture.GetRawTextureData();
        byte[] result = new MD5CryptoServiceProvider().ComputeHash(data);
        //byte[] result = new SHA512Managed().ComputeHash(data);
        StringBuilder hexadecimalHashResult = new StringBuilder();
        // Loop through each byte of the hashed data and format each one as a hexadecimal string.
        for (int i = 0; i < result.Length; i++)
        {
            hexadecimalHashResult.Append(result[i].ToString("x2"));
        }
        return hexadecimalHashResult.ToString();
    }
}
