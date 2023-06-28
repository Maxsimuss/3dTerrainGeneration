#include "pch.h"
#include <vector>

struct VertexData
{
public:
	VertexData() { }

	VertexData(int x, int y, int z, int normal, int r, int g, int b)
	{
		this->x = (char)x;
		this->y = (char)y;
		this->z = (char)z;
		this->normal = (char)normal;
		this->r = (char)r;
		this->g = (char)g;
		this->b = (char)b;
		this->reserved = 0;
	}
private:
	char x, y, z, r, g, b, normal, reserved;
};

struct MeshData {
public:
	VertexData** vertecies;
	int* lengths;
};

void Face(int x, int y, int z, int face, int r, int g, int b, int scale, std::vector<VertexData>** quadVectors)
{
	std::vector<VertexData>* faceVector = quadVectors[face];

	auto addPoint = [&](int x, int y, int z) {
		faceVector->push_back({ x * scale, y, z * scale, face, r, g, b });
	};

	switch (face)
	{
	case 0:
		addPoint(x, y, z);
		addPoint(x, y + 1, z);
		addPoint(x, y + 1, z + 1);
		addPoint(x, y + 1, z + 1);
		addPoint(x, y, z + 1);
		addPoint(x, y, z);

		break;
	case 1:
		addPoint(x + 1, y, z + 1);
		addPoint(x + 1, y, z);
		addPoint(x, y, z);
		addPoint(x, y, z);
		addPoint(x, y, z + 1);
		addPoint(x + 1, y, z + 1);
		break;
	case 2:
		addPoint(x, y, z);
		addPoint(x + 1, y, z);
		addPoint(x + 1, y + 1, z);
		addPoint(x + 1, y + 1, z);
		addPoint(x, y + 1, z);
		addPoint(x, y, z);
		break;
	case 3:
		addPoint(x, y + 1, z + 1);
		addPoint(x, y + 1, z);
		addPoint(x, y, z);
		addPoint(x, y, z);
		addPoint(x, y, z + 1);
		addPoint(x, y + 1, z + 1);

		break;
	case 4:
		addPoint(x, y, z);
		addPoint(x + 1, y, z);
		addPoint(x + 1, y, z + 1);
		addPoint(x + 1, y, z + 1);
		addPoint(x, y, z + 1);
		addPoint(x, y, z);
		break;
	case 5:
		addPoint(x + 1, y + 1, z);
		addPoint(x + 1, y, z);
		addPoint(x, y, z);
		addPoint(x, y, z);
		addPoint(x, y + 1, z);
		addPoint(x + 1, y + 1, z);
		break;
	}
}

struct vec3 {
	int x, y, z;

	int operator [] (int i) const { return ((int*)this)[i]; }
	int& operator [] (int i) { return ((int*)this)[i]; }

	vec3 operator + (vec3 other) {
		return { (int)(x + other.x), (int)(y + other.y), (int)(z + other.z) };
	}
};

#define MAX_SIZE 128
bool merged[MAX_SIZE * MAX_SIZE];

extern "C" __declspec(dllexport) MeshData __stdcall GreedyMesh(uint32_t * blocks, int width, int height, short scale, char emission)
{
	std::vector<VertexData>* quads[6];
	for (size_t i = 0; i < 6; i++)
	{
		quads[i] = new std::vector<VertexData>();
	}

	auto GetBlock = [&](vec3 pos) {
		if (pos.x >= width || pos.z >= width || pos.y >= height || pos.x < 0 || pos.z < 0 || pos.y < 0)
		{
			return (uint32_t)0;
		}

		return blocks[(pos.x * width + pos.z) * height + pos.y];
	};

	auto IsBlockFaceVisible = [&](vec3 blockPosition, int axis, bool backFace) {
		blockPosition[axis] += backFace ? -1 : 1;
		return GetBlock(blockPosition) == 0;
	};

	auto CompareStep = [&](vec3 a, vec3 b, int direction, bool backFace) {
		uint32_t blockA = GetBlock(a);
		uint32_t blockB = GetBlock(b);

		return blockA == blockB && blockB != 0 && IsBlockFaceVisible(b, direction, backFace);
	};

	vec3 Dimensions{ width, height, width };

	vec3 startPos{ 0,0,0 }, currPos{ 0,0,0 }, quadSize{ 0,0,0 }, m{ 0,0,0 }, n{ 0,0,0 }, offsetPos{ 0,0,0 };
	vec3 vertices[4] = {};

	uint32_t startBlock = 0;
	int direction = 0, workAxis1 = 0, workAxis2 = 0;

	// Iterate over each face of the blocks.
	for (int face = 0; face < 6; face++)
	{
		bool isBackFace = face > 2;
		direction = face % 3;
		char nx = (char)(direction == 0 ? isBackFace ? 0 : 14 : 7);
		char ny = (char)(direction == 1 ? isBackFace ? 0 : 14 : 7);
		char nz = (char)(direction == 2 ? isBackFace ? 0 : 14 : 7);

		workAxis1 = (direction + 1) % 3;
		workAxis2 = (direction + 2) % 3;

		startPos = { 0,0,0 };
		currPos = { 0,0,0 };


		// Iterate over the chunk layer by layer.
		for (startPos[direction] = 0; startPos[direction] < Dimensions[direction]; startPos[direction]++)
		{
			memset(merged, 0, MAX_SIZE * MAX_SIZE);

			// Build the slices of the mesh.
			for (startPos[workAxis1] = 0; startPos[workAxis1] < Dimensions[workAxis1]; startPos[workAxis1]++)
			{
				for (startPos[workAxis2] = 0; startPos[workAxis2] < Dimensions[workAxis2]; startPos[workAxis2]++)
				{
					startBlock = blocks[(startPos.x * width + startPos.z) * height + startPos.y];

					// If this block has already been merged, is air, or not visible skip it.
					if (merged[startPos[workAxis1] * MAX_SIZE + startPos[workAxis2]] || startBlock == 0 || !IsBlockFaceVisible(startPos, direction, isBackFace))
					{
						continue;
					}

					// Reset the work var
					quadSize = { 0,0,0 };

					// Figure out the width, then save it
					for (currPos = startPos, currPos[workAxis2]++; currPos[workAxis2] < Dimensions[workAxis2] && CompareStep(startPos, currPos, direction, isBackFace) && !merged[currPos[workAxis1] * MAX_SIZE + currPos[workAxis2]]; currPos[workAxis2]++) {}
					quadSize[workAxis2] = currPos[workAxis2] - startPos[workAxis2];

					// Figure out the height, then save it
					for (currPos = startPos, currPos[workAxis1]++; currPos[workAxis1] < Dimensions[workAxis1] && CompareStep(startPos, currPos, direction, isBackFace) && !merged[currPos[workAxis1] * MAX_SIZE + currPos[workAxis2]]; currPos[workAxis1]++)
					{
						for (currPos[workAxis2] = startPos[workAxis2]; currPos[workAxis2] < Dimensions[workAxis2] && CompareStep(startPos, currPos, direction, isBackFace) && !merged[currPos[workAxis1] * MAX_SIZE + currPos[workAxis2]]; currPos[workAxis2]++) {}

						// If we didn't reach the end then its not a good add.
						if (currPos[workAxis2] - startPos[workAxis2] < quadSize[workAxis2])
						{
							break;
						}
						else
						{
							currPos[workAxis2] = startPos[workAxis2];
						}
					}
					quadSize[workAxis1] = currPos[workAxis1] - startPos[workAxis1];

					// Now we add the quad to the mesh
					m = { 0,0,0 };
					m[workAxis1] = quadSize[workAxis1];

					n = { 0,0,0 };
					n[workAxis2] = quadSize[workAxis2];

					// We need to add a slight offset when working with front faces.
					offsetPos = startPos;
					offsetPos[direction] += isBackFace ? 0 : 1;

					//Draw the face to the mesh
					vertices[0] = offsetPos;
					vertices[1] = offsetPos + m;
					vertices[2] = offsetPos + m + n;
					vertices[3] = offsetPos + n;

					char cr = (char)(startBlock >> 24 & 0xFF);
					char cg = (char)(startBlock >> 16 & 0xFF);
					char cb = (char)(startBlock >> 8 & 0xFF);

					std::vector<VertexData>* quad = quads[face];

					auto addPoint = [&](vec3 pos) {
						quad->push_back({ pos.x * scale, pos.y, pos.z * scale, face, cr, cg, cb });
					};

					if (!isBackFace)
					{
						addPoint(vertices[0]);
						addPoint(vertices[1]);
						addPoint(vertices[2]);


						addPoint(vertices[0]);
						addPoint(vertices[2]);
						addPoint(vertices[3]);
					}
					else
					{
						addPoint(vertices[2]);
						addPoint(vertices[1]);
						addPoint(vertices[0]);


						addPoint(vertices[3]);
						addPoint(vertices[2]);
						addPoint(vertices[0]);
					}

					// Mark it merged
					for (int f = 0; f < quadSize[workAxis1]; f++)
					{
						for (int g = 0; g < quadSize[workAxis2]; g++)
						{
							merged[(startPos[workAxis1] + f) * MAX_SIZE + startPos[workAxis2] + g] = true;
						}
					}
				}
			}
		}
	}

	VertexData** vertecies = new VertexData * [6];
	int* sizes = new int[6];

	for (size_t i = 0; i < 6; i++)
	{
		vertecies[i] = new VertexData[quads[i]->size()];
		sizes[i] = quads[i]->size();

		copy(quads[i]->begin(), quads[i]->end(), vertecies[i]);

		delete quads[i];
	}

	return { vertecies, sizes };
}

extern "C" __declspec(dllexport) MeshData __stdcall QuickMesh(uint32_t * blocks, int width, int height, short scale, char emission)
{
	auto getBlock = [&](int x, int y, int z) {
		if (x >= width || z >= width || y >= height || x < 0 || z < 0 || y < 0)
		{
			return (uint32_t)0;
		}

		return blocks[(x * width + z) * height + y];
	};

	std::vector<VertexData>* quadVectors[6];
	for (size_t i = 0; i < 6; i++)
	{
		quadVectors[i] = new std::vector<VertexData>();
	}

	for (int x = 0; x < width; x++)
	{
		for (int z = 0; z < width; z++)
		{
			for (int y = 0; y < height; y++)
			{
				uint32_t id = getBlock(x, y, z);
				if (id != 0)
				{
					char r = (char)(id >> 16 & 0xFF);
					char g = (char)(id >> 8 & 0xFF);
					char b = (char)(id & 0xFF);

					if ((y + 1 == height || getBlock(x, y + 1, z) == 0))
					{
						Face(x, y + 1, z, 1, r, g, b, scale, quadVectors);
					}
					if ((x + 1 == width || getBlock(x + 1, y, z) == 0))
					{
						Face(x + 1, y, z, 0, r, g, b, scale, quadVectors);
					}
					if ((z + 1 == width || getBlock(x, y, z + 1) == 0))
					{
						Face(x, y, z + 1, 2, r, g, b, scale, quadVectors);
					}

					if ((y == 0 || getBlock(x, y - 1, z) == 0))
					{
						Face(x, y, z, 4, r, g, b, scale, quadVectors);
					}
					if ((x == 0 || getBlock(x - 1, y, z) == 0))
					{
						Face(x, y, z, 3, r, g, b, scale, quadVectors);
					}
					if ((z == 0 || getBlock(x, y, z - 1) == 0))
					{
						Face(x, y, z, 5, r, g, b, scale, quadVectors);
					}
				}
			}
		}
	}

	VertexData** vertecies = new VertexData * [6];
	int* sizes = new int[6];

	for (size_t i = 0; i < 6; i++)
	{
		vertecies[i] = new VertexData[quadVectors[i]->size()];
		sizes[i] = quadVectors[i]->size();

		copy(quadVectors[i]->begin(), quadVectors[i]->end(), vertecies[i]);

		delete quadVectors[i];
	}

	return { vertecies, sizes };
}

extern "C" __declspec(dllexport) void __stdcall FreeMeshData(MeshData mesh)
{
	for (size_t i = 0; i < 6; i++)
	{
		delete[] mesh.vertecies[i];
	}

	delete[] mesh.lengths;
}