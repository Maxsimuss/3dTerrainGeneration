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
uniform sampler2DShadow shadowMapTex;
uniform float shadowRes;

uniform vec3 skyLight;
uniform vec3 viewPos;
uniform float renderDistance;
uniform float time;
uniform float timeL;
uniform int lightCount;

uniform mat4 projection;
uniform mat4 _projection;
uniform mat4 matrixFar;

uniform Light sun;

const float radius = .75;
const float bias = -0.2;

layout(std430, binding = 3) buffer layoutName
{
    vec4 data[];
};

float rand(vec2 co){
    return fract(sin(dot(co + time, vec2(12.9898, 78.233))) * 43758.5453);
}
const float zNear = .05;
const float zFar = 512;

float unlinearize_depth(float d) {
    float A   = -(zFar + zNear) / (zFar - zNear);
    float B   = -2*zFar*zNear / (zFar - zNear);
    float z_n = -(A*d + B) / d; // z_n in [-1, 1]
    return 0.5*z_n + 0.5; // z_b in [0, 1];
}

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

vec3 tonemapFilmic(const vec3 color) {
    vec3 x = max(vec3(0.0), color - 0.004);
    return (x * (6.2 * x + 0.5)) / (x * (6.2 * x + 1.7) + 0.06);
}

// Courtesy of Iñigo Quilez.
float snoise( in vec4 x )
{
    vec4 p = floor(x);
    vec4 f = fract(x);
    f = f*f*(3.0-2.0*f);

    vec2 uv = (p.xy+vec2(37.0,17.0)*p.z) + f.xy;
    vec2 rg = textureLod( iChannel0, (uv+ 0.5)/256.0, 0.0 ).yx;
    return mix( rg.x, rg.y, f.z );

}

vec4 get(vec3 position) {
    vec4 ShadowCoord = vec4(position.xyz + (texture(normalTex, TexCoords).rgb * vec3(2, 2, 2) - vec3(1, 1, 1)) * 2000 / shadowRes, 1.) * matrixFar;
    ShadowCoord /= ShadowCoord.w;
    ShadowCoord.xyz = ShadowCoord.xyz * .5 + .5;
    return ShadowCoord;
}


void main() {
    float depth = texture(depthTex, TexCoords).r;
    vec3 position = depthToView(TexCoords, depth, projection);
    float dist = distance(position, viewPos);
    vec4 albedo = texture(colorTex, TexCoords);
    vec4 normal = texture(normalTex, TexCoords) * vec4(2, 2, 2, 1) - vec4(1, 1, 1, 0);


    // iterate over the sample kernel and calculate occlusion factor
    float occlusion = 0;
    float comp = linearize_depth(depth) + .01;
    for(int i = 0; i < 8; ++i)
    {
        // get sample position
        vec3 samplePos = normalize(vec3(rand(TexCoords + i / 32. - 1.) * 2 - 1., rand(TexCoords + 1 + i / 32.) * 2 - 1., rand(TexCoords + 2 + i / 32.) * 2 - 1.)); // from tangent to view-space
        samplePos = position + samplePos * radius; 
        
        // project sample position (to sample texture) (to get position on screen/texture)
        vec4 offset = vec4(samplePos, 1.0) * _projection;
        offset.xyz /= offset.w; // perspective divide
        offset.xyz = offset.xyz * 0.5 + 0.5; // transform to range 0.0 - 1.0
        
        float sampleDepth = linearize_depth(texture(depthTex, offset.xy).r); 
        occlusion += (sampleDepth >= comp ? 1.0 : 0.0);  
    }
    occlusion /= 8.;
    float ambient = 1;
    
    vec3 sunLight = max(dot(normal.rgb, sun.position), 0.0) * sun.color;
    vec3 lightDir = normalize((sun.position + viewPos) - position);

    float sh = clamp(texture(shadowTex, TexCoords).r, 0., 1.);

    vec3 diffuse = ambient * skyLight * clamp(occlusion, 0., .5) / 2. * albedo.rgb + sunLight * albedo.rgb * sh;

    for (int i = 0; i < lightCount; i++) {
        vec3 lightDir = normalize(data[i].xyz - position);

        vec3 lightColor = pow(max(1 - length(data[i].xyz - position) / data[i].w, 0.), 4) * vec3(1.2, .3, .1) * 4;
        diffuse += (max(dot(normal.rgb, lightDir), 0.0) + normal.a / 4.) * (clamp(occlusion, 0., .5) / 2. + .5) * lightColor;
    }

    float fog = 0;
    vec3 fogColor = vec3(0);
#define count 50.
    vec3 col = texture(skyTex, TexCoords).rgb;
    for (int x = 0; x < count; x++) {
        vec3 _pos = viewPos * (1 - x/count) + position * (x/count);
		// fog += .5;
        fog += pow(clamp((snoise(vec4(_pos * 6.4, timeL)) / 8 + snoise(vec4(_pos * 1.6, timeL)) / 4 + snoise(vec4(_pos * 0.4, timeL)) / 2 + snoise(vec4(_pos * 0.1, timeL))) * .5 + .5, 0, 1), 2);
        vec3 exposure = vec3(0);
        for (int i = 0; i < lightCount; i++) {
            exposure += pow(max(1 - length(data[i].xyz - _pos) / data[i].w, 0.), 4) * vec3(1.2, .3, .1);
        }

        fogColor += (skyLight + col) * (texture(shadowMapTex, get(_pos).xyz) + ambient) + exposure;
    }
    fog = fog * .001 * pow(dist, 2.) / count;
    fog = fog / (fog + 1);
    fogColor /= count;
    // fogColor = fogColor / (fogColor + 1);

    // vec3 color = mix(max(vec3(0.), ), diffuse, clamp(albedo.a - foggyness, 0., 1.));
    // color += texture(starTex, TexCoords).rgb * 2 * max(0., 1. - albedo.a + pow(foggyness, 30.));
    // FragColor = vec4(tonemapFilmic(color * .5) / .725, 1.);
    diffuse = mix(diffuse, col + texture(starTex, TexCoords).rgb * 2, 1 - albedo.aaa) + fogColor * fog;
    FragColor = vec4(diffuse, 1);
}