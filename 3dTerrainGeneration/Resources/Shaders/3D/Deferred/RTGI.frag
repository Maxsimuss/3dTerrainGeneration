#version 430 core
precision highp float;

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
uniform mat4 viewMatrix;
uniform mat4 projMatrix;
uniform mat4 projection;
uniform mat4 projectionPrev;
uniform mat4 _projection;
uniform usampler3D data;
uniform sampler2D depthTex;
uniform sampler2D normalTex;
uniform sampler2D memoryTex;
uniform float time;
uniform int SIZE;
uniform vec2 wh;

#define RAD 1
#define RAYTRACE_TAA_MIX .1
#define RAYTRACE_SPP 200
#define RAYTRACE_BIAS 0.1
#define RAY_LEN0 32
#define RAY_LEN1 64

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

hitResult raycast(vec3 rayPos, vec3 _rayDir, int steps, ivec3 off) {
    vec3 rayDir = normalize(_rayDir);
	ivec3 mapPos = ivec3(floor(rayPos));
	vec3 deltaDist = abs(vec3(length(rayDir)) / rayDir);
	ivec3 rayStep = ivec3(sign(rayDir));
	vec3 sideDist = (sign(rayDir) * (vec3(mapPos) - rayPos) + (sign(rayDir) * 0.5) + 0.5) * deltaDist; 
	bvec3 mask;
	for (int i = 0; i < steps; i++) {
        uint voxel = uint(texelFetch(data, mapPos.yzx, 0).r);
		if ((voxel & 0x01) != 0) {
            float d = length(vec3(mask) * (sideDist - deltaDist)); // rayDir normalized
            vec3 dst = rayPos + rayDir * d;

            return hitResult(1, vec3(float((voxel >> 6) & 0x03) / 3., float((voxel >> 3) & 0x07) / 7., float((voxel >> 1) & 0x03) / 3.), dst, mask);
        }

        mask = lessThanEqual(sideDist.xyz, min(sideDist.yzx, sideDist.zxy));
        
        sideDist += vec3(mask) * deltaDist;
        mapPos += ivec3(vec3(mask)) * rayStep;
	}
    return hitResult(0, skyLight, rayPos, bvec3(0));
}

void main() {
    float d = texture(depthTex, TexCoords).r;
    // vec3 _pos = depthToView(TexCoords, d, projection);

    vec4 viewVect = (vec4(TexCoords, 1, 1) * 2 - 1) * inverse(viewMatrix* projMatrix);
    viewVect /= viewVect.w;
    viewVect.xyz = normalize(viewVect.xyz);
    hitResult primary = raycast(position, viewVect.xyz, 256, ivec3(0));
    vec3 _pos = primary.position;

    vec3 normal = texture(normalTex, TexCoords).xyz * 2 - 1;

    hitResult shadow = raycast(primary.position + sunDir * .01, sunDir, 256, ivec3(0));

    vec3 gi = vec3(0);
    gi = primary.color * (2 - shadow.hit) * .5;

    // gi = hit0.color * hit0.hit + normal / 10;
    // gi = vec3(texelFetch(data, ivec3(position), 0).r) + normal / 10;

    // for (int i = 0; i < RAYTRACE_SPP; i++) {
    //     vec3 norm = CosWeightedRandomHemisphereDirection(normal, i * 2);
    //     vec3 dir = reflect(viewVect.xyz, norm);
        
    //     hitResult hit0 = raycast(_pos + dir * RAYTRACE_BIAS, dir, RAY_LEN0, ivec3(256));
    //     if(hit0.hit > .5) {
    //         if(raycast(hit0.position + sunDir * RAYTRACE_BIAS, sunDir, RAY_LEN1, ivec3(256)).hit < .5) {
    //             gi += hit0.color * sunLight * 2 * max(0, dot(vec3(hit0.mask), abs(sunDir)));
    //         }
    //     } else {
    //         gi += skyLight * 30;
    //     }
    // }
    // gi /= RAYTRACE_SPP;

    // // vec4 c = vec4(mix(mem.rgb, gi, mixAmt), d);
    // vec4 p = vec4(_pos, (normal.x + 1) + (normal.y + 1) * 3 + (normal.z + 1) * 9);
    // if(d == 1) {
    //     p = vec4(100000);
    // }
    FragColorFiltered = vec4(gi, 1.0);
    FragColor = vec4(gi, 1.0);
    Position = vec4(0);
}