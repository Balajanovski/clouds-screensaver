#version 440 core

uniform vec2 lightPositionOnScreen;
uniform sampler2D occlusionTex;

const int NUM_SAMPLES = 100;

const float exposure = 0.45;
const float decay = 0.96;
const float density = 0.9;
const float weight = 0.04;

in vec2 TexCoords;
in vec3 vPos;

layout (location = 0) out vec4 FragColor;

float evaluateSummation() {
	vec2 textureCoords = TexCoords;
    vec2 deltaTexCoords = vec2(textureCoords.xy - lightPositionOnScreen);
    deltaTexCoords *= 1.0 /  float(NUM_SAMPLES) * density;
    float illuminationDecay = 1.0;
	float alphaness = 0.0;
	
	// Evaluate summation from Equation 3 ( see https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch13.html) NUM_SAMPLES iterations.
    for(int i = 0; i < NUM_SAMPLES; ++i) {
            textureCoords -= deltaTexCoords;

            vec4 occlusionSample = texture(occlusionTex, textureCoords);
			
            occlusionSample *= illuminationDecay * weight;

            alphaness += occlusionSample.r;

            illuminationDecay *= decay;
    }

	alphaness *= exposure;
	alphaness = clamp(1.0 - alphaness, 0.0, 1.0);

	return alphaness;
}

void main() {	
	FragColor = vec4(evaluateSummation());
}
