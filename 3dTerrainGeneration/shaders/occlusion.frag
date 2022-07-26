#version 420

out float occlusion;
in vec2 TexCoords;

uniform sampler2D depthTex;
uniform sampler2D normalTex;
uniform float time;

uniform mat4 projection;
uniform mat4 projectionPrev;
uniform mat4 _projection;

const float radius = 2.5;
const float zNear = .2;
const float zFar = 4096;

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

    for(int i = 0; i < 8; ++i) {
        vec3 randomVec = normalize(vec3(rand(TexCoords + i) * 2 - 1, rand(TexCoords + i + 1.) * 2 - 1, rand(TexCoords + i + 2) * 2 - 1));
        vec3 tangent   = normalize(randomVec - normal.xyz * dot(randomVec, normal.xyz));
        vec3 bitangent = cross(normal.xyz, tangent);
        mat3 TBN       = mat3(tangent, bitangent, normal.xyz);  
        // get sample position
        vec3 samplePos = TBN * normalize(vec3(rand(TexCoords + i - 1.), rand(TexCoords + 1 + i), rand(TexCoords + 2 + i))); // from tangent to view-space
        samplePos = position + radius * samplePos * rand(TexCoords + i / 32. + 3.); 
        
        // project sample position (to sample texture) (to get position on screen/texture)
        vec4 offset = vec4(samplePos, 1.0) * _projection;
        offset.xyz /= offset.w; // perspective divide
        offset.xyz = offset.xyz * 0.5 + 0.5; // transform to range 0.0 - 1.0
        
        float sampleDepth = linearize_depth(texture(depthTex, offset.xy).r);
        float rangeCheck = smoothstep(0.0, 1.0, radius / abs(linDepth - sampleDepth));
        occlusion += (sampleDepth >= linearize_depth(offset.z) ? 0.0 : 1.0) * rangeCheck;  
    }
    vec4 prev = vec4(position, 1.) * projectionPrev;
    prev /= prev.w;
    prev = prev * 0.5 + 0.5;

    // if(prev.x < 0 || prev.x > 1 || prev.y < 0 || prev.y > 1) {
    //     occlusion = (1 - occlusion / 4.);
    // } else {
    //     occlusion = ((1 - occlusion / 4.) + texture(occlusionTex, prev.xy).r * 3.) / 4.;
    // }
    occlusion = (1 - occlusion / 8.);
    occlusion = 1;
}