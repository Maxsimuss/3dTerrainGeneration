#version 450
out vec4 FragColor;
  
in vec2 TexCoords;
// precision highp float;

uniform sampler2D depthTex;
uniform sampler2D colorTex0;
uniform sampler2D colorTex1;
uniform int width;
uniform int height;
uniform mat4 projectionPrev;
uniform mat4 projection;
uniform vec3 rand;

const float zNear = .2;
const float zFar = 4096;
float linearize_depth(float d) {
    float z_n = 2.0 * d - 1.0;
    return 2.0 * zNear * zFar / (zFar + zNear - z_n * (zFar - zNear));
}

vec3 depthToView(vec2 texCoord, float depth, mat4 projInv) {
    vec4 ndc = vec4(texCoord, depth, 1) * 2 - 1;
    vec4 viewPos = ndc * projInv;
    return viewPos.xyz / viewPos.w;
}

vec2 offsets[9] = {{-1, 1}, {0, 1}, {1, 1}, {-1, 0}, {0, 0}, {1, 0}, {-1, -1}, {0, -1}, {1, -1}};

void main() {
    vec2 tc = TexCoords + rand.xy * .5;
    float depth = texture(depthTex, TexCoords).r;
    vec3 position = depthToView(TexCoords, depth, projection);

    vec4 prev = vec4(position, 1.) * projectionPrev;
    prev /= prev.w;
    prev = prev * 0.5 + 0.5;

    vec3 color;
    vec4 n = texture(colorTex1, prev.xy);
    float x = 1./width;
    float y = 1./height;

    vec3 curr = texture(colorTex0, tc).rgb;
    vec3 _min = curr;
    vec3 _max = curr;
    for(int i = 0; i < 9; i++) {
        vec3 _sample = texture(colorTex0, tc + offsets[i] * vec2(x, y) / 2.).rgb; 
        _min = min(_min, _sample);
        _max = max(_max, _sample);
    }


    if(prev.x < 0 || prev.x > 1 || prev.y < 0 || prev.y > 1) {
        color = curr;
    } else {
        color = mix(clamp(n.rgb, _min, _max), curr, .01);
    }

    FragColor = vec4(color, 1);
}