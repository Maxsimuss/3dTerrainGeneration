#version 460

layout (location = 0) in vec4 aData;
layout (location = 1) in mat4 model;

uniform mat4 lightSpaceMatrix;
uniform mat4 view;
uniform mat4 projection;
uniform vec3 rand;

void main()
{
    vec3 pos = vec3(int(aData.x) & 0x000000FF, int(aData.x) >> 8 & 0x000000FF, int(aData.y) & 0x000000FF);
    vec4 clipPos = model * vec4(pos, 1.0) * view * projection;
    vec4 xd = model * vec4(pos, 1.0) * lightSpaceMatrix;
    gl_Position = xd ;
}  