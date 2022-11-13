using UnityEngine;
using System.Collections;

public static class falloffMap
{

	public static float[,] GenerateFalloffMap(int width, int height, float a, float b) {
        float[,] falloffMap = new float[width, height];

        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                float x = i / (float)width * 2 - 1;
                float y = j / (float)height * 2 - 1;

                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                falloffMap[i, j] = Evaluate(value, a, b);
            }
        }

        return falloffMap;
    }
	public static float Evaluate(float value, float a, float b) {
		return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow((b - b * value), a));
	}

}