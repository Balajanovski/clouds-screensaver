#version 440 core

in vec2 TexCoords;

uniform sampler2D texture_diffuse1;
uniform sampler2D texture_emmissive1;

void main() {
	gl_FragDepth = gl_FragCoord.z / max(texture(texture_emmissive1, TexCoords).r, texture(texture_diffuse1, TexCoords).a);
}