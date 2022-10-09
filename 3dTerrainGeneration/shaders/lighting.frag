#version 430 core
out vec4 FragColor;
  
in vec2 TexCoords;

struct Light {
    vec3 position;
    vec3 color;
};

uniform sampler2D depthTex; //depth
uniform sampler2D colorTex; //albedo
uniform sampler2D normalTex; //normal
uniform sampler2D shadowTex; //shadow
uniform sampler2D skyTex; //sky
uniform sampler2D starTex; //stars
uniform sampler2D fogTex; //fog
uniform sampler2D occlusionTex; //fog
uniform sampler2D giTex; //gi
uniform sampler2D giNTex; //gi

uniform vec3 skyLight;
uniform vec3 viewPos;
uniform float renderDistance;
uniform int lightCount;
uniform int giW;
uniform int giH;

uniform mat4 projection;

uniform Light sun;

layout(std430, binding = 3) buffer fireballs {
    vec4 data[];
};

vec3 depthToView(vec2 texCoord, float depth, mat4 projInv) {
    vec4 ndc = vec4(texCoord, depth, 1) * 2 - 1;
    vec4 viewPos = ndc * projInv;
    return viewPos.xyz / viewPos.w;
}

const float zNear = .2;
const float zFar = 3072;
float linearize_depth(float d)
{
    float z_n = 2.0 * d - 1.0;
    return 2.0 * zNear * zFar / (zFar + zNear - z_n * (zFar - zNear));
}

void main() {
    float depth = texture(depthTex, TexCoords).r;
    vec3 position = depthToView(TexCoords, depth, projection);
    float dist = distance(position, viewPos);
    vec4 albedo = texture(colorTex, TexCoords);
    vec4 normal = texture(normalTex, TexCoords) * vec4(2, 2, 2, 1) - vec4(1, 1, 1, 0);
    vec3 sky = texture(skyTex, TexCoords).rgb * 2;

    // iterate over the sample kernel and calculate occlusion factor
    float occlusion = texture(occlusionTex, TexCoords).r;

    float shadow = clamp(texture(shadowTex, TexCoords).r, 0., 1.);
    vec3 sunLight = max(dot(normal.rgb, sun.position), 0.0) * shadow * sun.color;

    // vec3 diffuse = ambient * skyLight * clamp(occlusion, 0., .5) / 2. * albedo.rgb + sunLight * albedo.rgb * sh * .8;
    vec3 gi = vec3(0);
    float s = 1000;
    vec3 nr = texture(normalTex, TexCoords).rgb;
    vec2 giR = vec2(giW - 1, giH - 1);
    for(int x = -3; x <= 3; x++) {
        for(int y = -3; y <= 3; y++) {
            vec2 offset = vec2(float(x) / giW, float(y) / giH);
            vec4 xd = texture(giNTex, TexCoords + offset);
            float l = length(xd.rgb - position);
            if(l < s && abs(xd.a - ((normal.x + 1) + (normal.y + 1) * 3 + (normal.z + 1) * 9)) < .001) {
                gi = texture(giTex, TexCoords + offset).rgb;
                s = l;
            }
        }
    }

    vec3 diffuse = (sunLight + gi * occlusion) * albedo.rgb;

    for (int i = 0; i < lightCount; i++) {
        vec3 lightDir = normalize(data[i].xyz - position);

        vec3 lightColor = pow(max(1 - length(data[i].xyz - position) / data[i].w, 0.), 4) * vec3(1.2, .3, .1) * 4;
        diffuse += (max(dot(normal.rgb, lightDir), 0.0) + normal.a / 4.) * (clamp(occlusion, 0., .5) / 2. + .5) * lightColor * occlusion;
    }

    
    float len = linearize_depth(depth) / 500;
    float fogAmt = len / (len + 1) * texture(fogTex, TexCoords).r;

    vec3 fog = 20 * sky;

    vec3 color = mix(texture(skyTex, TexCoords).rgb * 40 + texture(starTex, TexCoords).rgb * 30, mix(diffuse, fog, fogAmt), albedo.aaa);
    FragColor = vec4(color / 2, 1);
}