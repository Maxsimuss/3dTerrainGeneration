#version 330 core

layout (location = 0) in vec2 pos;
layout (location = 1) in vec3 transform;

uniform mat4 view;
uniform mat4 projection;
uniform float aspect;

out vec2 TexCoords;

void main() {
    TexCoords = pos;
    vec3 position = transform;

    gl_Position = vec4(position, 1.0) * view * projection + vec4(pos * .4 * vec2(1, aspect), 0., 1.);
}