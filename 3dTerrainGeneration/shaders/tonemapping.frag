#version 430 core
out vec3 FragColor;
  
in vec2 TexCoords;


uniform sampler2D colorTex;
layout(std430, binding = 1) buffer outputData
{
    int maxLuma;
};
void main() {
    vec3 color = texture(colorTex, TexCoords).rgb / (maxLuma / 512.);
    
    FragColor = color;
}