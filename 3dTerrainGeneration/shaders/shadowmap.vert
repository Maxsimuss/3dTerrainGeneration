#version 460

layout (location = 0) in vec4 aData;
layout (location = 1) in mat4 model;

uniform mat4 lightSpaceMatrix;
uniform mat4 view;
uniform mat4 projection;
uniform vec2 taaOffset;

void main()
{
    vec3 pos = vec3(int(aData.x) & 0x000000FF, int(aData.x) >> 8 & 0x000000FF, int(aData.y) & 0x000000FF);
    vec4 clipPos = model * vec4(pos, 1.0) * view * projection;
    clipPos = clipPos + vec4(taaOffset, 0, 0) * clipPos.w;

    vec4 xd = clipPos * inverse(projection) * inverse(view) * lightSpaceMatrix;
    gl_Position = xd;
}  