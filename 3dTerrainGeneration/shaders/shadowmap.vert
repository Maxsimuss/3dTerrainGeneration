#version 460

layout (location = 0) in vec3 pos;
layout (location = 1) in vec3 color;
layout (location = 2) in vec2 other;
layout (location = 3) in mat4 model;

uniform mat4 lightSpaceMatrix;
uniform mat4 view;
uniform mat4 projection;
uniform vec2 taaOffset;

void main()
{
    vec4 clipPos = model * vec4(pos, 1.0) * view * projection;
    // clipPos = clipPos + vec4(taaOffset, 0, 0) * clipPos.w;

    vec4 xd = clipPos * inverse(projection) * inverse(view) * lightSpaceMatrix;
    gl_Position = xd;
}  