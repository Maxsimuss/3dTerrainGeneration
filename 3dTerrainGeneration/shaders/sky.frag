#version 430 core
out vec4 FragColor;
  
in vec2 TexCoords;

uniform vec3 sun_dir;

void main() {
    vec3 c = vec3(.055, .130, .224);
    
    vec3 col = pow(vec3(pow(smoothstep(0., 1., sun_dir.y / 2 + .5), 24.) * 80.), c);
    FragColor = vec4(col / 10. + c / 20.,1.0);
}