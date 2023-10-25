#pragma once

enum BlockMask {
	Fertile = 1,
	Structure = 1 << 1,
	Road = 1 << 2,
	Important = 1 << 3,
};