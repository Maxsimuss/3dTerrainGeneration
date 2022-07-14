#version 460

in vec4 aData;

uniform mat4 lightSpaceMatrix;
uniform mat4 view;
uniform mat4 projection;
uniform vec3 rand;
layout(std430, binding = 0) buffer matrices {
    mat4 transforms[];
};
void main()
{
    mat4 model = transforms[gl_DrawID];


    vec3 pos = vec3(int(aData.x) & 0x000000FF, int(aData.x) >> 8 & 0x000000FF, int(aData.y) & 0x000000FF);
    vec4 clipPos = model * vec4(pos, 1.0) * view * projection;
    vec4 xd = model * vec4(pos, 1.0) * lightSpaceMatrix;
    gl_Position = xd ;
}  