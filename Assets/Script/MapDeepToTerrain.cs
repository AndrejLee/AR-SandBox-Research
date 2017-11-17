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

