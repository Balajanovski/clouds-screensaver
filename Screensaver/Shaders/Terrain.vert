#version 440 core

layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoords;

out vec2 texCoords;

out vec3 surfaceNormal;
out mat3 TBN;
out vec3 vPos;

out vec3 toCameraVector;

uniform mat4 modelMatrix;
uniform mat4 projectionMatrix;
uniform mat4 viewMatrix;

void main() {
	vec4 worldPos = modelMatrix * vec4(aPos, 1.0);

	gl_Position = projectionMatrix * viewMatrix * worldPos;

	texCoords = aTexCoords;

	surfaceNormal = normalize((modelMatrix * vec4(aNormal, 0.0)).xyz);

	// Calculate TBN matrix for normal mapping through using a clever assumption that the
	// tangent's x-component must be 0, since the terrain is splatted-grid geometry
	// Assumption from: https://www.gamedev.net/forums/topic/651083-tangents-for-heightmap-from-limited-information/
	vec3 tempTangent = vec3(0, 0, 1);
	vec3 surfaceBitTangent = cross(tempTangent, surfaceNormal);
	vec3 surfaceTangent = cross(surfaceNormal, surfaceBitTangent);
	TBN = mat3(surfaceTangent, surfaceBitTangent, surfaceNormal);

	toCameraVector = normalize(( inverse(viewMatrix) * vec4(0.0, 0.0, 0.0, 0.1) ).xyz - worldPos.xyz);
	vPos = aPos;
}