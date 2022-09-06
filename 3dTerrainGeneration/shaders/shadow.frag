#version 430 core

out vec4 FragColor;
  
in vec2 TexCoords;

uniform sampler2D depthTex; //pos
uniform sampler2D normalTex; //normal   
uniform sampler2D shadowTex[3]; //shadowmap
uniform mat4 matrices[3];
uniform int cuts[3];
uniform vec2 taaOffset;

uniform float time;
uniform mat4 projection;

vec3 depthToView(vec2 texCoord, float depth, mat4 projInv) {
    vec4 ndc = vec4(texCoord, depth, 1) * 2 - 1;
    vec4 viewPos = ndc * projInv;
    return viewPos.xyz / viewPos.w;
}

float rand(vec2 co){
    return fract(sin(dot(co + time, vec2(12.9898, 78.233))) * 43758.5453);
}

const float zNear = .2;
const float zFar = 3072;
float linearize_depth(float d) {
    float z_n = 2.0 * d - 1.0;
    return 2.0 * zNear * zFar / (zFar + zNear - z_n * (zFar - zNear));
}

vec2 rand2(float i) {
    return (vec2(rand(TexCoords + i * 2), rand(TexCoords + 1 + i * 2)) - .5) * 2.;
}

void main() {
    float depth = texture(depthTex, TexCoords).r;
    vec3 position = depthToView(TexCoords - taaOffset * .5, depth, projection);

    float shadow = 0;
    float BIAS;
    int idx = 0;
    if(linearize_depth(depth) / 1.7 > cuts[1]) {
        idx = 2;
        BIAS = .000075;
    } else if(linearize_depth(depth) / 2 > cuts[0]) {
        idx = 1;
        BIAS = .00002;
    } else {
        idx = 0;
        BIAS = .000002;
    }
    

    vec4 ShadowCoord = vec4(position.xyz, 1.) * matrices[idx];
    ShadowCoord /= ShadowCoord.w;
    ShadowCoord.xyz = ShadowCoord.xyz * .5 + .5;

    shadow += (texture(shadowTex[idx], ShadowCoord.xy).r - ShadowCoord.z + BIAS) > 0 ? 1 : 0;

    FragColor = vec4(shadow, 0., 0., 1.);
}