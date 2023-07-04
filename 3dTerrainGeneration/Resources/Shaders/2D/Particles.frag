#version 330 core

in vec2 TexCoords;
out vec4 Color;

void main() {
    float a = clamp(1 - length(TexCoords), 0., 1.);
    a = a * a * a;
    a = clamp(a / 2 + smoothstep(0., .001, a - .7), 0., 1.);

    Color = vec4(1, 0, 0, a);
}