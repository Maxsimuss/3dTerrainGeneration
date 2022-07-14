#version 330 core
out vec4 FragColor;
  
in vec2 TexCoords;

uniform sampler2D colortex0;

const vec3 W = vec3(0.2125, 0.7154, 0.0721);
float luminance(vec3 rgb) {
    return dot(rgb, W);
}

void main() {
    vec3 color = texture(colortex0, TexCoords).rgb;

	FragColor = vec4(color * max(luminance(color) - 1, 0), 1.0);
}