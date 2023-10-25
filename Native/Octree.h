#pragma once
#include <stdint.h>
#include <mutex>
#include <iostream>

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

	~OctreeNode() {
		if (!isLeaf) {
			delete children;
		}
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

class HybridVoxelOctree {
public:
	HybridVoxelOctree(int depth) {
		node = OctreeNode(0, 0, 0, depth);
		size = pow(2, depth);
		uncompressedData = new uint32_t[size * size * size];
		memset(uncompressedData, 0, size * size * size * sizeof(uint32_t));
	}

	~HybridVoxelOctree() {
		if (!isCompressed) {
			delete[] uncompressedData;
		}
	}

	void SetVoxel(int x, int y, int z, uint32_t value) {
		//mutex.lock();

		if (isCompressed) {
			node.SetVoxel(x, y, z, value);
		}
		else {
			uncompressedData[(x * size + z) * size + y] = value;
		}

		//mutex.unlock();
	}

	uint32_t GetValue(int x, int y, int z) {

		if (isCompressed) {
			return node.GetValue(x, y, z);
		}
		else {
			return uncompressedData[(x * size + z) * size + y];
		}
	}

	void Compress() {
		mutex.lock();

		if (isCompressed) {
			mutex.unlock();
			return;
		}

		for (size_t i = 0; i < size * size * size; i++)
		{
			node.SetVoxel(i / size / size, i % size, i / size % size, uncompressedData[i]);
		}

		isCompressed = true;

		delete[] uncompressedData;
		mutex.unlock();
	}

	void CompressEmpty() {
		mutex.lock();
		if (isCompressed) {
			mutex.unlock();
			return;
		}

		isCompressed = true;
		delete[] uncompressedData;
		mutex.unlock();
	}

private:
	bool isCompressed = false;
	int size = 0;
	OctreeNode node;
	uint32_t* uncompressedData;
	std::mutex mutex;
};

extern "C" __declspec(dllexport) HybridVoxelOctree * CreateSVO(int depth) {
	return new HybridVoxelOctree(depth);
}

extern "C" __declspec(dllexport) void DeleteSVO(HybridVoxelOctree * svo) {
	delete svo;
}

extern "C" __declspec(dllexport) void SetVoxel(HybridVoxelOctree * svo, int x, int y, int z, uint32_t value) {
	svo->SetVoxel(x, y, z, value);
}

extern "C" __declspec(dllexport) void FillArea(HybridVoxelOctree * svo, uint32_t * data, int x, int y, int z, int w, int h, int d) {
	for (size_t _x = 0; _x < w; _x++)
	{
		for (size_t _z = 0; _z < d; _z++)
		{
			for (size_t _y = 0; _y < h; _y++)
			{
				uint32_t value = data[(_x * w + _z) * h];

				svo->SetVoxel(_x, _y, _z, value);
			}
		}
	}
}

extern "C" __declspec(dllexport) void GetRow(HybridVoxelOctree * svo, uint32_t * data, int x, int z, int size) {
	for (size_t i = 0; i < size; i++)
	{
		data[i] = svo->GetValue(x, i, z);
	}
}

extern "C" __declspec(dllexport) uint32_t GetValue(HybridVoxelOctree * svo, int x, int y, int z) {
	return svo->GetValue(x, y, z);
}

extern "C" __declspec(dllexport) void Compress(HybridVoxelOctree * svo) {
	return svo->Compress();
}

extern "C" __declspec(dllexport) void CompressEmpty(HybridVoxelOctree * svo) {
	return svo->CompressEmpty();
}