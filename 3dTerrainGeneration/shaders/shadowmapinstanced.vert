#version 420

layout (location = 0) in vec4 aData;
layout (location = 1) in mat4 model;

uniform mat4 lightSpaceMatrix;


void main()
{
    vec3 pos = vec3(int(aData.x) & 0x000000FF, int(aData.x) >> 8 & 0x000000FF, int(aData.y) & 0x000000FF);
    gl_Position = vec4(pos, 1.0) * model * lightSpaceMatrix;
}  