#version 440 core

out vec4 out_color;
  
in vec2 TexCoords;
in vec3 vPos;

uniform sampler2D textureToDraw;

void main() {              
    out_color = texture(textureToDraw, TexCoords);
}