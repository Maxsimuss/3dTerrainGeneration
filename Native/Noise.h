#pragma once
#include <FastNoise/FastNoise.h>

auto simplex = FastNoise::New<FastNoise::Simplex>();
float Simplex2D(float x, float y, float scale) {
	return simplex->GenSingle2D(x / scale, y / scale, 0);
}

float OctaveSimplex2D(float x, float y, int octaves, float persistence, float lacunarity, float scale) {
    float noise = 0;
    float frequency = 1;
    float amplitude = 1;
    float maxValue = 0;

    for (int octave = 0; octave < octaves; octave++)
    {
        float noiseValue = Simplex2D(x, y, scale / frequency) * amplitude;

        noise += noiseValue;
        maxValue += amplitude;

        frequency *= lacunarity;
        amplitude *= persistence;
    }

    if (maxValue > 0)
    {
        noise /= maxValue;
    }

    return noise;
}