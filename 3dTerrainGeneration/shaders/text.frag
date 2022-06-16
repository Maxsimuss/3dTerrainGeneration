#version 420
out vec4 FragColor;

uniform sampler2D colortex0;
uniform vec4 color;
in vec2 TexCoords;

void main()
{
    FragColor = texture(colortex0, TexCoords) * color;
    // FragColor = vec4(1.);
}