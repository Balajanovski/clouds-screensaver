#version 440 core

layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoords;

out vec2 texCoords;

out vec3 surfaceNormal;
out mat3 TBN;
out vec3 vPos;
out vec4 fragPosLightSpace;

out vec3 toCameraVector;

uniform mat4 model;
uniform mat4 projectionMatrix;
uniform mat4 viewMatrix;
uniform mat4 lightSpaceProjection;
uniform mat4 lightSpaceView;

void main() {
	vec4 worldPos = model * vec4(aPos, 1.0);

	fragPosLightSpace = lightSpaceProjection * lightSpaceView * vec4(worldPos.xyz, 1.0);

	texCoords = aTexCoords;

	surfaceNormal = mat3(transpose(inverse(model))) * aNormal;

	// Calculate TBN matrix for normal mapping through using a clever assumption that the
	// tangent's x-component must be 0, since the terrain is splatted-grid geometry
	// Assumption from: https://www.gamedev.net/forums/topic/651083-tangents-for-heightmap-from-limited-information/
	vec3 tempTangent = vec3(0, 0, 1);
	vec3 aBitangent = cross(tempTangent, aNormal);
	vec3 aTangent = cross(aNormal, aBitangent);

	vec3 T = normalize(vec3(model * vec4(aTangent, 0.0)));
	vec3 B = normalize(vec3(model * vec4(aBitangent, 0.0)));
	vec3 N = normalize(vec3(model * vec4(aNormal, 0.0)));

	TBN = transpose(mat3(T, B, N));

	toCameraVector = normalize(( inverse(viewMatrix) * vec4(0.0, 0.0, 0.0, 0.1) ).xyz - worldPos.xyz);
	vPos = aPos;

	gl_Position = projectionMatrix * viewMatrix * worldPos;
}