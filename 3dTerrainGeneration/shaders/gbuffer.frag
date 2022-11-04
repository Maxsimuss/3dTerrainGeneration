#version 420

out vec4 Albedo_out;
out vec3 Normal_out;
out vec4 Position_out;

in vec3 Normal;
in vec3 Color;
in vec4 Position;

void main()
{
    Albedo_out = vec4(Color, 1.);
    Normal_out = vec3(Normal);
    Position_out = Position;
}