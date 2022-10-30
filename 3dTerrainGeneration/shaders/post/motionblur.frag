#version 420 core
out vec4 FragColor;
  
in vec2 TexCoords;

uniform sampler2D colortex0;
uniform sampler2D colortex1;

uniform mat4 projectionPrev;
uniform mat4 projection;

#define MOTIONBLUR_QUALITY 20
#define MOTIONBLUR_LENGTH .2

float rand(vec2 co){
    return fract(sin(dot(co, vec2(12.9898, 78.233))) * 43758.5453);
}


vec3 depthToView(vec2 texCoord, float depth, mat4 projInv) {
    vec4 ndc = vec4(texCoord, depth, 1) * 2 - 1;
    vec4 viewPos = ndc * projInv;
    return viewPos.xyz / viewPos.w;
}

void main() {
    float depth = texture(colortex1, TexCoords).r;
    vec3 position = depthToView(TexCoords, depth, projection);
    vec4 prev = vec4(position, 1.) * projectionPrev;
    prev /= prev.w;
    prev = prev * 0.5 + 0.5;

    vec2 delta = TexCoords - prev.xy;

    vec3 color = vec3(0);
    int samples = 0;
    for(int i = 0; i <= 2; i++) {
        vec2 d = delta * rand(TexCoords + i);
        vec2 t = TexCoords + d;
        if(t.x < 0 || t.x > 1 || t.y < 0 || t.y > 1 || length(d) > .3) {
            continue;
        }
        color += texture(colortex0, t).rgb;
        samples++;
    }
    
    if(samples < 1) {
        color = texture(colortex0, TexCoords).rgb;
        samples++;
    }

    FragColor = vec4(color / samples, 1.0);
}