#version 460

layout (location = 0) in uint aData;
layout (location = 1) in mat4 model;

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

    vec3 pos = vec3(aData >> 25 & (0x0000007F), aData >> 18 & (0x0000007F), aData >> 11 & (0x0000007F));

    uint face = aData >> 8 & 0x00000007;
    Color = (vec3(((aData >> 5) & uint(7)) * 36, ((aData >> 2) & uint(7)) * 36, (aData & uint(3)) * 85) + 10) / 265.;
    Normal = normalize(NORMALS[face] * normalMatrix) / 2 + .5;

    vec4 clipPos = model * vec4(pos, 1.0) * view * projection;
    gl_Position = clipPos + vec4(taaOffset * clipPos.w, 0, 0);
}