#version 440 core

out vec4 out_color;
  
in vec2 TexCoords;
in vec3 vPos;

uniform sampler2D textureToDraw;

uniform vec2 resolution;

#define  offset_x  1. / resolution.x  
#define offset_y 1. / resolution.y

vec4 gaussianBlur(sampler2D tex, vec2 uv){
 vec2 offsets[9] = vec2[](
        vec2(-offset_x,  offset_y), // top-left
        vec2( 0.0f,    offset_y), // top-center
        vec2( offset_x,  offset_y), // top-right
        vec2(-offset_x,  0.0f),   // center-left
        vec2( 0.0f,    0.0f),   // center-center
        vec2( offset_x,  0.0f),   // center-right
        vec2(-offset_x, -offset_y), // bottom-left
        vec2( 0.0f,   -offset_y), // bottom-center
        vec2( offset_x, -offset_y)  // bottom-right    
    );

	const float kernelDividingFactor = 16.0;
	float kernel[9] = float[](
		1.0 / kernelDividingFactor, 2.0 / kernelDividingFactor, 1.0 / kernelDividingFactor,
		2.0 / kernelDividingFactor, 4.0 / kernelDividingFactor, 2.0 / kernelDividingFactor,
		1.0 / kernelDividingFactor, 2.0 / kernelDividingFactor, 1.0 / kernelDividingFactor  
	);
    
    vec4 sampleTex[9];

    for(int i = 0; i < 9; i++)
    {	
		vec4 pixel = texture(tex, uv.st + offsets[i]);
        sampleTex[i] = pixel;
    }
    vec4 col = vec4(0.0);
    for(int i = 0; i < 9; i++)
        col += sampleTex[i] * kernel[i];
    
    return col;
}

void main() {              
    out_color = gaussianBlur(textureToDraw, TexCoords);
}