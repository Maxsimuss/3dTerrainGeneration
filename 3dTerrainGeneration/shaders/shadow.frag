#version 430 core

out vec4 FragColor;
  
in vec2 TexCoords;

uniform sampler2D depthTex; //pos
uniform sampler2D normalTex; //normal   
uniform sampler2D colortex4; //shadowmap
uniform sampler2D colortex5; //shadowmap2

uniform float time;
uniform float shadowRes;
uniform float shadowRadiusNear;
uniform float shadowRadiusFar;
uniform mat4 matrix0;
uniform mat4 matrix1;
uniform mat4 projection;

vec3 depthToView(vec2 texCoord, float depth, mat4 projInv) {
    vec4 ndc = vec4(texCoord, depth, 1) * 2 - 1;
    vec4 viewPos = ndc * projInv;
    return viewPos.xyz / viewPos.w;
}

float rand(vec2 co){
    return fract(sin(dot(co, vec2(12.9898, 78.233))) * 43758.5453);
}

vec4 get(vec3 position, mat4 matrix, float normalOffset) {
    vec4 ShadowCoord = vec4(position.xyz, 1.) * matrix;
    ShadowCoord /= ShadowCoord.w;
    ShadowCoord.xyz = ShadowCoord.xyz * .5 + .5;
    return ShadowCoord;
}

const float zNear = .2;
const float zFar = 4096;
float linearize_depth(float d) {
    float z_n = 2.0 * d - 1.0;
    return 2.0 * zNear * zFar / (zFar + zNear - z_n * (zFar - zNear));
}

#define BIAS .000005

float findAvgBlockerDistance(vec3 ShadowCoord) {
    int blockers = 0;
    float d = 0;
    for (int i = 0; i < 8; i++) {
        vec2 offset = vec2(rand(TexCoords + vec2(i, 0)) * 2 - 1, rand(TexCoords + vec2(0, i)) * 2 - 1) * .00125;
        float dist = texture(colortex5, ShadowCoord.xy + offset).r - ShadowCoord.z;
        if(dist + BIAS * (1 + length(offset) * 1000) < 0) {
            d -= dist;
            blockers++;
        }
    }
    if(blockers < 1) {
        return blockers;
    } else {
        return min(d / blockers, .00125);
    }
}

void main() {
    float depth = texture(depthTex, TexCoords).r;
    vec3 position = depthToView(TexCoords, depth, projection);

    float shadow = 0;
    vec3 ShadowCoord = get(position, matrix0, 500).xyz;
    if(ShadowCoord.x < 0.1 || ShadowCoord.y < 0.1 || ShadowCoord.z < 0.1 || ShadowCoord.x > .9 || ShadowCoord.y > .9 || ShadowCoord.z >= .9) {
        ShadowCoord = get(position, matrix1, 2000).xyz;
        shadow = (texture(colortex4, ShadowCoord.xy).r - ShadowCoord.z + BIAS * 2) > 0 ? 1 : 0;
    } else {
        float blockerDistance = findAvgBlockerDistance(ShadowCoord);

        for (int i = 0; i < 4; i++) {
            vec2 offset = vec2(rand(TexCoords + vec2(i, 0)) * 2 - 1, rand(TexCoords + vec2(0, i)) * 2 - 1) * blockerDistance;
            float dist = texture(colortex5, ShadowCoord.xy + offset).r - ShadowCoord.z + BIAS * (1 + length(offset) * 3400);
            shadow += dist > 0 ? 1 : 0;
        }

        shadow /= 4;
        // shadow = blockerDistance * 100;
    }

    FragColor = vec4(shadow.rrr, 1.);
}