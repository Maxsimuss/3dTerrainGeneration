#version 420

out vec4 FragColor[];

in vec3 Normal;
in vec3 FragPos;
in vec3 Color;
in float Emission;

void main()
{
    vec3 norm = Normal;
    if(!gl_FrontFacing) {
        norm = -Normal;
    }
    
    FragColor[0] = vec4(Color, 1.);
    FragColor[1] = vec4(norm / 2 + .5, Emission);
}