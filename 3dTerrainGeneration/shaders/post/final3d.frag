#version 330 core
out vec4 FragColor;
  
in vec2 TexCoords;

uniform sampler2D colortex0;
uniform sampler2D colortex1;
uniform float aspect;
uniform float time;

vec3 rgb2hsv(vec3 c) {
	vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
	vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
	vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));

	float d = q.x - min(q.w, q.y);
	float e = 1.0e-10;
	return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}
vec3 hsv2rgb(vec3 c) {
	vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
	vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
	return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

float rand(vec2 co){
    return fract(sin(dot(co, vec2(12.9898, 78.233))) * 43758.5453);
}

vec3 tonemapFilmic(const vec3 color) {
	vec3 x = max(vec3(0.0), color - 0.004);
	return (x * (6.2 * x + 0.5)) / (x * (6.2 * x + 1.7) + 0.06);
}

const float gamma = 2.2;
vec3 lumaBasedReinhardToneMapping(vec3 color)
{
	float luma = dot(color, vec3(0.2126, 0.7152, 0.0722));
	float toneMappedLuma = luma / (1. + luma);
	color *= toneMappedLuma / luma;
	color = pow(color, vec3(1. / gamma));
	return color;
}

const vec3 W = vec3(0.2125, 0.7154, 0.0721);
float luminance(vec3 rgb) {
    return dot(rgb, W);
}

void main() {
	vec3 color = clamp(texture(colortex0, TexCoords).rgb, 0., 1.) + texture(colortex1, TexCoords).rgb / 2.;
	// color = sqrt(color);
	// color /= color + 1;
	// color *= 1.5;

	color = tonemapFilmic(pow(color, vec3(1/.7))) / .85;

    FragColor = vec4(color, 1.0);
}