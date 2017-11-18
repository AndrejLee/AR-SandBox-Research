using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

public class MapDeepToTerrain : MonoBehaviour {

    short[] DepthImage;
    public DepthWrapper KinectDepth;
    bool init = true;

    // Use this for initialization
    void Start() {

    }

    // Update is called once per frame
    void Update()
    {
        if (KinectDepth.pollDepth())
        {
            DepthImage = KinectDepth.depthImg;
           

            byte[] result = new byte[DepthImage.Length * sizeof(short)];
            int lengthResult = result.Length;
            Buffer.BlockCopy(DepthImage, 0, result, 0, result.Length);
            //loadDeep(GetComponent<Terrain>().terrainData, result, false);

            loadDeep(GetComponent<Terrain>().terrainData, DepthImage, false);

            //WriteToFile();
        }
    }

    void loadDeep(TerrainData tData, byte[] rawData, bool adjustResolution = false)
    {
        int h = (int)Mathf.Sqrt((float)rawData.Length / 2);
        if (adjustResolution)
        {
            var size = tData.size;
            tData.heightmapResolution = h;
            tData.size = size;
        }
        else if (h > tData.heightmapHeight)
        {
            h = tData.heightmapHeight;
        }
        int w = h;

        float[,] data = new float[h, w];
        int i = 0;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int u;

                // little-endian (windows)
                u = rawData[i + 1] << 8 | rawData[i];

                float v = (float)u / 0xFFFF;
                data[y, x] = v;
                i += 2;
            }
        }

        tData.SetHeights(0, 0, data);
       
    }

    void loadDeep(TerrainData tData, short[] rawData, bool adjustResolution = false)
    {
        int h = 240;
        int w = 320;


        //SHIT
        //int w = 240;
        //int h = 320;

        float[,] data = new float[h,w];
        int i = rawData.Length - 1;
        float maxVal = (float) rawData.Max() / 6000;
        
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {

                    //data[y, x] = (float)5 * (y + x) / 0xFFFF;
                    //if (rawData[i] < 1000)
                    //{
                    //    data[y, x] = 0;
                    //}
                    //else if (1000 <= rawData[i] && rawData[i] < 2000)
                    //{
                    //    data[y, x] = 0.1f;
                    //}
                    //else
                    //{
                    //    data[y, x] = 0;
                    //}

                    float temp = (float)rawData[i] / 6000;

                    data[y, x] = maxVal - temp;
                    i--;
                }
            }
        tData.size = new Vector3(w, 100, h);
        tData.SetHeights(0, 0, data);
    }
	
	private void mapColor(TerrainData terrainData, int w, int h)
    {
        // Splatmap data is stored internally as a 3d array of floats, so declare a new empty array ready for your custom splatmap data:
        float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                // Normalise x/y coordinates to range 0-1 
                float y_01 = (float)y / (float)terrainData.alphamapHeight;
                float x_01 = (float)x / (float)terrainData.alphamapWidth;

                // Sample the height at this location (note GetHeight expects int coordinates corresponding to locations in the heightmap array)
                float height = terrainData.GetHeight(Mathf.RoundToInt(y_01 * terrainData.heightmapHeight), Mathf.RoundToInt(x_01 * terrainData.heightmapWidth));

                // Calculate the normal of the terrain (note this is in normalised coordinates relative to the overall terrain dimensions)
                Vector3 normal = terrainData.GetInterpolatedNormal(y_01, x_01);

                // Calculate the steepness of the terrain
                float steepness = terrainData.GetSteepness(y_01, x_01);

                // Setup an array to record the mix of texture weights at this point
                float[] splatWeights = new float[terrainData.alphamapLayers];

                // CHANGE THE RULES BELOW TO SET THE WEIGHTS OF EACH TEXTURE ON WHATEVER RULES YOU WANT

                // Texture[0] has constant influence
                splatWeights[0] = 0.5f;

                // Texture[1] is stronger at lower altitudes
                splatWeights[1] = Mathf.Clamp01((terrainData.heightmapHeight - height));

                // Texture[2] stronger on flatter terrain
                // Note "steepness" is unbounded, so we "normalise" it by dividing by the extent of heightmap height and scale factor
                // Subtract result from 1.0 to give greater weighting to flat surfaces
                splatWeights[2] = 1.0f - Mathf.Clamp01(steepness * steepness / (terrainData.heightmapHeight / 5.0f));

                // Texture[3] increases with height but only on surfaces facing positive Z axis 
                //splatWeights[3] = height * Mathf.Clamp01(normal.z);

                // Sum of all textures weights must add to 1, so calculate normalization factor from sum of weights
                float z = splatWeights.Sum();

                // Loop through each terrain texture
                for (int i = 0; i < terrainData.alphamapLayers; i++)
                {

                    // Normalize so that sum of all texture weights = 1
                    splatWeights[i] /= z;

                    // Assign this point to the splatmap array
                    splatmapData[x, y, i] = splatWeights[i];
                }
            }
        }

        // Finally assign the new splatmap to the terrainData:
        terrainData.SetAlphamaps(0, 0, splatmapData);
    }

    void WriteShorts(short[] values, string path)
    {
        using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
        {
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                foreach (short value in values)
                {
                    bw.Write(value);
                }
            }
        }
    }

    void FromShort(short number, out byte byte1, out byte byte2)
    {
        byte2 = (byte)(number >> 8);
        byte1 = (byte)(number & 255);
    }

    void WriteToFile()
    {
        if (init)
        {
            init = false;
            using (FileStream fs = new FileStream("a.txt", FileMode.CreateNew, FileAccess.Write))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                Debug.Log(DepthImage.Length);

                for (int i = 0; i < DepthImage.Length; i++)
                {
                    if (i % 320 == 0)
                    {
                        sw.WriteLine("");
                    }
                    if (DepthImage[i] < 1000)
                    {
                        sw.Write(("0").ToString());
                    }
                    else if (1000 < DepthImage[i] && DepthImage[i] < 2000)
                    {
                        sw.Write(("2").ToString());
                    }
                    else
                    {
                        sw.Write(("7").ToString());
                    }
                }
                sw.Close();
            }

        }
    }

}

