#version 430

const float GoldenAngle = 2.39996323;
const float Iterations = 6.0;

const mat2 Rotation = mat2(
    cos(GoldenAngle),
    sin(GoldenAngle),
   -sin(GoldenAngle),
    cos(GoldenAngle)
);

uniform sampler2D weightTex;
uniform sampler2D colorTex;
uniform sampler2D depthTex;
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

float blurRadius(
    float A, // aperture
    float f, // focal length
    float S1, // focal distance
    float far, // far clipping plane
    float maxCoc, // mac coc diameter
    
	vec2 uv,
	sampler2D depthMap)
{
    vec4 currentPixel = texture(depthMap, uv);
    
    float S2 = currentPixel.r * far;
    
    //https://en.wikipedia.org/wiki/Circle_of_confusion
    float coc = A * ( abs(S2 - S1) / S2 ) * ( f / (S1 - f) );
    
    float sensorHeight = 0.024; // 24mm
    
    float percentOfSensor = coc / sensorHeight;
    
    // blur factor
    return clamp(percentOfSensor, 0.0, maxCoc);
}

void main() {
	// float compDepth = texture(depthTex, vec2(.5)).r;

    // float rec = 1.0; // reciprocal 
    
    // float radius = abs(compDepth - texture(depthTex, TexCoords).r) / 20;
    // vec2 horizontalAngle = vec2(0.0, radius / sqrt(Iterations));
    
    // vec2 aspect = vec2(1.0, aspectRatio);
    
	// int samples = 1;
	// for (float i; i < Iterations; i++) {
    //     rec += 1.0 / rec;
        
	//     horizontalAngle = horizontalAngle * Rotation;
        
    //     vec2 offset = (rec - 1.0) * horizontalAngle;
        
    //     float depth = texture(depthTex, TexCoords + aspect * offset).r;
	// 	// if(abs(depth - compDepth) / 10 > length(offset)) {
	// 		color += texture(colorTex, TexCoords + aspect * offset);
	// 		samples++;
	// 	// }
	// }
	// color += texture(colorTex, TexCoords);
	// color /= samples;
    color = texture(colorTex, TexCoords);
}