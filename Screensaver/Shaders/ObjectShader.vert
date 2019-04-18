#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoords;

out vec2 TexCoords;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

out vec3 surfaceNormal;

out vec3 toCameraVector;

void main() {
	vec4 worldPos = model * vec4(aPos, 1.0);
	gl_Position = projection * view * worldPos;

    TexCoords = aTexCoords;    
	surfaceNormal = transpose(inverse(mat3(model))) * aNormal;;
    
	toCameraVector = normalize(( inverse(view) * vec4(0.0, 0.0, 0.0, 0.1) ).xyz - worldPos.xyz);
}