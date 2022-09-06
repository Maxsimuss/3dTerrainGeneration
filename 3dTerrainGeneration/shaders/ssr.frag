#version 430 core

layout (depth_any) out float gl_FragDepth;
out vec4 FragColor;

in vec2 TexCoords;

uniform vec3 position;
uniform vec3 sunDir;
uniform vec3 skyLight;
uniform vec3 sunLight;
uniform mat4 projection;
uniform mat4 _projection;
uniform sampler3D data;
uniform sampler2D noise;
uniform sampler2D depthTex;
uniform sampler2D normalTex;
uniform float time;
uniform int SIZE;

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

hitResult raycast(vec3 rayPos, vec3 rayDir, int steps) {
    ivec3 mapPos = ivec3(rayPos);
	vec3 deltaDist = abs(vec3(length(rayDir)) / rayDir);
	ivec3 rayStep = ivec3(sign(rayDir));
	vec3 sideDist = (sign(rayDir) * (vec3(mapPos) - rayPos) + (sign(rayDir) * 0.5) + 0.5) * deltaDist; 
	bvec3 mask;
	for (int i = 0; i < steps; i++) {
        vec3 xd = vec3(mapPos.z, mapPos.y, mapPos.x);
        vec4 voxel = texelFetch(data, mapPos, 0);
        int dist = int(voxel.w * 255);
		if (dist == 0x77) {
            float d = length(vec3(mask) * (sideDist - deltaDist)); // rayDir normalized
            vec3 dst = rayPos + rayDir * d;

            return hitResult(1, voxel.rgb, dst, mask);
        }

        mask = lessThanEqual(sideDist.xyz, min(sideDist.yzx, sideDist.zxy));
        sideDist += vec3(mask) * deltaDist;
        mapPos += ivec3(mask) * rayStep;
        mask = lessThanEqual(sideDist.xyz, min(sideDist.yzx, sideDist.zxy));
        sideDist += vec3(mask) * deltaDist;
        mapPos += ivec3(mask) * rayStep;
	}
    return hitResult(0, skyLight, rayPos, bvec3(0));
}

vec3 CosWeightedRandomHemisphereDirection( vec3 n, float rand1, float rand2 )
{
    float Xi1 = rand1;
    float Xi2 = rand2;

    float  theta = acos(sqrt(1.0-Xi1));
    float  phi = 2.0 * 3.1415926535897932384626433832795 * Xi2;

    float xs = sin(theta) * cos(phi);
    float ys = cos(theta);
    float zs = sin(theta) * sin(phi);

    vec3 y = n;
    vec3 h = y;
    if (abs(h.x)<=abs(h.y) && abs(h.x)<=abs(h.z))
        h.x= 1.0;
    else if (abs(h.y)<=abs(h.x) &&abs(h.y)<=abs(h.z))
        h.y= 1.0;
    else
        h.z= 1.0;

    vec3 x = normalize(cross(h,y));
    vec3 z = normalize(cross(x,y));

    vec3 direction = xs * x + ys * y + zs * z;
    return normalize(direction);
}

void main() {
    vec3 _pos = depthToView(TexCoords, texture(depthTex, TexCoords).r, projection);
    vec3 normal = texture(normalTex, TexCoords).xyz * 2 - 1;

    vec3 gi = vec3(0);
    for (int i = 0; i < 40; i++) {
        float r = 1;
        vec3 dir = CosWeightedRandomHemisphereDirection(normal, (rand(TexCoords + i * 3) * 2 - 1) * r, (rand(TexCoords + i * 3 + 1.) * 2 - 1) * r);

        hitResult hit = raycast(_pos + dir * 4, dir, 128);
        if(hit.hit > .5) {
            if(raycast(hit.position + sunDir * .1, sunDir, 128).hit < .5) {
                gi += hit.color * sunLight * max(0, dot(dir, normal));
            }
        } else {
            gi += skyLight / 2;
        }
    }
    FragColor = vec4(gi / 10, 1);
}