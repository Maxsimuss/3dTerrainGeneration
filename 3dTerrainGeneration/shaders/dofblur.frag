#version 430

uniform sampler2D weightTex;
uniform sampler2D colorTex;
uniform float aspectRatio;

in vec2 TexCoords;
out vec4 color;

vec2 offsets[] = {
	vec2(0, 1), 
	vec2(0.8660254037844386, 0.5000000000000001), 
	vec2(0.8660254037844387, -0.4999999999999998),
	vec2(1.2246467991473532e-16, -1),
	vec2(-0.8660254037844385, -0.5000000000000004),
	vec2(-0.8660254037844386, 0.5000000000000001),
};

#define RADIUS .01
#define R vec2(RADIUS, RADIUS * aspectRatio)

void main() {
	float weight = texture(weightTex, TexCoords).r;

	color += texture(colorTex, TexCoords);
	for (int i = 0; i < 6; i++) {
		color += texture(colorTex, TexCoords + offsets[i] * R * weight);
	}

	color /= 7;
}