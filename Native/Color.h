#pragma once
#include <stdint.h>

uint32_t ToInt(uint8_t r, uint8_t g, uint8_t b)
{
    return (uint32_t)(r << 24 | g << 16 | b << 8);
}