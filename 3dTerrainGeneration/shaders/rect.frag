#version 330 core

out vec4 FragColor;

in vec4 Color;
in vec2 TexCoords;

void main()
{
    FragColor = Color;
}