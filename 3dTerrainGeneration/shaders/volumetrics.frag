#version 430 core
out float FragColor;
  
in vec2 TexCoords;


struct Light {
    vec3 position;
    vec3 color;
};


uniform sampler2D depthTex; //depth
uniform sampler2D normalTex; //normal
uniform sampler2D shadowMapTex;
uniform float shadowRes;
uniform float fogQuality;

uniform vec3 viewPos;
uniform vec2 pixel;

uniform mat4 projection;
uniform mat4 matrixFar;
uniform Light sun;

const float zNear = .2;
const float zFar = 4096;
float linearize_depth(float d)
{
    float z_n = 2.0 * d - 1.0;
    return 2.0 * zNear * zFar / (zFar + zNear - z_n * (zFar - zNear));
}

vec3 depthToView(vec2 texCoord, float depth, mat4 projInv) {
    vec4 ndc = vec4(texCoord, depth, 1) * 2 - 1;
    vec4 viewPos = ndc * projInv;
    return viewPos.xyz / viewPos.w;
}

vec4 get(vec3 position) {
    vec4 ShadowCoord = vec4(position.xyz, 1.) * matrixFar;
    ShadowCoord /= ShadowCoord.w;
    ShadowCoord.xyz = ShadowCoord.xyz * .5 + .5;
    return ShadowCoord;
}

void main() {
    float depth = texture(depthTex, TexCoords).r;
    vec3 position = depthToView(TexCoords, depth, projection);

    float fog = 0;
    float sunStr = max(dot(vec3(0, 1, 0), sun.position), 0.0);
    sunStr = pow(min(sunStr, .1) * 10, 10);
    for (int x = 0; x < fogQuality; x++) {
        vec3 _pos = viewPos * (1 - x/fogQuality) + position * (x/fogQuality);

        vec3 sh = get(_pos).xyz;
        fog += ((texture(shadowMapTex, sh.xy).r <= sh.z ? 0 : 1) * .9 + .1);
    }
    
    FragColor = fog / fogQuality;
}