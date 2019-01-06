#version 440 core

layout (location = 0) in vec2 aPos;
layout (location = 1) in vec2 aTexCoords;

out vec2 TexCoords;
out vec3 vPos;

void main() {
	gl_Position = vec4(aPos, 0.0, 1.0);
	vPos = vec3(aPos, 1.0);
	TexCoords = aTexCoords;
}