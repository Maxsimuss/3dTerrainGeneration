#version 420

out vec4 FragColor[];

in vec3 Normal;
in vec3 Color;
in float Emission;

void main()
{
    FragColor[0] = vec4(Color, 1.);
    FragColor[1] = vec4(Normal, Emission);
}