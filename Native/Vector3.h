#pragma once

struct Vector3 {
	float x, y, z;

	bool operator< (const Vector3& o) const {
		return (x + y + z) < (o.x + o.y + o.z);
	}

	bool operator== (const Vector3& o) const {
		return x == o.x && y == o.y && z == o.z;
	}
};