#version 440 core

uniform float exposure;
uniform float decay;
uniform float density;
uniform float weight;
uniform vec2 lightPositionOnScreen;
uniform sampler2D firstPass;
const int NUM_SAMPLES = 100;

in vec2 TexCoords;
in vec3 vPos;

out vec4 FragColor;

void main() {	
	vec2 textureCoords = TexCoords;
    vec2 deltaTexCoords = vec2(textureCoords.st - lightPositionOnScreen.xy);
    deltaTexCoords *= 1.0 /  float(NUM_SAMPLES) * density;
    float illuminationDecay = 1.0;
	
	
    for(int i = 0; i < NUM_SAMPLES; ++i) {
            textureCoords -= deltaTexCoords;
            vec4 occlusionSample = texture(firstPass, textureCoords);
			
            occlusionSample *= illuminationDecay * weight;

            FragColor += occlusionSample;

            illuminationDecay *= decay;
    }
    
	FragColor *= exposure;
}
