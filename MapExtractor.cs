using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class MapExtractor : MonoBehaviour
{
    public class TileNeighborData
    {
        public List<string> north;
        public List<string> east;
        public List<string> south;
        public List<string> west;
        
        public TileNeighborData(HashSet<string> northTileNeighbors, HashSet<string> eastTileNeighbors, HashSet<string> southTileNeighbors, HashSet<string> westTileNeighbors)
        {
            this.north = northTileNeighbors.ToList<string>();
            this.east = eastTileNeighbors.ToList<string>();
            this.south = southTileNeighbors.ToList<string>();
            this.west = westTileNeighbors.ToList<string>();
        }
    }
    public const int TILE_WIDTH_PIXEL = 16;
    public const int TILE_HEIGHT_PIXEL = 16;

    public const string KEY_DELIMITER = "-";
    public const string VALUE_DELIMITER = "~";
    public const string KEY_VALUE_DELIMITER = "=";

    public const string RESOURCE_DIRECTORY_PATH = "C:/Users/Public/Documents/Unity Projects/Map Extractor/Assets/Resources";
    public const string MAP_INPUT_DIRECTORY = "Images";
    public const string BASE_OUTPUT_DIRECTORY_PATH = "C:/Users/t/Desktop/Map Tile Extractor/tiles";
    public const string IMAGES_OUTPUT_FOLDER_NAME = "images";
    public const string UNDEFINED_OUTPUT_FOLDER_NAME = "UNDEFINED";
    public const string TILE_REFERENCES_JSON_FILE_NAME = "tileReferences.json";

    public enum Direction
    {
        NORTH,
        EAST,
        SOUTH,
        WEST
    };

    private SortedDictionary<string, HashSet<string>> tileReferences;


    private static MapExtractor instance;

    public static MapExtractor GetInstance()
    {
        return instance;
    }

    public void Awake()
    {
        instance = this;
        tileReferences = new SortedDictionary<string, HashSet<string>>();
    }

    public void Start()
    {
        Debug.Log("Map Extractor START");


        /*
        TileNeighborData tileNeighborData = new TileNeighborData();
        tileNeighborData.north = new List<string>();
        tileNeighborData.east = new List<string>();
        tileNeighborData.south = new List<string>();
        tileNeighborData.west = new List<string>();

        tileNeighborData.north.Add("nsdgfaba2");
        tileNeighborData.north.Add("n12sdgsaba2");
        tileNeighborData.north.Add("n1sdgba2");
        tileNeighborData.east.Add("e12sdga2");
        tileNeighborData.east.Add("esdg4a2");
        tileNeighborData.south.Add("s3463aj");
        tileNeighborData.south.Add("suyluylbad");

        string outputJson = JsonUtility.ToJson(tileNeighborData, true);
        Debug.Log(outputJson);
        
        return;
        */








        Util.ConfigureTextureImporterDirectory("Assets/Resources/Images");
        
        // Get list of existing unique tile image hashes by reading from the tiles/images folder
        HashSet<string> allUniqueTileHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        List<FileInfo> existingTileImageFilePaths = Util.GetFileList(BASE_OUTPUT_DIRECTORY_PATH + "/" + IMAGES_OUTPUT_FOLDER_NAME);
        foreach (FileInfo filePath in existingTileImageFilePaths)
        {
            if (!filePath.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            //Debug.Log(Path.GetFileNameWithoutExtension(filePath.Name));

            allUniqueTileHashes.Add(Path.GetFileNameWithoutExtension(filePath.Name));
        }

        // Read the input map images and extract the tile images and neighbor data
        List<FileInfo> filePaths = Util.GetFileList(RESOURCE_DIRECTORY_PATH + "/" + MAP_INPUT_DIRECTORY);
        foreach (FileInfo filePath in filePaths)
        {
            if (!filePath.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Debug.Log(Path.GetFileNameWithoutExtension(filePath.Name));

            // Load the chapter map image
            Texture2D mapImage = Util.DuplicateTexture(Resources.Load(MAP_INPUT_DIRECTORY + "/" + Path.GetFileNameWithoutExtension(filePath.Name)) as Texture2D);

            Debug.Log("MapImage Height: " + mapImage.height);
            Debug.Log("MapImage Width: " + mapImage.width);

            UnityEngine.Assertions.Assert.IsTrue(mapImage.width % TILE_WIDTH_PIXEL == 0);
            UnityEngine.Assertions.Assert.IsTrue(mapImage.height % TILE_HEIGHT_PIXEL == 0);
            
            string[,] mapTileImageHashes = new string[mapImage.height / TILE_HEIGHT_PIXEL, mapImage.width / TILE_WIDTH_PIXEL];

            for (int y = 0; y < mapImage.height / TILE_HEIGHT_PIXEL; y++)
            {
                for (int x = 0; x < mapImage.width / TILE_WIDTH_PIXEL; x++)
                {
                    //Debug.Log("(" + x + ", " + y + ")");
                    Texture2D subImage = Util.GetSubTexture(mapImage, x * TILE_WIDTH_PIXEL, y * TILE_HEIGHT_PIXEL, TILE_WIDTH_PIXEL, TILE_HEIGHT_PIXEL);

                    string tileImageHash = GetTextureHash(subImage);

                    mapTileImageHashes[y, x] = tileImageHash;

                    if (!allUniqueTileHashes.Contains(tileImageHash))
                    {
                        Util.SaveTextureAsPNG(subImage, BASE_OUTPUT_DIRECTORY_PATH + "/" + IMAGES_OUTPUT_FOLDER_NAME + "/" + UNDEFINED_OUTPUT_FOLDER_NAME + "/" + tileImageHash + ".png");
                        allUniqueTileHashes.Add(tileImageHash);
                    }
                }
            }

            for (int y = 0; y < mapTileImageHashes.GetLength(0); y++)
            {
                for (int x = 0; x < mapTileImageHashes.GetLength(1); x++)
                {
                    //Debug.Log("(" + x + ", " + y + ") = " + mapTileImageHashes[y, x]);

                    /*
                    if (!tileReferences.ContainsKey(mapTileImageHashes[y, x]))
                    {
                        tileReferences[mapTileImageHashes[y, x]] = new Dictionary<Direction, List<string>>();
                    }
                    Dictionary<Direction, List<string>> currentTilemapTileImageHashes = tileReferences[mapTileImageHashes[y, x]];
                    */

                    // South node
                    if (y + 1 < mapTileImageHashes.GetLength(0))
                    {
                        string key = GetKey(mapTileImageHashes[y, x], Direction.SOUTH);
                        
                        if (!tileReferences.ContainsKey(key))
                        {
                            tileReferences[key] = new HashSet<string>();
                        }

                        tileReferences[key].Add(mapTileImageHashes[y + 1, x]);
                    }
                    // East node
                    if (x + 1 < mapTileImageHashes.GetLength(1))
                    {
                        string key = GetKey(mapTileImageHashes[y, x], Direction.EAST);
                        
                        if (!tileReferences.ContainsKey(key))
                        {
                            tileReferences[key] = new HashSet<string>();
                        }

                        tileReferences[key].Add(mapTileImageHashes[y, x + 1]);
                    }
                    // North node
                    if (y - 1 >= 0)
                    {
                        string key = GetKey(mapTileImageHashes[y, x], Direction.NORTH);
                        
                        if (!tileReferences.ContainsKey(key))
                        {
                            tileReferences[key] = new HashSet<string>();
                        }

                        tileReferences[key].Add(mapTileImageHashes[y - 1, x]);
                    }
                    // West node
                    if (x - 1 >= 0)
                    {
                        string key = GetKey(mapTileImageHashes[y, x], Direction.WEST);
                        
                        if (!tileReferences.ContainsKey(key))
                        {
                            tileReferences[key] = new HashSet<string>();
                        }

                        tileReferences[key].Add(mapTileImageHashes[y, x - 1]);
                    }
                }
            }
        }

        
        // Save neighbor data as JSON file
        /*
        StringBuilder outputLines = new StringBuilder();
        foreach (KeyValuePair<string, HashSet<string>> pair in tileReferences)
        {
            //Debug.Log("Key: " + pair.Key + ", Value: " + pair.Value);
            outputLines.Append(pair.Key + KEY_VALUE_DELIMITER);
            foreach (string neighborImageHash in pair.Value)
            {
                outputLines.Append(neighborImageHash + VALUE_DELIMITER);
            }
            outputLines.AppendLine("");
        }
        */
        

        
        
        StringBuilder outputLines = new StringBuilder();

        outputLines.AppendLine("{");

        foreach (string uniqueTileHash in allUniqueTileHashes)
        {
            outputLines.AppendLine("  " + "\"" + uniqueTileHash + "\":");
            
            HashSet<string> northTileNeighbors = new HashSet<string>();
            HashSet<string> eastTileNeighbors = new HashSet<string>();
            HashSet<string> southTileNeighbors = new HashSet<string>();
            HashSet<string> westTileNeighbors = new HashSet<string>();

            //tileReferences.TryGetValue(GetKey(uniqueTileHash, Direction.NORTH), out northTileNeighbors);

            if (tileReferences.ContainsKey(GetKey(uniqueTileHash, Direction.NORTH)))
            {
                northTileNeighbors = tileReferences[GetKey(uniqueTileHash, Direction.NORTH)];
            }
            if (tileReferences.ContainsKey(GetKey(uniqueTileHash, Direction.EAST)))
            {
                eastTileNeighbors = tileReferences[GetKey(uniqueTileHash, Direction.EAST)];
            }
            if (tileReferences.ContainsKey(GetKey(uniqueTileHash, Direction.SOUTH)))
            {
                southTileNeighbors = tileReferences[GetKey(uniqueTileHash, Direction.SOUTH)];
            }
            if (tileReferences.ContainsKey(GetKey(uniqueTileHash, Direction.WEST)))
            {
                westTileNeighbors = tileReferences[GetKey(uniqueTileHash, Direction.WEST)];
            }

            /*
            HashSet<string> northTileNeighbors = tileReferences[GetKey(uniqueTileHash, Direction.NORTH)];
            HashSet<string> eastTileNeighbors = tileReferences[GetKey(uniqueTileHash, Direction.EAST)];
            HashSet<string> southTileNeighbors = tileReferences[GetKey(uniqueTileHash, Direction.SOUTH)];
            HashSet<string> westTileNeighbors = tileReferences[GetKey(uniqueTileHash, Direction.WEST)];
            */
            TileNeighborData tileNeighborData = new TileNeighborData(northTileNeighbors, eastTileNeighbors, southTileNeighbors, westTileNeighbors);
            string outputJson = JsonUtility.ToJson(tileNeighborData, false);
            outputLines.Append(outputJson);
            
            outputLines.AppendLine(",");
        }

        // TODO: Remove to remove the last ","

        outputLines.AppendLine("}");

        Util.WriteTextFile(BASE_OUTPUT_DIRECTORY_PATH + "/" + TILE_REFERENCES_JSON_FILE_NAME, outputLines.ToString());

        

        



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
    }
    
    public static string GetKey(string tileImageHash, Direction direction)
    {
        return tileImageHash + KEY_DELIMITER + direction.ToString();
    }

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






    /*
    public void Update()
    {
    }
    */


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
    
}
