#version 420

#define TAA_SEARCH_RADIUS 9
#define TAA_DEPTH_SEARCH_RADIUS 9

layout (location = 0) out vec3 FragColor;
layout (location = 1) out float MemoryDepth;
  
layout (location = 0) in vec2 TexCoords;

layout (binding = 0) uniform sampler2D depthTex;
layout (binding = 1) uniform sampler2D colorTex0;
layout (binding = 2) uniform sampler2D colorTex1;
layout (binding = 3) uniform sampler2D depthTex1;
// layout (binding = 3) uniform ExampleBlock{ 
//     int width; 
//     int height; 
//     mat4 projectionPrev; 
//     mat4 projection; 
//     vec2 taaOffset; 
// };

const vec2 offsets[9] = {{-1, 1}, {0, 1}, {1, 1}, {-1, 0}, {0, 0}, {1, 0}, {-1, -1}, {0, -1}, {1, -1}};
const float zNear = .5;
const float zFar = 3072;

uniform int width;
uniform int height;
uniform mat4 projectionPrev;
uniform mat4 projection;
uniform vec2 taaOffset;

vec3 depthToView(vec2 texCoord, float depth, mat4 projInv) {
    vec4 ndc = vec4(texCoord, depth, 1) * 2 - 1;
    vec4 viewPos = ndc * projInv;
    return viewPos.xyz / viewPos.w;
}

float linearize_depth(float d) {
    float z_n = 2.0 * d - 1.0;
    return 2.0 * zNear * zFar / (zFar + zNear - z_n * (zFar - zNear));
}

void main() {
    float depth = texture(depthTex, TexCoords).r;
    vec2 tc = TexCoords + taaOffset * .5;
    vec3 position = depthToView(TexCoords, depth, projection);
    float x = 1./width;
    float y = 1./height;
    vec4 prev = vec4(position, 1.) * projectionPrev;
    prev /= prev.w;
    prev = prev * 0.5 + 0.5;

    vec3 color;
    vec4 n = texture(colorTex1, prev.xy);

    vec3 curr = texture(colorTex0, tc).rgb;
    vec3 _min = curr;
    vec3 _max = curr;
    float minDepth = zFar * 2;
    float mixAmt = .1;

    for(int i = 0; i < TAA_SEARCH_RADIUS; i++) {
        vec3 _sample = texture(colorTex0, tc + offsets[i % 9] * vec2(x, y) * (1 + i / 9)).rgb;
        _min = min(_min, _sample);
        _max = max(_max, _sample);
    }

    for(int i = 0; i < TAA_DEPTH_SEARCH_RADIUS; i++) {
        float d = texture(depthTex1, prev.xy + offsets[i % 9] * vec2(x, y) * (1 + i / 9)).r;
        minDepth = min(abs(linearize_depth(d) - linearize_depth(prev.z)), minDepth);
    }

    if(prev.x < 0 || prev.x > 1 || prev.y < 0 || prev.y > 1 || minDepth > .05 * linearize_depth(depth)) {
        mixAmt = 1;
    }
    
    // FragColor = mix(n.rgb, curr, .1);
    FragColor = mix(clamp(n.rgb, _min, _max), curr, mixAmt);
    MemoryDepth = depth;
}