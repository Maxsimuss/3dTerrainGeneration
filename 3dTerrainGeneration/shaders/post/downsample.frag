#version 330 core
out vec4 FragColor;
  
in vec2 TexCoords;

uniform sampler2D colortex0;

uniform float tw;
uniform float th;
uniform float radius;

void main() {
    vec2 halfpixel = 0.5 * vec2(tw, th);

    vec4 sum = texture(colortex0, TexCoords) * 4.0;
    sum += texture(colortex0, TexCoords - halfpixel.xy * radius);
    sum += texture(colortex0, TexCoords + halfpixel.xy * radius);
    sum += texture(colortex0, TexCoords + vec2(halfpixel.x, -halfpixel.y) * radius);
    sum += texture(colortex0, TexCoords - vec2(halfpixel.x, -halfpixel.y) * radius);

    FragColor = sum / 8.0;
}