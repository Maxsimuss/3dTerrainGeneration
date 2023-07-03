#pragma once
#include "Octree.h"
#include "BiomeGenerator.h"
#include "Noise.h"

enum BlockMask {
	Fertile = 1,
	Structure = 1 << 1,
	Road = 1 << 2,
};

extern "C" __declspec(dllexport) void GenerateTerrain(SparseVoxelOctree * svo, BiomeGenerator * biomeGenerator, int chunkSize, int locationX, int locationY, int locationZ)
{
	for (int x = 0; x < chunkSize; x++)
	{
		int X = locationX + x;
		for (int z = 0; z < chunkSize; z++)
		{
			int Z = locationZ + z;

			BiomeInfo biome = BiomeGenerator::GetBiomeInfo(X, Z);
			float distanceToRoad = std::clamp((abs(Simplex2D(X, Z, 1000)) - .02f) * 10.f, 0.f, 1.f);
			bool isRoad = distanceToRoad == 0;

			float height = OctaveSimplex2D(X, Z, 7, .5f, 3, 2000) * 100;
			float variance = 1;

			if (height < -25)
			{
				height += 25;
				height *= 25;
				variance *= 25;
				height -= 25;
			}

			if (height < 0)
			{
				height /= 5;
			}

			if (height < -150)
			{
				height += 150;
				height /= 5;
				variance /= 5;
				height -= 150;
			}

			if (height < -200)
			{
				height += 200;
				height /= 5;
				variance /= 5;
				height -= 200;
			}

			if (height > 25)
			{
				height -= 25;
				height *= 5;
				variance *= 5;
				height += 25;
			}

			if (height > 150)
			{
				height -= 150;
				height /= 5;
				variance /= 5;
				height += 150;
			}

			if (height > -25 && height < 50)
			{
				height += 25;
				height /= 75;
				height *= 2;
				height -= 1;
				height = pow(height, 3) * (1 - distanceToRoad) + height * distanceToRoad;
				height += 1;
				height /= 2;
				height *= 75;
				height -= 25;
			}
			else
			{
				isRoad = false;
			}

			height = (int)round(height);

			for (int y = 0; y < chunkSize && y <= height - locationY; y++)
			{
				int Y = locationY + y;

				if (height == Y)
				{
					uint32_t block = 0;

					if (Simplex2D(X, Z, 2) * .5 + .5 > biome.Temperature / -30)
					{
						if (isRoad)
						{
							block = ToInt(120, 80, 60);
							block |= (uint32_t)BlockMask::Road;
						}
						else
						{
							block = biomeGenerator->GetGrassColor(biome);
							if (variance < 5)
							{
								block |= (uint32_t)BlockMask::Fertile;
							}
						}
					}
					else
					{
						block = ToInt(200, 200, 220);
					}


					svo->SetVoxel(x, y, z, block);
				}
				else
				{
					svo->SetVoxel(x, y, z, ToInt(100, 100, 100));
				}
			}
		}
	}
}
