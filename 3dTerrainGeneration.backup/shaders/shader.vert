#version 330 core

precision highp float;
precision highp int;

in vec3 aPos;
in vec3 aNormal;
in vec3 aColor;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

varying out vec3 Normal;
varying out vec3 FragPos;
varying out vec3 Color;

void main()
{
    gl_Position = vec4(aPos, 1.0) * model * view * projection;
    FragPos = vec3(vec4(aPos, 1.0) * model);
    Normal = aNormal * mat3(transpose(model));
    Color = aColor;
}