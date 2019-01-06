#version 440 core

#define fragCoord (gl_FragCoord.xy)

in vec3 vPos;
layout (location = 0) out vec4 out_color;

uniform vec3 sunColor = vec3(1.0);
uniform vec3 sunDir = normalize( vec3(0.0,1.0,-1.0));

uniform float time;
uniform vec2 resolution;
uniform vec3 cameraPos;
uniform mat4 view;
uniform float fov = 45.0;

uniform sampler3D cloudNoise;
uniform sampler3D worleyNoise;
uniform sampler2D weatherTexture;
uniform sampler2D curlNoise;

// Math constants
#define M_PI 3.1415926535897932384626433832795
#define EPSILON 0.001

// Field of View in radians
#define FOV (fov/180*M_PI)

// Raymarching constants
#define MAX_STEPS 32.0
#define LIGHT_RAY_ITERATIONS 6
#define RCP_LIGHT_RAY_ITERATIONS (1.0/float(LIGHT_RAY_ITERATIONS))

// Cloud constants
const float CLOUDS_MIN_TRANSMITTANCE = 1e-1;
const float CLOUDS_TRANSMITTANCE_THRESHOLD = 1.0 - CLOUDS_MIN_TRANSMITTANCE;
const float EARTH_RADIUS = (1500000.0);
const float SPHERE_INNER_RADIUS = (EARTH_RADIUS + 5000.0);
const float SPHERE_OUTER_RADIUS = (SPHERE_INNER_RADIUS + 17000.0);
const float SPHERE_DELTA = float(SPHERE_OUTER_RADIUS - SPHERE_INNER_RADIUS);
const vec3 sphereCenter = vec3(0.0, -EARTH_RADIUS, 0.0);
const vec3 windDirection = vec3(0, 0, -1);
const float CLOUD_SPEED = 100.0;
const float CLOUD_TOP_OFFSET = 750.0;
const float CLOUD_SCALE = 40.0;
const float coverageMultiplier = 0.4;
const vec3 CLOUDS_AMBIENT_COLOR_TOP = (vec3(169.0, 149.0, 149.0)*(1.5/255.0));
const vec3 CLOUDS_AMBIENT_COLOR_BOTTOM = (vec3(65.0, 70.0, 80.0)*(1.5/255.0));

vec3 noiseKernel[6] = vec3[] (
	vec3( 0.38051305,  0.92453449, -0.02111345),
	vec3(-0.50625799, -0.03590792, -0.86163418),
	vec3(-0.32509218, -0.94557439,  0.01428793),
	vec3( 0.09026238, -0.27376545,  0.95755165),
	vec3( 0.28128598,  0.42443639, -0.86065785),
	vec3(-0.16852403,  0.14748697,  0.97460106)
);

const vec4 STRATUS_GRADIENT = vec4(0.02, 0.05, 0.09, 0.11);
const vec4 STRATOCUMULUS_GRADIENT = vec4(0.02, 0.2, 0.48, 0.625);
const vec4 CUMULUS_GRADIENT = vec4(0.01, 0.0625, 0.78, 1.0);

// ---------------------------------
// Signed Distance Field Raymarching
// ---------------------------------

// Determine the unit vector to march along
vec3 rayDirection(in float fieldOfView, in vec2 size, in vec2 frag_coord) {
    vec2 xy = frag_coord - size / 2.0;
    float z = size.y / tan(fieldOfView / 2.0);
    return normalize(vec3(xy, -z));
}

// -----------------------------
// Fractal brownian motion noise
// Code sourced from: https://www.shadertoy.com/view/Xsl3zr


// Utility function that maps a value from one range to another.
float remap(in float originalValue, in float originalMin, in float originalMax, in float newMin, in float newMax) {
	return newMin + (((originalValue - originalMin) / (originalMax - originalMin)) * (newMax - newMin));
}

// Perlin-Worley noise for cloud shape and volume
// Idea sourced from GPU Pro 7
vec4 sampleCloudTex(in vec3 pos) {
	return texture(cloudNoise, pos);
}

// Worley noise to add detail to the clouds
// Idea sourced from GPU Pro 7
vec4 worley(in vec3 p) {
	return texture(worleyNoise, p);
}

// Curl noise for whisps in clouds
// Idea sourced from GPU Pro 7
vec4 curl(in vec2 p) {
	return texture(curlNoise, p);
}

// Sample from weather texture
// Idea of a weather texture sourced from GPU Pro 7 
vec4 weather(in vec2 pos) {
	return texture(weatherTexture, pos);
}

// Mix density gradients of different cloud types
// Idea sourced from GPU Pro 7
vec4 mixGradients(in float cloudType){
	float stratus = 1.0 - clamp(cloudType * 2.0, 0.0, 1.0);
	float stratoCumulus = 1.0 - abs(cloudType - 0.5) * 2.0;
	float cumulus = clamp(cloudType - 0.5, 0.0, 1.0) * 2.0;
	return STRATUS_GRADIENT * stratus + STRATOCUMULUS_GRADIENT * stratoCumulus + CUMULUS_GRADIENT * cumulus;
}

float getDensityHeightGradient(in float heightFrac, in float cloudType) {
	vec4 cloudGradient = mixGradients(cloudType);

	// gradicent computation (see Siggraph 2017 Nubis-Decima talk)
	return smoothstep(cloudGradient.x, cloudGradient.y, heightFrac) - smoothstep(cloudGradient.z, cloudGradient.w, heightFrac);
}

// fractional value for sample position in the cloud layer
// get global fractional position in cloud zone
float getHeightFraction(vec3 pos){
	return clamp((length(pos - sphereCenter) - SPHERE_INNER_RADIUS)/(SPHERE_OUTER_RADIUS - SPHERE_INNER_RADIUS), 0.0, 1.0);
}

// ---------------------
// Sampling cloud volume
// ---------------------

float getCoverage(in vec3 weatherData) {
	return weatherData.r;
}

float getPrecipitation(in vec3 weatherData) {
	return weatherData.g;
}

float getCloudType(in vec3 weatherData) {
	// weather b channel tells the cloud type 0.0 = stratus, 0.5 = stratocumulus, 1.0 = cumulus
	return weatherData.b;
}

// Cloud density algorithm follows GPU Pro 7 Article's Idea
// Help with implementation sourced from: https://www.gamedev.net/forums/topic/680832-horizonzero-dawn-cloud-system/?page=6
float sampleCloudDensity(in vec3 pos, in vec3 weatherData, in float heightFrac, in bool highQuality) {
	pos += heightFrac * windDirection * CLOUD_TOP_OFFSET;
	pos += (windDirection + vec3(0.0, -0.25, 0.0)) * CLOUD_SPEED * time;
	pos *= CLOUD_SCALE;
	
	// Fluffy cloud shapes achieved with Perlin-Worley Noise
	vec4 lowFreqNoise = sampleCloudTex(pos);
	float lowFreqFBM =
		(lowFreqNoise.g * 0.625) +
		(lowFreqNoise.b * 0.25) +
		(lowFreqNoise.a * 0.125);

	float baseCloud = remap(
		lowFreqNoise.r,
		-(1.0 - lowFreqFBM), 1.0,
		0.0, 1.0);

	float densityGradient = getDensityHeightGradient(heightFrac, getCloudType(weatherData));
	baseCloud *= densityGradient;

	float cloudCoverage = getCoverage(weatherData);
	float baseCloudWithCoverage = remap(
		baseCloud,
		1.0 - cloudCoverage, 1.0,
		0.0, 1.0);
	baseCloudWithCoverage *= cloudCoverage;

	if (highQuality) {
		// Add curl noise (whisps in the clouds)
		pos += curl(pos.xy).xy * (1.0 - heightFrac);

		// Erode the clouds to add detail using worley noise
		vec3 highFreqNoise = worley(pos * 0.1).rgb;
		float highFreqFBM =
			(highFreqNoise.r * 0.625) +
			(highFreqNoise.g * 0.25) +
			(highFreqNoise.b * 0.125);

		float highFreqNoiseModifier = mix(highFreqFBM, 1.0 - highFreqFBM, clamp(heightFrac * 10.0, 0.0, 1.0));

		baseCloudWithCoverage = remap(
			baseCloudWithCoverage,
			highFreqNoiseModifier * 0.2, 1.0,
			0.0, 1.0);
	}

	return clamp(baseCloudWithCoverage, 0.0, 1.0);
}

// ------------------
// Lighting
// ------------------


// Determine amount of light which reaches a point in the cloud by raymarching through a cone
// Implementation help sourced from: https://github.com/fede-vaccaro/TerrainEngine-OpenGL/blob/master/shaders/volumetric_clouds.frag

float beerLambert(float sampleDensity, float precipitation)
{
	return exp(-sampleDensity * precipitation);
}

float powder(float sampleDensity, float lightDotEye) {
	float powd = 1.0 - exp(-sampleDensity * 2.0);
	return mix(
		1.0,
		powd,
		clamp((-lightDotEye * 0.5) + 0.5, 0.0, 1.0)
	);
}

float henyeyGreenstein(float lightDotEye, float g) {
	float g2 = g * g;
	return ((1.0 - g2) / pow((1.0 + g2 - 2.0 * g * lightDotEye), 1.5)) * 0.25;
}

float lightEnergy(float lightDotEye, float densitySample, float originalDensity, float precipitation) {
	return 2.0 *
		beerLambert(densitySample, precipitation) *
		powder(originalDensity, lightDotEye) * 
		mix(henyeyGreenstein(lightDotEye, 0.8), henyeyGreenstein(lightDotEye, -0.5), 0.5);
}

float sampleCloudDensityAlongCone(vec3 startPos, float stepSize, float lightDotEye, float originalDensity) {
	vec3 lightStep = stepSize * sunDir;
	vec3 pos = startPos;
	float coneRadius = 1.0;
	float coneStep = RCP_LIGHT_RAY_ITERATIONS;
	float densityAlongCone = 0.0;
	float lodStride = RCP_LIGHT_RAY_ITERATIONS;
	vec3 weatherData = vec3(0.0);
	float rcpThickness = 1.0 / (stepSize * LIGHT_RAY_ITERATIONS);
	float density = 0.0;

	for(uint i = 0; i < LIGHT_RAY_ITERATIONS; ++i) {
		vec3 conePos = pos + coneRadius * noiseKernel[i] * float(i + 1u);
		float heightFrac = getHeightFraction(conePos);
		if(heightFrac <= 1.0f) {
			weatherData = weather(conePos.xy).rgb;
			float cloudDensity = sampleCloudDensity(
				conePos,
				weatherData,
				heightFrac,
				density > 0.3);

			if(cloudDensity > 0.0f)
			{
				density += cloudDensity;
				float transmittance = 1.0f - (density * rcpThickness);
				densityAlongCone += (cloudDensity * transmittance);
			}
		}
		pos += lightStep;
		coneRadius += coneStep;
	}

	// take additional step at large distance away for shadowing from other clouds
	pos = pos + (lightStep * 8.0f);
	weatherData = weather(pos.xz).rgb;
	float heightFrac = getHeightFraction(pos);
	if(heightFrac <= 1.0f) {
		float cloudDensity = sampleCloudDensity(
			pos,
			weatherData,
			heightFrac,
			false);

		// no need to branch here since density variable is no longer used after this
		density += cloudDensity;
		float transmittance = 1.0f - clamp(density * rcpThickness, 0.0, 1.0);
		densityAlongCone += (cloudDensity * transmittance);
	}

	return clamp(lightEnergy(
		lightDotEye,
		densityAlongCone,
		originalDensity,
		mix(1.0f, 2.0f, getPrecipitation(weatherData))), 0.0, 1.0);
}

vec3 ambientLight(float heightFrac) {
	return mix(
		vec3(0.5f, 0.67f, 0.82f),
		vec3(1.0f, 1.0f, 1.0f),
		heightFrac);
}

// Volumetric raymarching algorithm
vec4 volumetricRaymarch(in vec3 p, in vec3 rayDir, in float stepSize, in vec2 t, in vec2 dt, in vec2 wt, in vec3 endRay) {
	vec4 accumulation = vec4(0.0);

	float thickness = length(endRay - p);
	float rcpThickness = 1.0f / thickness;
	float lightDotEye = dot(normalize(sunDir), normalize(rayDir));
	const float absorption = 0.01;
	float density;

	// Integrate over the volume texture to determine transmittance
	for (int i = 0; i < MAX_STEPS; ++i) {
		// Snap next sample to view aligned plane
		vec4 data = t.x < t.y ? vec4( t.x, wt.x, dt.x, 0.0 ) : vec4( t.y, wt.y, 0.0, dt.y );
		vec3 pos = p + data.x * rayDir;
		float w = data.y;
		t += data.zw;

		vec3 weatherData = weather(pos.xz).rgb;
		float heightFrac = getHeightFraction(pos);
		float cloudDensity = sampleCloudDensity(pos, weatherData, heightFrac, true);
		if (cloudDensity > 0.0 + EPSILON) {
			density += cloudDensity;
			float transmittance = 1.0f - (density * rcpThickness);
			float lightDensity = sampleCloudDensityAlongCone(
				pos,
				stepSize,
				lightDotEye,
				cloudDensity);

			vec3 ambientBadApprox = ambientLight(heightFrac) * min(1.0f, length(sunColor.rgb * 0.0125f)) * transmittance;
			vec4 source = vec4((sunColor.rgb * lightDensity) + ambientBadApprox, cloudDensity * transmittance);
			source.rgb *= source.a;
			accumulation = (1.0f - accumulation.a) * source + accumulation;
	
			if(accumulation.a >= 1.0f) {
				break;
			}
		}
	}

	return accumulation;
}

// Plane Alignment
// From: https://github.com/huwb/volsample
//NOTE: This assumes the volume will only be UNIFORMLY scaled. Non uniform scale would require tons of little changes.
float mysign( float x ) { return x < 0. ? -1. : 1. ; }
vec2 mysign( vec2 x ) { return vec2( x.x < 0. ? -1. : 1., x.y < 0. ? -1. : 1. ) ; }

void planeAlignment(in vec3 ro, in vec3 rd, in float stepSize, out vec2 t, out vec2 dt, out vec2 wt) {
	dt = vec2(stepSize);
    t = dt;
    wt = vec2(0.5,0.5);
    return;

	// structured sampling pattern line normals
    vec3 n0 = (abs( rd.x ) > abs( rd.z )) ? vec3(1., 0., 0.) : vec3(0., 0., 1.); // non diagonal
    vec3 n1 = vec3(mysign( rd.x * rd.z ), 0., 1.); // diagonal

    // normal lengths (used later)
    vec2 ln = vec2(length( n0 ), length( n1 ));
    n0 /= ln.x;
    n1 /= ln.y;

    // some useful DPs
    vec2 ndotro = vec2(dot( ro, n0 ), dot( ro, n1 ));
    vec2 ndotrd = vec2(dot( rd, n0 ), dot( rd, n1 ));

    // step size
	vec2 period = ln * stepSize;
    dt = period / abs( ndotrd );

    // dist to line through origin
    vec2 dist = abs( ndotro / ndotrd );

    // raymarch start offset - skips leftover bit to get from ro to first strata lines
    t = -mysign( ndotrd ) * mod( ndotro, period ) / abs( ndotrd );
    if( ndotrd.x > 0. ) t.x += dt.x;
    if( ndotrd.y > 0. ) t.y += dt.y;

    // sample weights
    float minperiod = stepSize;
    float maxperiod = sqrt( 2. )*stepSize;
    wt = smoothstep( maxperiod, minperiod, dt/ln );
    wt /= (wt.x + wt.y);
}


float raySphereIntersection(in vec3 pos, in vec3 dir, in float r) {
    float a = dot(dir, dir);
    float b = 2.0 * dot(dir, pos);
    float c = dot(pos, pos) - (r * r);
		float d = sqrt((b*b) - 4.0*a*c);
		float p = -b - d;
		float p2 = -b + d;
    return max(p, p2)/(2.0*a);
}

void main() {
	vec3 rayDir = rayDirection(FOV, resolution, fragCoord);

	//compute raymarching starting and ending point by intersecting with spheres
	vec3 startPos, endPos;
	startPos = rayDir * raySphereIntersection(cameraPos, rayDir, SPHERE_INNER_RADIUS);
	endPos = rayDir * raySphereIntersection(cameraPos, rayDir, SPHERE_OUTER_RADIUS);

	float stepSize = length(endPos - startPos) / MAX_STEPS;

	vec2 t, dt, wt;
	planeAlignment(startPos, rayDir, stepSize, t, dt, wt);

	vec4 color = volumetricRaymarch(startPos, rayDir, stepSize, t, dt, wt, endPos);

	// add sun glare to clouds
	float sun = clamp( dot(sunDir,normalize(endPos - startPos)), 0.0, 1.0 );
	vec3 s = 0.8*vec3(1.0,0.4,0.2)*pow( sun, 256.0 );
	color.rgb += s * color.a;

	out_color = color;
}