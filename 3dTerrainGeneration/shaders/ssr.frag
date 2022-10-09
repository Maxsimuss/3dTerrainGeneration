﻿#version 430 core

out vec4 FragColorFiltered;
out vec4 FragColor;
out vec4 Position;

in vec2 TexCoords;

uniform vec2 taaOffset;
uniform vec3 position;
uniform vec3 viewDir;
uniform vec3 sunDir;
uniform vec3 skyLight;
uniform vec3 sunLight;
uniform mat4 projection;
uniform mat4 projectionPrev;
uniform mat4 _projection;
uniform sampler3D data;
uniform sampler2D depthTex;
uniform sampler2D normalTex;
uniform sampler2D memoryTex;
uniform float time;
uniform int SIZE;

#define RAD 1

float rand(vec2 co) {
    return fract(sin(dot((co + time) * 10, vec2(12.9898, 78.233))) * 43758.5453);
    // return texture(noise, (co + time * 10) * 5.54298).r;
}

vec3 depthToView(vec2 texCoord, float depth, mat4 projInv) {
    vec4 ndc = vec4(texCoord, depth, 1) * 2 - 1;
    vec4 viewPos = ndc * projInv;
    return viewPos.xyz / viewPos.w;
}


struct hitResult {
    float hit;
    vec3 color;
    vec3 position;
    bvec3 mask;
};

hitResult raycast(vec3 rayPos, vec3 _rayDir, int steps) {
    vec3 rayDir = normalize(_rayDir);
	ivec3 mapPos = ivec3(floor(rayPos + 0.));
	vec3 deltaDist = abs(vec3(length(rayDir)) / rayDir);
	ivec3 rayStep = ivec3(sign(rayDir));
	vec3 sideDist = (sign(rayDir) * (vec3(mapPos) - rayPos) + (sign(rayDir) * 0.5) + 0.5) * deltaDist; 
	bvec3 mask;
	for (int i = 0; i < steps; i++) {
        vec4 voxel = texelFetch(data, mapPos + ivec3(256), 0);
		if (voxel.w != 0) {
            float d = length(vec3(mask) * (sideDist - deltaDist)); // rayDir normalized
            vec3 dst = rayPos + rayDir * d;

            return hitResult(1, voxel.bgr, dst, mask);
        }

        mask = lessThanEqual(sideDist.xyz, min(sideDist.yzx, sideDist.zxy));
        
        sideDist += vec3(mask) * deltaDist;
        mapPos += ivec3(vec3(mask)) * rayStep;
	}
    return hitResult(0, skyLight, rayPos, bvec3(0));
}

vec3 CosWeightedRandomHemisphereDirection(vec3 n, int i)
{
    return normalize(n + vec3(rand(TexCoords + i * 3) * 2 - 1, rand(TexCoords + i * 3 + 1) * 2 - 1, rand(TexCoords + i * 3 + 2) * 2 - 1) * RAD);
}

void main() {
    float d = texture(depthTex, TexCoords).r;
    vec3 _pos = depthToView(TexCoords - taaOffset, d, projection);
    vec3 normal = texture(normalTex, TexCoords).xyz * 2 - 1;

    vec3 gi = vec3(0);
    for (int i = 0; i < 30; i++) {
        vec3 norm = CosWeightedRandomHemisphereDirection(normal, i * 2);
        vec3 dir = reflect(normalize(_pos - position), norm);
        
        hitResult hit0 = raycast(_pos + dir * .01, dir, 256);
        if(hit0.hit > .5) {
            if(raycast(hit0.position + sunDir * .01, sunDir, 256).hit < .5) {
                gi += hit0.color * sunLight * max(0, dot(vec3(hit0.mask), abs(sunDir)));
            }
            dir = reflect(dir, vec3(hit0.mask));
            dir = CosWeightedRandomHemisphereDirection(dir, i * 2 + 1);
            hitResult hit1 = raycast(hit0.position + dir * .01, dir, 128);
            if(hit1.hit > .5) {
                if(raycast(hit1.position + sunDir * .01, sunDir, 128).hit < .5) {
                    gi += hit0.color * hit1.color * sunLight * max(0, dot(vec3(hit1.mask), abs(sunDir)));
                }
            } else {
                gi += hit0.color * skyLight * 10;
            }
        } else {
            gi += skyLight * 10;
        }
    }
    gi /= 30;

    vec4 prev = vec4(_pos, 1.) * projectionPrev;
    prev /= prev.w;
    prev = prev * 0.5 + 0.5;

    // float mixAmt = .01;
    float mixAmt = .5;
    if(prev.x < 0 || prev.x > 1 || prev.y < 0 || prev.y > 1) {
        mixAmt = 1;
    }
    vec4 c = vec4(mix(texture(memoryTex, prev.xy).rgb, gi, mixAmt), d);
    FragColorFiltered = c;
    FragColor = c;
    Position = vec4(_pos, (normal.x + 1) + (normal.y + 1) * 3 + (normal.z + 1) * 9);
}