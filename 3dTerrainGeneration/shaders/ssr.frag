#version 430 core

layout (depth_any) out float gl_FragDepth;
out vec4 FragColor;
out vec4 Normal;

in vec2 TexCoords;

uniform vec3 position;
uniform mat4 projection;
uniform mat4 _projection;
uniform sampler3D data;
uniform sampler2D noise;
uniform float time;
uniform int SIZE;

vec3 depthToView(vec2 texCoord, float depth, mat4 projInv) {
    vec4 ndc = vec4(texCoord, depth, 1) * 2 - 1;
    vec4 viewPos = ndc * projInv;
    return viewPos.xyz / viewPos.w;
}


float rand(vec2 co) {
    // return fract(sin(dot((co + time) * 10, vec2(12.9898, 78.233))) * 43758.5453);
    return texture(noise, (co + time * 10) * 5.54298).r;
}

struct hitResult {
    float hit;
    vec3 color;
    vec3 position;
    bvec3 mask;
};
vec3 sunColor = vec3(12, 6.4, 2) * 25;
vec3 skyColor = vec3(.25, .7, 1.4) * 10;

hitResult raycast(vec3 rayPos, vec3 rayDir, int steps) {
    ivec3 mapPos = ivec3(floor(rayPos + 0.));
	vec3 deltaDist = abs(vec3(length(rayDir)) / rayDir);
	ivec3 rayStep = ivec3(sign(rayDir));
	vec3 sideDist = (sign(rayDir) * (vec3(mapPos) - rayPos) + (sign(rayDir) * 0.5) + 0.5) * deltaDist; 
	bvec3 mask;
	for (int i = 0; i < steps; i++) {
        vec4 voxel = texture(data, vec3(mapPos) / SIZE);
        int dist = int(voxel.w * 255);
		if (dist == 0x77) {
            float d = length(vec3(mask) * (sideDist - deltaDist)); // rayDir normalized
            vec3 dst = rayPos + rayDir * d;

            return hitResult(1, voxel.rgb, dst, mask);
        }

        mask = lessThanEqual(sideDist.xyz, min(sideDist.yzx, sideDist.zxy));
        sideDist += vec3(mask) * deltaDist;
        mapPos += ivec3(vec3(mask)) * rayStep;
	}
    return hitResult(0, skyColor, rayPos, bvec3(0));
}

void main() {
    vec3 rayDir = normalize(depthToView(TexCoords, 1., projection));
    vec3 randDir = normalize(vec3(rand(TexCoords) * 2 - 1, rand(TexCoords + 1) * 2 - 1, rand(TexCoords + 2) * 2 - 1));
    vec3 sunDir = normalize(normalize(vec3(.1, .5, 1)) * 151.92 * 1000000 + randDir * randDir * 69634);

    hitResult primary = raycast(position, rayDir, 256); //primary hit
    
    float fuzz = 1;

    vec3 dir = normalize(rayDir + randDir * fuzz);
    hitResult reflectionHit = primary;
    float amt = 1;
    vec3 reflectionColor = vec3(0);

    int i = 0;
    do {
        amt /= 4;
        dir = normalize(reflect(dir, vec3(reflectionHit.mask)) + randDir * fuzz);
        
        float sunAmt = 1 - raycast(reflectionHit.position + sunDir * .0001, sunDir, 64 / (i * i + 1)).hit * dot(vec3(reflectionHit.mask), sunDir);
        reflectionHit = raycast(reflectionHit.position + dir * .0001, dir, 32 / (i * i + 1));

        reflectionColor += reflectionHit.color * amt * (sunAmt * sunColor + skyColor);
        if(reflectionHit.hit < .5) {
            break;
        }
        i++;
    } while(i < 3);
    float sunAmt = 1 - raycast(primary.position + sunDir * .001, sunDir, 128).hit;

    vec3 color = (primary.color * (sunAmt * dot(vec3(primary.mask), sunDir) * sunColor + skyColor) * .9
     + reflectionColor * primary.hit * .1) / 10;

    vec4 pos = vec4(primary.position, 1.) * _projection;
    pos.xyz /= pos.w;

    gl_FragDepth = pos.z / 2. + .5;
    if(primary.hit < .5) {
        gl_FragDepth = 1;
    }

    FragColor = vec4(color / (color + 1), 1);
    Normal = vec4(primary.mask, 1.);
}