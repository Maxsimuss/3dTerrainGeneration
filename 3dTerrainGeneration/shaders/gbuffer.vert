#version 460

layout (location = 0) in vec4 aData;
layout (location = 1) in mat4 model;

uniform mat4 view;
uniform mat4 projection;
uniform vec3 rand;

out vec3 Normal;
out vec3 Color;
out float Emission;

void main()
{
    mat3 normalMatrix = inverse(mat3(model));

    vec3 pos = vec3(int(aData.x) & 0x000000FF, int(aData.x) >> 8 & 0x000000FF, int(aData.y) & 0x000000FF);
    Color = vec3(int(aData.y) >> 8 & 0x000000FF, int(aData.z) & 0x000000FF, int(aData.z) >> 8 & 0x000000FF) / 255.;
    Normal = normalize(vec3(int(aData.w) >> 8 & 0x00000001, int(aData.w) >> 9 & 0x00000001, int(aData.w) >> 10 & 0x00000001) * normalMatrix);
    Emission = (int(aData.w) & 0x000000FF) / 255.;

    vec4 clipPos = model * vec4(pos, 1.0) * view * projection;
    gl_Position = clipPos + vec4(rand.xy, 0, 0) * clipPos.w;
}