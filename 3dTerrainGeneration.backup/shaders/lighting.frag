#version 330 core

struct Material {
    vec3 specular;
    float shininess;
};

struct Light {
    vec3 position;
    float amount;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};


uniform Light sun;
uniform Light moon;
uniform Material material;

uniform vec3 viewPos;
uniform vec3 fogColor;
uniform float renderDistance;

out vec4 FragColor[];

in vec3 Normal;
in vec3 FragPos;
in vec3 Color;

void main()
{
    vec3 viewDir = normalize(viewPos - FragPos);
    vec3 norm = normalize(Normal);
    vec3 sunLightDir = normalize(sun.position);
    vec3 moonLightDir = normalize(moon.position);
    

	vec3 sunHalfwayDir = normalize(sunLightDir + viewDir);
	vec3 moonHalfwayDir = normalize(moonLightDir + viewDir);


    float sunLight = max(dot(norm, sunLightDir), 0.0);
    float moonLight = max(dot(norm, moonLightDir), 0.0);

    
    vec3 ambient = (sun.ambient * sun.amount + moon.ambient * moon.amount) * Color / 255.;
    vec3 diffuse = (sun.diffuse * sunLight * sun.amount + moon.diffuse * moonLight * moon.amount) * Color / 255.;

    vec3 specular = 
		((sun.specular * pow(max(dot(Normal, sunHalfwayDir), 0.0), material.shininess) * sun.amount) + 
		(moon.specular * pow(max(dot(Normal, moonHalfwayDir), 0.0), material.shininess) * moon.amount))
		* material.specular;

    vec3 color = ambient + (diffuse + specular);

	float dist = distance(FragPos, viewPos);
	float farFade = 1. - max(min(dist / renderDistance, 1.), 0.);

    //FragColor[0] = vec4(norm / 2, 1.);
    FragColor[0] = vec4(ambient, 1.);
    FragColor[1] = vec4(diffuse + specular, 1.);
    FragColor[3] = vec4(vec3(farFade), 1.);
}