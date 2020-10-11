// Copyright (c) 2020 Cloudcell Limited

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public int worldScaleX = 100;
    public int worldScaleZ = 100;

    public float freq = 10;
    public float amp = 10;

    public GameObject block;
    public GameObject wather;

    public int seed;

    public float watherLevel = 2;
    void Start()
    {
        if (seed == 0)
        {
            seed = Random.Range(-9999, 9999);
        }
        Generate();
    }

    void Generate()
    {
        int genScaleZ = 0;
        int genScaleX = 0;

        GameObject plane = Instantiate(wather, new Vector3(worldScaleX / 2f - 0.5f, wather.transform.position.y, worldScaleZ / 2f - 0.5f), Quaternion.identity);
        plane.transform.localScale = new Vector3(0.1f * worldScaleX, 1f, 0.1f * worldScaleZ);

        for (; genScaleZ < worldScaleZ; genScaleZ++)
        {
            for (; genScaleX < worldScaleX; genScaleX++)
            {
                float y = Mathf.PerlinNoise(genScaleX / freq + seed, genScaleZ / freq + seed) * amp;

                float cubeY;
                if (y < watherLevel)
                {
                    cubeY = 0;
                }
                else
                {
                    cubeY = 1;
                }
                GameObject cube = Instantiate(block, new Vector3(genScaleX, cubeY, genScaleZ), Quaternion.identity);

                //cube.GetComponent<MeshRenderer>().material.color = new Color(1 - y / freq, 1f, 0f, 0f);

            }
            genScaleX = 0;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

}