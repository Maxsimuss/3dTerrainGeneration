#version 460

layout (location = 0) in vec3 pos;
layout (location = 1) in vec3 color;
layout (location = 2) in vec2 other;
layout (location = 3) in mat4 model;

uniform mat4 view;
uniform mat4 projection;
uniform vec2 taaOffset;

out vec3 Normal;
out vec3 Color;

vec3 NORMALS[6] = vec3[] (
    vec3(1., 0., 0.),
    vec3(0., 1., 0.),
    vec3(0., 0., 1.),
    vec3(-1., 0., 0.),
    vec3(0., -1., 0.),
    vec3(0., 0., -1.)
);

void main()
{
    mat3 normalMatrix = inverse(mat3(model));

    Color = color;
    Normal = normalize(NORMALS[uint(other.x)] * normalMatrix) / 2 + .5;

    vec4 clipPos = model * vec4(pos, 1.0) * view * projection;
    gl_Position = clipPos + vec4(taaOffset * clipPos.w, 0, 0);
}