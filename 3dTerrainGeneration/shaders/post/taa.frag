#version 420
out vec4 FragColor;
  
in vec2 TexCoords;

uniform sampler2D depthTex;
uniform sampler2D colorTex0;
uniform sampler2D colorTex1;
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

vec2 offsets[9] = {{-1, 1}, {0, 1}, {1, 1}, {-1, 0}, {0, 0}, {1, 0}, {-1, -1}, {0, -1}, {1, -1}};

const float zNear = .2;
const float zFar = 3072;
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
#define TAA_SEARCH_RADIUS 9
#define TAA_DEPTH_SEARCH_RADIUS 9

    for(int i = 0; i < TAA_SEARCH_RADIUS; i++) {
        vec3 _sample = texture(colorTex0, tc + offsets[i % 9] * vec2(x, y) * (1 + i / 9)).rgb;
        _min = min(_min, _sample);
        _max = max(_max, _sample);
    }

    for(int i = 0; i < TAA_DEPTH_SEARCH_RADIUS; i++) {
        float d = texture(colorTex1, prev.xy + offsets[i % 9] * vec2(x, y) * (1 + i / 9)).a;
        minDepth = min(abs(linearize_depth(d) - linearize_depth(prev.z)), minDepth);
    }

    if(prev.x < 0 || prev.x > 1 || prev.y < 0 || prev.y > 1 || minDepth > .05 * linearize_depth(depth)) {
        mixAmt = 1;
    }
    // color = mix(n.rgb, curr, mixAmt);
    color = mix(clamp(n.rgb, _min, _max), curr, mixAmt);
    FragColor = vec4(color, depth);
}