#version 420
// #pragma optionNV (unroll all)
#define SSAO_SAMPLES 1

out float occlusion;
in vec2 TexCoords;

uniform sampler2D depthTex;
uniform sampler2D normalTex;
uniform float time;

uniform mat4 projection;
uniform mat4 projectionPrev;
uniform mat4 _projection;

const float radius = 2.;
const float zNear = .5;
const float zFar = 3072;

float rand(vec2 co) {
    return fract(sin(dot(co + time, vec2(12.9898, 78.233))) * 43758.5453);
}

float linearize_depth(float d) {
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
    float linDepth = linearize_depth(depth);

    vec3 position = depthToView(TexCoords, depth, projection);
    vec3 normal = texture(normalTex, TexCoords).xyz * 2 - 1;
    for(int i = 0; i < SSAO_SAMPLES; ++i) {
        vec3 randomVec = (vec3(rand(TexCoords + i * 3) * 2 - 1, rand(TexCoords + i * 3 + 1.) * 2 - 1, rand(TexCoords + i * 3 + 2) * 2 - 1));
        vec3 tangent   = (randomVec - normal.xyz * dot(randomVec, normal.xyz));
        vec3 bitangent = cross(normal.xyz, tangent);
        mat3 TBN       = mat3(tangent, bitangent, normal.xyz);  
        // get sample position
        vec3 samplePos = TBN * (vec3(rand(TexCoords + i * 3 - 30), rand(TexCoords + i * 3 - 20), rand(TexCoords + i * 3 - 10))); // from tangent to view-space
        samplePos = position + radius * samplePos * rand(TexCoords + i / 32. + 3.); 
        
        // project sample position (to sample texture) (to get position on screen/texture)
        vec4 offset = vec4(samplePos, 1.0) * _projection;
        offset.xyz /= offset.w; // perspective divide
        offset.xyz = offset.xyz * 0.5 + 0.5; // transform to range 0.0 - 1.0
        
        float sampleDepth = linearize_depth(texture(depthTex, offset.xy).r);
        float rangeCheck = smoothstep(0.0, 1.0, radius / abs(linDepth - sampleDepth));
        occlusion += (sampleDepth >= linearize_depth(offset.z) ? 0.0 : 1.0) * rangeCheck;
    }

    occlusion = (1 - occlusion / SSAO_SAMPLES);
}