#version 430 core
out vec4 FragColor;
  
in vec2 TexCoords;

uniform mat4 projection;
uniform mat4 viewMatrix;
uniform vec3 sun_dir;

uniform float time;

#define PI 3.14159265359

const vec3 coef = vec3(1.6, 2.2, 2.8);

float hash(float n) { return fract(sin(n) * 1e4); }
float hash(vec2 p) { return fract(1e4 * sin(17.0 * p.x + p.y * 0.1) * (0.1 + abs(sin(p.y * 13.0 + p.x)))); }

// This one has non-ideal tiling properties that I'm still tuning
float noise(vec3 x) {
	const vec3 step = vec3(110, 241, 171);

	vec3 i = floor(x);
	vec3 f = fract(x);
 
	// For performance, compute the base input to a 1D hash from the integer part of the argument and the 
	// incremental change to the 1D based on the 3D -> 1D wrapping
    float n = dot(i, step);

	vec3 u = f * f * (3.0 - 2.0 * f);
	return mix(mix(mix( hash(n + dot(step, vec3(0, 0, 0))), hash(n + dot(step, vec3(1, 0, 0))), u.x),
                   mix( hash(n + dot(step, vec3(0, 1, 0))), hash(n + dot(step, vec3(1, 1, 0))), u.x), u.y),
               mix(mix( hash(n + dot(step, vec3(0, 0, 1))), hash(n + dot(step, vec3(1, 0, 1))), u.x),
                   mix( hash(n + dot(step, vec3(0, 1, 1))), hash(n + dot(step, vec3(1, 1, 1))), u.x), u.y), u.z);
}

vec3 depthToView() {
    vec4 ndc = vec4(TexCoords, 1, 1) * 2 - 1;
    vec4 viewPos = viewMatrix * (ndc * projection);
    return viewPos.xyz;
}

void main() {
	vec3 p = normalize(depthToView());

	float s = clamp(dot(p, sun_dir), 0, 1);

	vec3 sun = clamp(pow(s, 120).rrr * 30 - 25, 0, 5) / coef;
	vec3 v = p * 200 + time;
	vec3 stars = clamp(pow(clamp(noise(v), 0., 1.), 50.), 0, 1).rrr;

	FragColor = vec4(max(sun, stars) / 2., 1);
}