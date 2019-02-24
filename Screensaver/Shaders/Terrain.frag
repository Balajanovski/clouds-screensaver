#version 440 core

in vec2 texCoords;

in vec3 surfaceNormal;
in vec3 toLightSource;
in vec3 vPos;

in vec3 toCameraVector;

uniform sampler2D grassTexture;

out vec4 out_color;

void main() {
	out_color = texture(grassTexture, texCoords);
}