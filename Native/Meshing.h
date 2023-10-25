#include <vector>
#include "Octree.h"
#include "BlockMask.h"

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

struct vec3 {
	int x, y, z;

	int operator [] (int i) const { return ((int*)this)[i]; }
	int& operator [] (int i) { return ((int*)this)[i]; }

	vec3 operator + (vec3 other) {
		return { (int)(x + other.x), (int)(y + other.y), (int)(z + other.z) };
	}
};

#define MAX_SIZE 128
#define MAX_LODs 5
thread_local static MeshData meshData[MAX_LODs];
thread_local static uint32_t tempBlocks[MAX_SIZE * MAX_SIZE * MAX_SIZE];

extern "C" __declspec(dllexport) MeshData __stdcall GreedyMesh(uint32_t * blocks, int width, int height, short scale, char emission)
{
	static thread_local bool merged[MAX_SIZE * MAX_SIZE];
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


extern "C" __declspec(dllexport) MeshData * MeshLODs(HybridVoxelOctree * svo, int chunkSize, int lodCount) {
	//for (int i = 0; i < lodCount; i++)
	//{
	//	short lod = (short)pow(2, i);
	//	int wl = chunkSize / lod;

	//	memset(tempBlocks, 0, wl * wl * wl);

	//	for (size_t x = 0; x < chunkSize; x++)
	//	{
	//		for (size_t z = 0; z < chunkSize; z++)
	//		{
	//			for (size_t y = 0; y < chunkSize; y++)
	//			{
	//				uint32_t bl = svo->GetValue(x * lod, y * lod, z * lod);

	//				if (bl != 0) {
	//					int _x = x / lod;
	//					int _y = y / lod;
	//					int _z = z / lod;

	//					//uint32_t curr = tempBlocks[(_x * wl + _z) * wl + _y];
	//					//if (curr & BlockMask::Important) {
	//					//	continue;
	//					//}

	//					//if (curr == 0 || bl & BlockMask::Important) {
	//						tempBlocks[(_z * wl + _x) * wl + _y] = bl;
	//					//	continue;
	//					//}

	//					//int mask = (curr & 0xFF) | (bl & 0xFF);
	//					//int r = ((curr >> 24) & 0xFF) + ((bl >> 24) & 0xFF);
	//					//int g = ((curr >> 16) & 0xFF) + ((bl >> 16) & 0xFF);
	//					//int b = ((curr >> 8) & 0xFF) + ((bl >> 8) & 0xFF);

	//					//tempBlocks[(_x * wl + _z) * wl + _y] = (r / 2) << 24 | (g / 2) << 16 | (b / 2) << 8 | mask;
	//				}
	//			}
	//		}
	//	}

	//	meshData[i] = GreedyMesh(tempBlocks, wl, wl, lod, 0);
	//}
	
	//return meshData;

	for (int i = 0; i < lodCount; i++)
	{
		short lod = (short)pow(2, i);
		int wl = chunkSize / lod;

		for (short x = 0; x < wl; x++)
		{
			for (short z = 0; z < wl; z++)
			{
				for (short y = 0; y < chunkSize; y++)
				{
					uint32_t bl = svo->GetValue(x * lod, y, z * lod);
					tempBlocks[(x * wl + z) * chunkSize + y] = bl;
				}
			}
		}

		meshData[i] = GreedyMesh(tempBlocks, wl, chunkSize, lod, 0);
	}
	
	return meshData;
}

extern "C" __declspec(dllexport) void __stdcall FreeMeshData(MeshData mesh)
{
	for (size_t i = 0; i < 6; i++)
	{
		delete[] mesh.vertecies[i];
	}

	delete[] mesh.lengths;
}