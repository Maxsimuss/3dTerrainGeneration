#version 430 core
out vec3 FragColor;
  
in vec2 TexCoords;


uniform sampler2D colorTex;
uniform float maxLuma;

void main() {
    vec3 color = texture(colorTex, TexCoords).rgb / (maxLuma);
    
    FragColor = color;
}