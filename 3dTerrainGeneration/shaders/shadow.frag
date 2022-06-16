#version 430 core

out vec4 FragColor;
  
in vec2 TexCoords;

uniform sampler2D depthTex; //pos
uniform sampler2D normalTex; //normal   
uniform sampler2DShadow colortex4; //shadowmap
uniform sampler2DShadow colortex5; //shadowmap2

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

float sampleTexNear(vec2 shadowCoord, vec2 off, float b) {
    return texture(colortex4, vec3(shadowCoord + off * vec2(rand(TexCoords + off + time + 1), rand(TexCoords + off + time)) * .0002, b));
}

float sampleTexFar(vec2 shadowCoord, vec2 off, float b) {
    return texture(colortex5, vec3(shadowCoord + off * vec2(rand(TexCoords + off + time + 1), rand(TexCoords + off + time)) * .0012, b));
}

float sampleTex(bool far, vec2 shadowCoord, vec2 off, float b) {
    if(!far) {
        return sampleTexFar(shadowCoord, off, b);
    } else {
        return sampleTexNear(shadowCoord, off, b);
    }
}

float sampleRow(bool far, vec2 shadowCoord, float y, float b) {
    return 
        sampleTex(far, shadowCoord, vec2(-1.25, y), b) + 
        sampleTex(far, shadowCoord, vec2(-.75, y), b) + 
        sampleTex(far, shadowCoord, vec2(.75, y), b) + 
        sampleTex(far, shadowCoord, vec2(1.25, y), b);
}

vec4 get(vec3 position, mat4 matrix, float normalOffset) {
    vec4 ShadowCoord = vec4(position.xyz + (texture(normalTex, TexCoords).rgb * vec3(2, 2, 2) - vec3(1, 1, 1)) * normalOffset / shadowRes, 1.) * matrix;
    ShadowCoord /= ShadowCoord.w;
    ShadowCoord.xyz = ShadowCoord.xyz * .5 + .5;
    return ShadowCoord;
}

void main() {
    vec3 position = depthToView(TexCoords, texture(depthTex, TexCoords).r, projection);

    bool far = false;
    vec4 ShadowCoord = get(position, matrix0, 500);
    if(ShadowCoord.x < 0.1 || ShadowCoord.y < 0.1 || ShadowCoord.z < 0.1 || ShadowCoord.x > .9 || ShadowCoord.y > .9 || ShadowCoord.z >= .9) {
        ShadowCoord = get(position, matrix1, 2000);
        far = true;
    }

    float shadow = 0;
    float b = ShadowCoord.z;

    shadow += sampleRow(far, ShadowCoord.st, -1.25, b);
    shadow += sampleRow(far, ShadowCoord.st, -.75, b);
    shadow += sampleRow(far, ShadowCoord.st, .75, b);
    shadow += sampleRow(far, ShadowCoord.st, 1.25, b);
    shadow /= 16;
    // shadow = sampleTex(far, ShadowCoord.st, vec2(0), b);

    FragColor = vec4(shadow.rrr, 1.);
}