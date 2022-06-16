#version 420
out vec4 FragColor;

uniform sampler2D colortex0;
uniform sampler2D colortex1;
in vec2 TexCoords;

void main()
{
    FragColor = texture(colortex0, TexCoords) * 2 + texture(colortex1, TexCoords);
    // FragColor = vec4(1.);
}