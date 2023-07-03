#pragma once
#include <math.h>
#include <algorithm>
#include <map>
#include "Noise.h"
#include "Vector3.h"
#include "Color.h"

struct BiomeInfo
{
public:
	float Humidity, Temperature, Fertility;

	BiomeInfo(float temperature, float humidity, float fertility)
	{
		Temperature = temperature;
		Humidity = humidity;
		Fertility = fertility;
	}

	float DistanceTo(BiomeInfo other)
	{
		float dt = (Temperature - other.Temperature) / 30;
		float dh = (Humidity - other.Humidity) / 100;
		float df = (Fertility - other.Fertility) / 100;

		return sqrt(dt * dt + dh * dh + df * df);
	}

	bool operator< (const BiomeInfo& o) const {
		return (Temperature + Humidity + Fertility) < (o.Temperature + o.Humidity + o.Fertility);
	}

	bool operator== (const BiomeInfo& o) const {
		return Temperature == o.Temperature && Humidity == o.Humidity && Fertility == o.Fertility;
	}
};

class BiomeGenerator
{
public:
	BiomeGenerator()
	{
		colors[{ 50, 0, 0 }] = { 36, 242, 91 };       // desert
		colors[{ 50, 100, 100 }] = { 111, 212, 23 };  // jungle
		colors[{ 20, 50, 50 }] = { 102, 252, 78 };    // normal??
		colors[{ -10, 0, 50 }] = { 72, 224, 214 };    // cold
		colors[{ -30, 0, 50 }] = { 173, 255, 250 };   // cold asf
	}

	static BiomeInfo GetBiomeInfo(int X, int Z)
	{
		float Temperature = Simplex2D(X - 32898, Z + 29899, 10000) * 40 + 10; // -30 to 50 deg
		float Humidity = std::clamp(Simplex2D(X + 21389, Z - 8937, 10000) * .5f + .5f, 0.0f, 1.0f) * 100; // 0 to 100 %
		float Fertility = std::clamp(Simplex2D(X - 3874, Z + 3298, 10000) * .5f + .5f, 0.0f, 1.0f) * 100; // 0 to 100 %

		return { Temperature, Humidity, Fertility };
	}

	float TemperatureDropoff(float temp, float Y)
	{
		return temp - Y / 200; //-1 deg per 200 blocks alt.
	}

	uint32_t GetGrassColor(BiomeInfo biomeInfo)
	{
		float total = 0;
		Vector3 color = { 0, 0, 0 };
		for (auto item : colors)
		{
			float dist = 1 / biomeInfo.DistanceTo(item.first);
			total += dist;
			color.x += item.second.x * dist;
			color.y += item.second.y * dist;
			color.z += item.second.z * dist;
		}

		color.x /= total;
		color.y /= total;
		color.z /= total;

		return ToInt(color.x, color.y, color.z);
	}

private:
	std::map<BiomeInfo, Vector3> colors;
};

extern "C" __declspec(dllexport) BiomeGenerator * CreateBiomeGenerator() {
	return new BiomeGenerator();
}

extern "C" __declspec(dllexport) void DeleteBiomeGenerator(BiomeGenerator * biomeGen) {
	delete biomeGen;
}