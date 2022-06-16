#version 420

in vec4 aData;

uniform mat4 lightSpaceMatrix;
uniform mat4 model;

void main()
{
    vec3 pos = vec3(int(aData.x) & 0x000000FF, int(aData.x) >> 8 & 0x000000FF, int(aData.y) & 0x000000FF);
    gl_Position = vec4(pos, 1.0) * model * lightSpaceMatrix;
}  