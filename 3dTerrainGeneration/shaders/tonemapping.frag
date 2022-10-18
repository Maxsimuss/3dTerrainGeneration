#version 430 core
out vec3 FragColor;
  
in vec2 TexCoords;

layout(std430, binding = 1) buffer outputData
{
    int maxLuma;
    float lumaSmooth;
};

uniform sampler2D colorTex;

void main() {
    vec3 color = texture(colorTex, TexCoords).rgb / lumaSmooth;

    FragColor = color;
}