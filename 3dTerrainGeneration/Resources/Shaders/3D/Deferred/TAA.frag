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


// https://gist.github.com/TheRealMJP/c83b8c0f46b63f3a88a5986f4fa982b1
vec4 SampleTextureCatmullRom(sampler2D tex, vec2 uv)
{
    // We're going to sample a a 4x4 grid of texels surrounding the target UV coordinate. We'll do this by rounding
    // down the sample location to get the exact center of our "starting" texel. The starting texel will be at
    // location [1, 1] in the grid, where [0, 0] is the top left corner.
    vec2 texSize = textureSize(tex, 0);
    vec2 samplePos = uv * texSize;
    vec2 texPos1 = floor(samplePos - 0.5f) + 0.5f;

    // Compute the fractional offset from our starting texel to our original sample location, which we'll
    // feed into the Catmull-Rom spline function to get our filter weights.
    vec2 f = samplePos - texPos1;

    // Compute the Catmull-Rom weights using the fractional offset that we calculated earlier.
    // These equations are pre-expanded based on our knowledge of where the texels will be located,
    // which lets us avoid having to evaluate a piece-wise function.
    vec2 w0 = f * (-0.5f + f * (1.0f - 0.5f * f));
    vec2 w1 = 1.0f + f * f * (-2.5f + 1.5f * f);
    vec2 w2 = f * (0.5f + f * (2.0f - 1.5f * f));
    vec2 w3 = f * f * (-0.5f + 0.5f * f);

    // Work out weighting factors and sampling offsets that will let us use bilinear filtering to
    // simultaneously evaluate the middle 2 samples from the 4x4 grid.
    vec2 w12 = w1 + w2;
    vec2 offset12 = w2 / (w1 + w2);

    // Compute the final UV coordinates we'll use for sampling the texture
    vec2 texPos0 = texPos1 - 1;
    vec2 texPos3 = texPos1 + 2;
    vec2 texPos12 = texPos1 + offset12;

    texPos0 /= texSize;
    texPos3 /= texSize;
    texPos12 /= texSize;

    vec4 result = vec4(0);
    result += texture(tex, vec2(texPos0.x, texPos0.y)) * w0.x * w0.y;
    result += texture(tex, vec2(texPos12.x, texPos0.y)) * w12.x * w0.y;
    result += texture(tex, vec2(texPos3.x, texPos0.y)) * w3.x * w0.y;

    result += texture(tex, vec2(texPos0.x, texPos12.y)) * w0.x * w12.y;
    result += texture(tex, vec2(texPos12.x, texPos12.y)) * w12.x * w12.y;
    result += texture(tex, vec2(texPos3.x, texPos12.y)) * w3.x * w12.y;

    result += texture(tex, vec2(texPos0.x, texPos3.y)) * w0.x * w3.y;
    result += texture(tex, vec2(texPos12.x, texPos3.y)) * w12.x * w3.y;
    result += texture(tex, vec2(texPos3.x, texPos3.y)) * w3.x * w3.y;

    return result;
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
    vec4 n = SampleTextureCatmullRom(colorTex1, prev.xy);

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
    
    // FragColor = vec3(mixAmt);
    FragColor = mix(clamp(n.rgb, _min, _max), curr, mixAmt);
    // FragColor = mix(n.rgb, curr, mixAmt);
    // FragColor = curr;
    MemoryDepth = depth;
}