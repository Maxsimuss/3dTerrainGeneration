#version 430 core
#pragma optionNV (unroll all)

out vec4 FragColor;
  
in vec2 TexCoords;

struct Light {
    vec3 position;
    vec3 color;
};


uniform sampler2D depthTex; //depth
uniform sampler2D normalTex; //normal
uniform sampler2D shadowTex[3];
uniform mat4 matrices[3];
uniform int cuts[3];

#define fogQuality 20.

uniform vec3 viewPos;
uniform float time;

uniform mat4 projection;
uniform Light sun;

float rand(vec2 co){
    return fract(sin(dot((co + fract(time * 42.1249104)) * 1000, vec2(12.9898, 78.233))) * 43758.5453);
}

const float zNear = .5;
const float zFar = 3072;
float linearize_depth(float d)
{
    float z_n = 2.0 * d - 1.0;
    return 2.0 * zNear * zFar / (zFar + zNear - z_n * (zFar - zNear));
}

vec3 depthToView(vec2 texCoord, float depth, mat4 projInv) {
    vec4 ndc = vec4(texCoord, depth, 1) * 2 - 1;
    vec4 viewPos = ndc * projInv;
    return viewPos.xyz / viewPos.w;
}

void main() {
    float depth = texture(depthTex, TexCoords).r;
    vec3 position = depthToView(TexCoords, depth, projection);

    float fog = 0;
    float sunStr = max(dot(vec3(0, 1, 0), sun.position), 0.0);
    sunStr = pow(min(sunStr, .1) * 10, 10);
    mat4 iv = inverse(projection);

    float c0 = cuts[0];
    float c1 = cuts[1];
    for (int x = 0; x < fogQuality; x++) {
        vec4 _pos = vec4(mix(viewPos, position, clamp((x + rand(TexCoords + x/20.))/fogQuality, 0., 1.)), 1.);

        float d = (_pos * iv).z;
        float BIAS = .000002;
        int idx = 0;
        if(d > c0) {
            idx = 2;
            BIAS = .000075;
        } else if(d > c1) {
            idx = 1;
            BIAS = .00002;
        }

        vec4 ShadowCoord = _pos * matrices[idx];
        ShadowCoord /= ShadowCoord.w;
        ShadowCoord.xyz = ShadowCoord.xyz * .5 + .5;
        fog += (((texture(shadowTex[idx], ShadowCoord.xy).r - ShadowCoord.z + BIAS) > 0 ? 1 : 0) * .5 + .5);
    }
    
    float c = fog / fogQuality;
    FragColor = vec4(c, c, c, linearize_depth(depth) / zFar);
    // FragColor = fog / fogQuality;
}