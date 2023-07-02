#pragma once
#include <stdint.h>

class OctreeNode
{
public:
	OctreeNode() 
	{
		depth = 0;

		isLeaf = true;
		value = 0;

		centerX = 0;
		centerY = 0;
		centerZ = 0;
	}

	OctreeNode(uint8_t x, uint8_t y, uint8_t z, uint8_t depth)
	{
		this->depth = depth;

		isLeaf = true;
		value = 0;

		uint8_t childDepth = (uint8_t)(depth - 1);
		uint8_t childSize = (uint8_t)(1 << childDepth);

		centerX = (uint8_t)(x + childSize);
		centerY = (uint8_t)(y + childSize);
		centerZ = (uint8_t)(z + childSize);
	}

	void SetVoxel(int x, int y, int z, uint32_t value)
	{
		if (depth == 0)
		{
			this->value = value;
		}
		else
		{
			if (isLeaf && this->value == value)
			{
				return;
			}

			if (isLeaf)
			{
				initChildren();
				for (int i = 0; i < 8; i++)
				{
					children[i].value = this->value;
				}
			}

			int childIndex = GetChildIndex(x, y, z);
			children[childIndex].SetVoxel(x, y, z, value);

			TryCollapse();
		}
	}

	uint32_t GetValue(int x, int y, int z)
	{
		if (isLeaf)
		{
			return value;
		}
		else
		{
			int childIndex = GetChildIndex(x, y, z);
			return children[childIndex].GetValue(x, y, z);
		}
	}

private:
	bool isLeaf;

	// used to store a uint32_t value or as pointer to children nodes
	uint32_t value;
	OctreeNode* children;

	uint8_t centerX, centerY, centerZ;
	uint8_t depth;

	void initChildren()
	{
		uint8_t childDepth = (uint8_t)(depth - 1);
		uint8_t childSize = (uint8_t)(1 << childDepth);

		children = new OctreeNode[8];

		uint8_t x = (uint8_t)(centerX - childSize);
		uint8_t y = (uint8_t)(centerY - childSize);
		uint8_t z = (uint8_t)(centerZ - childSize);

		children[0] = OctreeNode(x, y, z, childDepth);
		children[1] = OctreeNode((uint8_t)(x + childSize), y, z, childDepth);
		children[2] = OctreeNode(x, (uint8_t)(y + childSize), z, childDepth);
		children[3] = OctreeNode((uint8_t)(x + childSize), (uint8_t)(y + childSize), z, childDepth);
		children[4] = OctreeNode(x, y, (uint8_t)(z + childSize), childDepth);
		children[5] = OctreeNode((uint8_t)(x + childSize), y, (uint8_t)(z + childSize), childDepth);
		children[6] = OctreeNode(x, (uint8_t)(y + childSize), (uint8_t)(z + childSize), childDepth);
		children[7] = OctreeNode((uint8_t)(x + childSize), (uint8_t)(y + childSize), (uint8_t)(z + childSize), childDepth);

		isLeaf = false;
	}

	int GetChildIndex(int x, int y, int z)
	{
		int childIndex = 0;
		if (x >= centerX) childIndex |= 1;
		if (y >= centerY) childIndex |= 2;
		if (z >= centerZ) childIndex |= 4;
		return childIndex;
	}

	void TryCollapse()
	{
		if (isLeaf) return;

		uint32_t val = children[0].value;

		for (int i = 0; i < 8; i++)
		{
			if (!children[i].isLeaf || val != children[i].value)
			{
				return;
			}
		}

		isLeaf = true;
		delete[] children;
		value = val;
	}
};

class SparseVoxelOctree {
public:
	SparseVoxelOctree(int depth) {
		root = OctreeNode(0, 0, 0, depth);
	}

	void SetVoxel(int x, int y, int z, uint32_t value)
	{
		root.SetVoxel(x, y, z, value);
	}

	uint32_t GetValue(int x, int y, int z)
	{
		return root.GetValue(x, y, z);
	}

private:
	OctreeNode root;
};

extern "C" __declspec(dllexport) SparseVoxelOctree * CreateSVO(int depth) {
	return new SparseVoxelOctree(depth);
}

extern "C" __declspec(dllexport) void DeleteSVO(SparseVoxelOctree * svo) {
	delete svo;
}

extern "C" __declspec(dllexport) void SetVoxel(SparseVoxelOctree * svo, int x, int y, int z, uint32_t value) {
	return svo->SetVoxel(x, y, z, value);
}

extern "C" __declspec(dllexport) uint32_t GetValue(SparseVoxelOctree * svo, int x, int y, int z) {
	return svo->GetValue(x, y, z);
}