#version 330 core
out vec4 FragColor;
  
in vec2 TexCoords;

uniform sampler2D colortex0;

float czm_luminance(vec3 rgb)
{
    // Algorithm from Chapter 10 of Graphics Shaders.
    const vec3 W = vec3(0.2125, 0.7154, 0.0721);
    return dot(rgb, W);
}

void main() {
    vec3 color = texture(colortex0, TexCoords).rgb;

	FragColor = vec4(color * max(czm_luminance(color) - 1, 0), 1.0);
}