#version 330 core
out vec4 FragColor;
  
in vec2 TexCoords;

uniform sampler2D colortex0;
uniform sampler2D colortex1;

void main() {
	vec4 overlay = texture(colortex1, TexCoords);

    float alpha = overlay.a;
    if(alpha == 0.0) {
        alpha = 1;
    }

    FragColor = vec4(mix(texture(colortex0, TexCoords).rgb, overlay.rgb / alpha, overlay.a), 1.0);
}