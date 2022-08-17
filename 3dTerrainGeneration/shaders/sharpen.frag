#version 430 core
out vec4 FragColor;
  
in vec2 TexCoords;

uniform sampler2D colorTex;
uniform float width;
uniform float height;

float rand(vec2 co) {
    return fract(sin(dot(co, vec2(12.9898, 78.233))) * 43758.5453);
}

void main() {
	float amount = .3;
	float neighbor = amount * -1;
	float center = amount * 4 + 1;

    vec2 s = vec2(width, height);

	vec3 color = texture(colorTex, TexCoords + vec2(0, 1) / s).rgb * neighbor
				+ texture(colorTex, TexCoords + vec2(-1, 0) / s).rgb * neighbor
				+ texture(colorTex, TexCoords + vec2(0, 0) / s).rgb * center
				+ texture(colorTex, TexCoords + vec2(1, 0) / s).rgb * neighbor
				+ texture(colorTex, TexCoords + vec2(0, -1) / s).rgb * neighbor;

    float f = .5/255.;
    color += mix(-f, f, rand(TexCoords));

    FragColor = vec4(color, 1.);
}