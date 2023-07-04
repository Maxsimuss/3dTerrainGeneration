#version 430

uniform sampler2D depthTex;

in vec2 TexCoords;
out float weight;

void main() {
	weight = abs(texture(depthTex, vec2(.5)).r - texture(depthTex, TexCoords).r);
}