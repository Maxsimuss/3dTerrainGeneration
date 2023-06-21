// 
// 
// Native meshing??
// 
// 
// 

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

inline void AddPoint(int x, int y, int z, int face, int r, int g, int b, int scale, std::vector<VertexData>* quadVector)
{
	quadVector->push_back({ x * scale, y, z * scale, face, r, g, b });
}

void Face(int x, int y, int z, int face, int r, int g, int b, int scale, std::vector<VertexData>** quadVectors)
{
	std::vector<VertexData>* faceVector = quadVectors[face];

	switch (face)
	{
	case 0:
		AddPoint(x, y, z, face, r, g, b, scale, faceVector);
		AddPoint(x, y + 1, z, face, r, g, b, scale, faceVector);
		AddPoint(x, y + 1, z + 1, face, r, g, b, scale, faceVector);
		AddPoint(x, y + 1, z + 1, face, r, g, b, scale, faceVector);
		AddPoint(x, y, z + 1, face, r, g, b, scale, faceVector);
		AddPoint(x, y, z, face, r, g, b, scale, faceVector);

		break;
	case 1:
		AddPoint(x + 1, y, z + 1, face, r, g, b, scale, faceVector);
		AddPoint(x + 1, y, z, face, r, g, b, scale, faceVector);
		AddPoint(x, y, z, face, r, g, b, scale, faceVector);
		AddPoint(x, y, z, face, r, g, b, scale, faceVector);
		AddPoint(x, y, z + 1, face, r, g, b, scale, faceVector);
		AddPoint(x + 1, y, z + 1, face, r, g, b, scale, faceVector);
		break;
	case 2:
		AddPoint(x, y, z, face, r, g, b, scale, faceVector);
		AddPoint(x + 1, y, z, face, r, g, b, scale, faceVector);
		AddPoint(x + 1, y + 1, z, face, r, g, b, scale, faceVector);
		AddPoint(x + 1, y + 1, z, face, r, g, b, scale, faceVector);
		AddPoint(x, y + 1, z, face, r, g, b, scale, faceVector);
		AddPoint(x, y, z, face, r, g, b, scale, faceVector);
		break;
	case 3:
		AddPoint(x, y + 1, z + 1, face, r, g, b, scale, faceVector);
		AddPoint(x, y + 1, z, face, r, g, b, scale, faceVector);
		AddPoint(x, y, z, face, r, g, b, scale, faceVector);
		AddPoint(x, y, z, face, r, g, b, scale, faceVector);
		AddPoint(x, y, z + 1, face, r, g, b, scale, faceVector);
		AddPoint(x, y + 1, z + 1, face, r, g, b, scale, faceVector);

		break;
	case 4:
		AddPoint(x, y, z, face, r, g, b, scale, faceVector);
		AddPoint(x + 1, y, z, face, r, g, b, scale, faceVector);
		AddPoint(x + 1, y, z + 1, face, r, g, b, scale, faceVector);
		AddPoint(x + 1, y, z + 1, face, r, g, b, scale, faceVector);
		AddPoint(x, y, z + 1, face, r, g, b, scale, faceVector);
		AddPoint(x, y, z, face, r, g, b, scale, faceVector);
		break;
	case 5:
		AddPoint(x + 1, y + 1, z, face, r, g, b, scale, faceVector);
		AddPoint(x + 1, y, z, face, r, g, b, scale, faceVector);
		AddPoint(x, y, z, face, r, g, b, scale, faceVector);
		AddPoint(x, y, z, face, r, g, b, scale, faceVector);
		AddPoint(x, y + 1, z, face, r, g, b, scale, faceVector);
		AddPoint(x + 1, y + 1, z, face, r, g, b, scale, faceVector);
		break;
	}
}

MeshData __declspec(dllexport) Mesh(char*** blocks, int Width, int Height, short scale, unsigned int* colors, char emission)
{
	std::vector<VertexData>* quadVectors[6];
	for (size_t i = 0; i < 6; i++)
	{
		quadVectors[i] = new std::vector<VertexData>();
	}

	for (int x = 0; x < Width; x++)
	{
		for (int z = 0; z < Width; z++)
		{
			for (int y = 0; y < Height; y++)
			{
				char id = blocks[x][z][y];
				if (id != 0)
				{
					unsigned int color = colors[id - 1];
					char r = (char)(color >> 16 & 0xFF);
					char g = (char)(color >> 8 & 0xFF);
					char b = (char)(color & 0xFF);

					if ((y + 1 == Height || blocks[x][z][y + 1] == 0))
					{
						Face(x, y + 1, z, 1, r, g, b, scale, quadVectors);
					}
					if ((x + 1 == Width || blocks[x + 1][z][y] == 0))
					{
						Face(x + 1, y, z, 0, r, g, b, scale, quadVectors);
					}
					if ((z + 1 == Width || blocks[x][z + 1][y] == 0))
					{
						Face(x, y, z + 1, 2, r, g, b, scale, quadVectors);
					}

					if ((y == 0 || blocks[x][z][y - 1] == 0))
					{
						Face(x, y, z, 4, r, g, b, scale, quadVectors);
					}
					if ((x == 0 || blocks[x - 1][z][y] == 0))
					{
						Face(x, y, z, 3, r, g, b, scale, quadVectors);
					}
					if ((z == 0 || blocks[x][z - 1][y] == 0))
					{
						Face(x, y, z, 5, r, g, b, scale, quadVectors);
					}
				}
			}
		}
	}

	VertexData* vertecies[6];
	int sizes[6];

	for (size_t i = 0; i < 6; i++)
	{
		vertecies[i] = new VertexData[quadVectors[i]->size()];
		sizes[i] = quadVectors[i]->size();
		
		copy(quadVectors[i]->begin(), quadVectors[i]->end(), vertecies[i]);

		delete quadVectors[i];
	}

	return { vertecies, sizes };
}

void __declspec(dllexport) FreeMeshData(MeshData mesh) 
{
	for (size_t i = 0; i < 6; i++)
	{
		delete[] mesh.vertecies[i];
	}

	delete[] mesh.lengths;
}

BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}

