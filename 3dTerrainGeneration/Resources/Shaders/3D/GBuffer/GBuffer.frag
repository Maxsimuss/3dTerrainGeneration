#version 420

out vec4 Albedo_out;
out vec3 Normal_out;

in vec3 Normal;
in vec3 Color;

void main()
{
    Albedo_out = vec4(Color, 1.);
    Normal_out = vec3(Normal);
}