using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerationManager : MonoBehaviour
{
     public enum DrawMode {
        OriginalHeightMap,
        FalloffMap,
        HeightMap,
        TemperatureMap,
        PrecipitationMap,
        BiomeMap
    }
    [SerializeField]ProcGenConfig PCGConfig;
    [SerializeField] Terrain TargetTerrain;
    TexturePainting texturePainter;

    public DrawMode drawMode;
    public float[,] og_heightMap;
    public float[,] fallOffMap;
    public float[,] heightMap;
    public float[,] temperatureMap;
    public float[,] precipitationMap;
    public int[,] biomeMap;
    public bool autoUpdate; 
    public bool RegenerateLayers;

     [Header("Texture + Object that holds the map")]
    // Texture and object that holds the map
    public Renderer textureRenderer;
    public Texture2D temperatureColorImage;



    // private void Start()
    // {
        
    //     PCGConfig.seed = Random.Range(int.MinValue, int.MaxValue);
    //     //on start of the program a random x and y value will be chosen to randomize the terrain if random offset is toggled on
    //     if(PCGConfig.randomOffset)
        // {
        // PCGConfig.offset.x = Random.Range(0f, 9999f);
        // PCGConfig.offset.y = Random.Range(0f, 9999f);
        // }
    //     GenerateWorld();  
    // }

    // private void Update()
    // {
    //     GenerateWorld();
    //     TargetTerrain.terrainData = GenerateTerrain(TargetTerrain.terrainData);
    // }


    public void DrawTexture(float[,] map) {
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        Color[] colorMap = new Color[width * height];

        if (drawMode == DrawMode.TemperatureMap) {
            for (int j = 0; j < height; j++) {
                for (int i = 0; i < width; i++) {
                    int x = (int)(Mathf.Clamp01(map[i, j]) * temperatureColorImage.width);
                    int y = temperatureColorImage.height / 2;
                    colorMap[j * width + i] = temperatureColorImage.GetPixel(x, y);
                }
            }
        }
        else {
            for (int j = 0; j < height; j++) {
                for (int i = 0; i < width; i++) {
                    float value = (drawMode == DrawMode.PrecipitationMap) ? map[i, j] / 100f : map[i, j];
                    colorMap[j * width + i] = (map[i, j] > PCGConfig.seaLevel) ? Color.Lerp(Color.black, Color.white, value) : Color.black;
                }
            }
        }

        texture.SetPixels(colorMap);
        texture.Apply();
        //texture.filterMode = FilterMode.Point;

        textureRenderer.material.mainTexture = texture;
        //textureRenderer.transform.localScale = new Vector3(width, 0, height);  //this line changes the plane's size to the size of the grid  maps
    }

    public void DrawBiomeTexture(int[,] map) {
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        Color[] colorMap = new Color[width * height];

        for (int j = 0; j < height; j++) {
            for (int i = 0; i < width; i++) {
                foreach (var b in PCGConfig.Biomes)
                {
                    if(map[i,j] == 7)
                        colorMap[j * width + i] = Color.blue;
                    else if (b.BiomeId == map[i,j])
                        colorMap[j * width + i] = b.color;
                    else
                        continue;
                }
                
            }
        }

        texture.SetPixels(colorMap);
        texture.Apply();
        //texture.filterMode = FilterMode.Point;

        textureRenderer.material.mainTexture = texture;
       // textureRenderer.transform.localScale = new Vector3(width, 0, height);
    }

    public void GenerateWorld()
    {
        //cache map resolutions
        int alphaMapResolution = TargetTerrain.terrainData.alphamapResolution;
        int mapResolution = TargetTerrain.terrainData.heightmapResolution;

        og_heightMap = NoiseGeneration.GenerateNoiseMap(PCGConfig.width,PCGConfig.height, PCGConfig.seed, PCGConfig.scale,
                                                        PCGConfig.octaves, PCGConfig.persistance, PCGConfig.lacunarity, PCGConfig.offset);
        fallOffMap = falloffMap.GenerateFalloffMap(PCGConfig.width, PCGConfig.height, PCGConfig.a, PCGConfig.b);
        //recalculates height map with application of fall off map and height multiplier
        heightMap = HeightMap.getHeightMap(og_heightMap, PCGConfig.width, PCGConfig.height, PCGConfig.seaLevel,fallOffMap, PCGConfig.useFalloffMap);
       
        int earliestIndex = HeightMap.getEarliestIndex(heightMap,PCGConfig.width, PCGConfig.height, PCGConfig.seaLevel);
        int latestIndex = HeightMap.getLatestIndex(heightMap,PCGConfig.width, PCGConfig.height, PCGConfig.seaLevel);

        //Climate Maps
        temperatureMap = TempMap.GenerateTemperatureMap(heightMap, PCGConfig.temperatureBias, earliestIndex, latestIndex,
                                                PCGConfig.tempHeight, PCGConfig.tempLoss,PCGConfig.baseTemp, PCGConfig.useTrueEquator);
        precipitationMap = PrecipitationMap.GeneratePrecipitationMap(og_heightMap, temperatureMap, PCGConfig.dewPoint, earliestIndex, latestIndex, 
                                                            PCGConfig.precipitationIntensity, PCGConfig.useTrueEquator, PCGConfig.humidityFlatteningThreshold);
        biomeMap = BiomeMap.GenerateBiomeMap(heightMap, temperatureMap, precipitationMap, PCGConfig.seaLevel, PCGConfig.Biomes, PCGConfig.spread, PCGConfig.spreadThreshold);

        //apply terrain heights
        TargetTerrain.terrainData = GenerateTerrain(TargetTerrain.terrainData);

        //Draw Textures
        if (drawMode == DrawMode.OriginalHeightMap)
            DrawTexture(og_heightMap);
        if (drawMode == DrawMode.FalloffMap)
            DrawTexture(fallOffMap);
        if (drawMode == DrawMode.HeightMap)
            DrawTexture(heightMap);
        if (drawMode == DrawMode.TemperatureMap)
            DrawTexture(temperatureMap);
        if (drawMode == DrawMode.PrecipitationMap)
            DrawTexture(precipitationMap);
        if (drawMode == DrawMode.BiomeMap)
            DrawBiomeTexture(biomeMap);
        
    #if UNITY_EDITOR
        if(RegenerateLayers)
            RegenerateTextures();
    #endif     
        //texturePainter.Perform_GenerateTextureMapping(PCGConfig);
        Perform_TerrainPainting(mapResolution, alphaMapResolution);
            
    }//END GenerateWorld()


    #if UNITY_EDITOR
    public void RegenerateTextures()
    {
        texturePainter = gameObject.GetComponent<TexturePainting>();
        texturePainter.Perform_LayerSetup(TargetTerrain);
    }
    #endif 

    TerrainData GenerateTerrain (TerrainData terrainData)
    {
        terrainData.heightmapResolution = PCGConfig.width + 1;
        terrainData.size = new Vector3(PCGConfig.width, PCGConfig.depth, PCGConfig.height);
        
        terrainData.SetHeights(0, 0, heightMap);
        return terrainData;
    }

    public ProcGenConfig getConfig()
    {
        return PCGConfig;
    }
    
    void Perform_TerrainPainting(int mapResolution, int alphaMapResolution)
    {
        float[,] heightMap = TargetTerrain.terrainData.GetHeights(0, 0, mapResolution, mapResolution);
        float[,,] alphaMaps = TargetTerrain.terrainData.GetAlphamaps(0, 0, alphaMapResolution, alphaMapResolution);
        texturePainter = gameObject.GetComponent<TexturePainting>();
        // zero out all layers
        for (int y = 0; y < alphaMapResolution; ++y)
        {
            for (int x = 0; x < alphaMapResolution; ++x)
            {
                for (int layerIndex = 0; layerIndex < TargetTerrain.terrainData.alphamapLayers; ++layerIndex)
                {
                    alphaMaps[x, y, layerIndex] = 0;
                }
            }
        }   
        // generate the slope map
        float[,] SlopeMap = new float[alphaMapResolution, alphaMapResolution];
        for (int y = 0; y < alphaMapResolution; ++y)
        {
            for (int x = 0; x < alphaMapResolution; ++x)
            {
                SlopeMap[x, y] = TargetTerrain.terrainData.GetInterpolatedNormal((float) x / alphaMapResolution, (float) y / alphaMapResolution).y;
            }
        } 

        // run terrain painting for each biome
        for (int biomeIndex = 0; biomeIndex < PCGConfig.Biomes.Count; ++biomeIndex)
        {
            var biome = PCGConfig.Biomes[biomeIndex];
            if (biome.TexturePainter == null)
                continue;

            BaseTexturePainter[] modifiers = biome.TexturePainter.GetComponents<BaseTexturePainter>();

            foreach(var modifier in modifiers)
            {
                modifier.Execute(texturePainter, mapResolution, heightMap, TargetTerrain.terrainData.heightmapScale, SlopeMap, 
                alphaMaps, alphaMapResolution, biomeMap, biomeIndex, biome);
            }
        }        

        // run texture post processing
        if (PCGConfig.PaintingPostProcessingModifier != null)
        {
            BaseTexturePainter[] modifiers = PCGConfig.PaintingPostProcessingModifier.GetComponents<BaseTexturePainter>();

            foreach(var modifier in modifiers)
            {
                modifier.Execute(texturePainter, mapResolution, heightMap, TargetTerrain.terrainData.heightmapScale, SlopeMap, 
                alphaMaps, alphaMapResolution);
            }    
        }

        TargetTerrain.terrainData.SetAlphamaps(0, 0, alphaMaps);
    }
}
