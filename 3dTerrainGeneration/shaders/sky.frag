#version 430 core
out vec4 FragColor;
  
in vec2 TexCoords;

uniform mat4 projection;
uniform mat4 viewMatrix;
uniform vec3 sun_dir;
uniform vec3 ;

uniform float time;

#define PI 3.14159265359

const vec3 coef = vec3(1.6, 2.2, 2.8);

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

vec3 depthToView() {
    vec4 ndc = vec4(TexCoords, 1, 1) * 2 - 1;
    vec4 viewPos = viewMatrix * (ndc * projection);
    return viewPos.xyz;
}

void main() {
    vec3 p = depthToView();

    vec3 c = vec3(.055, .130, .224);
    
    vec3 col = pow(vec3(pow(smoothstep(0., 1., sun_dir.y / 2 + .5), 24.) * 80.), c);

    // Output to screen
    FragColor = vec4(col / 10. + c / 20.,1.0);
}