#version 440 core

layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoords;

out vec2 texCoords;

out vec3 surfaceNormal;
out vec3 toLightSource;
out vec3 vPos;

out vec3 toCameraVector;

uniform mat4 modelMatrix;
uniform mat4 projectionMatrix;
uniform mat4 viewMatrix;

uniform vec3 lightPos;

void main() {
	vec4 worldPos = modelMatrix * vec4(aPos, 1.0);

	gl_Position = projectionMatrix * viewMatrix * worldPos;

	texCoords = aTexCoords;

	surfaceNormal = normalize((modelMatrix * vec4(aNormal, 0.0)).xyz);
	toLightSource = normalize(lightPos - worldPos.xyz);

	toCameraVector = normalize(( inverse(viewMatrix) * vec4(0.0, 0.0, 0.0, 0.1) ).xyz - worldPos.xyz);
	vPos = aPos;
}