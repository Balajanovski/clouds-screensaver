#version 440 core

in vec2 texCoords;

in vec3 surfaceNormal;
in vec3 toLightSource;
in vec3 vPos;

in vec3 toCameraVector;

uniform sampler2D healthyGrass, grass, patchyGrass, rock, snow;
uniform float snowHeight;
uniform float grassCoverage;
uniform float amplitude;

out vec4 out_color;

vec4 applySnow(in vec4 heightColor, in float height, in float transition, in vec4 snowTex) {
	vec4 snowedHeightColor = heightColor;

	if (height >= snowHeight - transition) {
		snowedHeightColor = snowTex;
	} else if (height >= snowHeight- 2*transition) {
		snowedHeightColor = mix(snowTex, heightColor, pow(1.3, -height + (snowHeight- 2*transition)));
	}

	return snowedHeightColor;
}

vec4 getTexture() {
	float transition = 20.0;

	float height = vPos.y;

	vec4 rockTex = texture(rock, texCoords*40.0);
	vec4 snowTex = texture(snow, texCoords*40.0);
	// Mix different grass textures together at differing frequencies to create more natural looking grass
	vec4 grassTex = texture(grass, texCoords*(40.0)) * 0.4 +
					texture(healthyGrass, texCoords*(50.0)) * 0.3 + 
					texture(patchyGrass, texCoords*(10.0)) * 0.3;

	vec3 upVector = vec3(0, 1, 0);

	vec4 heightColor;
	float cosV = abs(dot(surfaceNormal, upVector)) / (length(surfaceNormal) * 1.0);
	float tenPercentGrass = grassCoverage - grassCoverage*0.1;

	// Find base texture
	if(cosV > tenPercentGrass) {
		float blendingCoeff = clamp(pow((cosV - tenPercentGrass) / (grassCoverage * 0.1), 1.0), 0.0, 1.0);
		heightColor = mix(rockTex, grassTex, blendingCoeff);

		vec4 snowedHeightColor = applySnow(heightColor, height, transition, snowTex);
		heightColor = mix(heightColor, snowedHeightColor, blendingCoeff);
	} else if (cosV > grassCoverage) {
		heightColor = grassTex;

		heightColor = applySnow(heightColor, height, transition, snowTex);
    } else {
		heightColor = rockTex;	
	}


	return heightColor;
	
}

void main() {
	out_color = getTexture();
}