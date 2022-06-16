#version 330 core
out vec4 FragColor[];
  
in vec2 TexCoords;

uniform sampler2D colortex0;
uniform sampler2D colortex1;
uniform sampler2D colortex2;
uniform sampler2D colortex4;

uniform float zNear;
uniform float zFar;
uniform vec3 fogColor;

float linearize_depth(float d)
{
    return zNear * zFar / (zFar + d * (zNear - zFar));
}

void main() {
    float d = texture(colortex0, TexCoords).r;
    float ld = linearize_depth(d);
    float rad = 0.05 / ld;

    vec3 ambient = texture(colortex1, TexCoords).rgb;
    vec3 lighting = texture(colortex2, TexCoords).rgb;
    float depth = ld * 4
        - linearize_depth(texture(colortex0, TexCoords + vec2(rad, rad)).r)
        - linearize_depth(texture(colortex0, TexCoords + vec2(rad, -rad)).r)
        - linearize_depth(texture(colortex0, TexCoords + vec2(-rad, rad)).r)
        - linearize_depth(texture(colortex0, TexCoords + vec2(-rad, -rad)).r);

    float occ = 1. -clamp(depth, 0., .5) / 5.;
    vec3 color = ambient + lighting * occ;
    float fogAmt = 1. - texture(colortex4, TexCoords).r;
    
    vec3 fog = mix(fogColor, pow(fogColor * 1.1, vec3(1.4)), TexCoords.y);
    FragColor[0] = vec4(mix(color, fogColor, clamp(fogAmt, 0, 1)), 1.);
    FragColor[1] = vec4(occ);
}