#version 440 core

#define fragCoord (gl_FragCoord.xy)

layout (location = 0) out vec4 out_color;
layout (location = 1) out vec4 cloudColor;
layout (location = 2) out vec4 alphaness;

uniform vec3 sunColor;
uniform vec3 sunDir;

uniform float EARTH_RADIUS;

uniform float time;
uniform vec2 resolution;
uniform vec3 cameraPos;

uniform mat4 inverseView;
uniform mat4 inverseProjection;
uniform mat4 inverseViewProjection;
uniform mat4 oldViewProjection;

// Assortment of different noises for cloud volumes
uniform sampler3D cloudNoise;
uniform sampler3D worleyNoise;
uniform sampler2D weatherTexture;
uniform sampler2D curlNoise;

// Previous frame for temporal reprojection
uniform sampler2D lastFrame;
uniform sampler2D lastFrameAlphaness;

// Terrain occlusion, for excluding texels from calculations as an optimization
uniform sampler2D terrainOcclusion;

// Frame iteration counter for temporal reprojection mod 16
uniform int frameIter;

// Math constants
#define M_PI 3.1415926535897932384626433832795
#define EPSILON 0.001

// Raymarching constants
#define MAX_STEPS 255.0
#define LIGHT_RAY_ITERATIONS 6
#define RCP_LIGHT_RAY_ITERATIONS (1.0/float(LIGHT_RAY_ITERATIONS))

// Cloud constants
const float CLOUDS_MIN_TRANSMITTANCE = 1e-1;
const float CLOUDS_TRANSMITTANCE_THRESHOLD = 1.0 - CLOUDS_MIN_TRANSMITTANCE;
const float SPHERE_INNER_RADIUS = (EARTH_RADIUS + 5000.0);
const float SPHERE_OUTER_RADIUS = (SPHERE_INNER_RADIUS + 17000.0);
const float SPHERE_DELTA = float(SPHERE_OUTER_RADIUS - SPHERE_INNER_RADIUS);
const vec3 sphereCenter = vec3(0.0, -EARTH_RADIUS, 0.0);
const vec3 windDirection = vec3(1, 0, 1);
const float CLOUD_SPEED = 100.0;
const float CLOUD_TOP_OFFSET = 750.0;
const float CLOUD_SCALE = 40.0;
const float coverageMultiplier = 0.6;
const vec3 CLOUDS_AMBIENT_COLOR_TOP = (vec3(169.0, 149.0, 149.0)*(1.5/255.0));
const vec3 CLOUDS_AMBIENT_COLOR_BOTTOM = (vec3(65.0, 70.0, 80.0)*(1.5/255.0));
const float WEATHER_SCALE = 0.0000008;

vec3 noiseKernel[6] = vec3[] (
	vec3( 0.38051305,  0.92453449, -0.02111345),
	vec3(-0.50625799, -0.03590792, -0.86163418),
	vec3(-0.32509218, -0.94557439,  0.01428793),
	vec3( 0.09026238, -0.27376545,  0.95755165),
	vec3( 0.28128598,  0.42443639, -0.86065785),
	vec3(-0.16852403,  0.14748697,  0.97460106)
);

const vec4 STRATUS_GRADIENT = vec4(0.0, 0.1, 0.2, 0.3);
const vec4 STRATOCUMULUS_GRADIENT = vec4(0.02, 0.2, 0.48, 0.625);
const vec4 CUMULUS_GRADIENT = vec4(0.00, 0.1625, 0.88, 0.98);

// Utility function that maps a value from one range to another.
float remap(in float originalValue, in float originalMin, in float originalMax, in float newMin, in float newMax) {
	return newMin + (((originalValue - originalMin) / (originalMax - originalMin)) * (newMax - newMin));
}

// Perlin-Worley noise for cloud shape and volume
// Idea sourced from GPU Pro 7
vec4 sampleCloudTex(in vec3 pos) {
	return texture(cloudNoise, pos) * 0.8;
}

// Worley noise to add detail to the clouds
// Idea sourced from GPU Pro 7
vec4 worley(in vec3 pos) {
	return texture(worleyNoise, pos) * 0.1;
}

// Curl noise for whisps in clouds
// Idea sourced from GPU Pro 7
vec4 curl(in vec2 pos) {
	return texture(curlNoise, pos);
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

float getDensityForCloudType(in float heightFrac, in float cloudType) {
	vec4 cloudGradient = mixGradients(cloudType);

	// gradicent computation (see Siggraph 2017 Nubis-Decima talk)
	return smoothstep(cloudGradient.x, cloudGradient.y, heightFrac) - smoothstep(cloudGradient.z, cloudGradient.w, heightFrac);
}

// fractional value for sample position in the cloud layer
// get global fractional position in cloud zone
float getHeightFraction(vec3 pos){
	return(length(pos - sphereCenter) - SPHERE_INNER_RADIUS)/SPHERE_DELTA;
}

// ---------------------
// Sampling cloud volume
// ---------------------

float getCoverage(in vec3 weatherData) {
	return smoothstep(0.0, 0.4, weatherData.r);
}

float getPrecipitation(in vec3 weatherData) {
	return weatherData.g;
}

float getCloudType(in vec3 weatherData) {
	// weather b channel tells the cloud type 0.0 = stratus, 0.5 = stratocumulus, 1.0 = cumulus
	return weatherData.b;
}

vec2 getUVProjection(vec3 p){
	return p.xz/SPHERE_INNER_RADIUS + 0.5;
}

// Cloud density algorithm follows GPU Pro 7 Article's Idea
// Help with implementation sourced from: https://www.gamedev.net/forums/topic/680832-horizonzero-dawn-cloud-system/?page=6
float sampleCloudDensity(in vec3 pos, in float heightFrac, in bool highQuality) {
	vec3 animation = heightFrac * windDirection * CLOUD_TOP_OFFSET + windDirection * time * CLOUD_SPEED;
	vec2 uv = getUVProjection(pos);
	vec2 movingUV = getUVProjection(pos + animation);

	if(heightFrac < 0.0 || heightFrac > 1.0) {
		return 0.0;
	}
	
	// Fluffy cloud shapes achieved with Perlin-Worley Noise
	vec4 lowFreqNoise = sampleCloudTex(vec3(uv*CLOUD_SCALE, heightFrac));
	float lowFreqFBM = dot(lowFreqNoise.gba, vec3(0.625, 0.25, 0.125));

	float baseCloud = remap(
		lowFreqNoise.r,
		-(1.0 - lowFreqFBM), 1.0,
		0.0, 1.0);

	vec3 weatherData = weather(movingUV).rgb * coverageMultiplier;

	float density = getDensityForCloudType(heightFrac, 0.5);
	baseCloud *= (density / heightFrac);

	float cloudCoverage = getCoverage(weatherData);
	float baseCloudWithCoverage = remap(
		baseCloud,
		cloudCoverage, 1.0,
		0.0, 1.0);
	baseCloudWithCoverage *= cloudCoverage;

	if (highQuality) {
		// Add curl noise (whisps in the clouds)
		vec2 whisp = curl(vec2(movingUV*CLOUD_SCALE)*0.1).xy;
		pos.xy += whisp * 400.0 * (1.0 - heightFrac);

		// Erode the clouds to add detail using worley noise
		vec3 highFreqErosionNoise = worley(vec3(movingUV*CLOUD_SCALE, heightFrac)).rgb;
		float highFreqErosionFBM = dot(highFreqErosionNoise.rgb, vec3(0.625, 0.25, 0.125));

		float highFreqNoiseModifier = mix(highFreqErosionFBM, 1.0 - highFreqErosionFBM, clamp(heightFrac * 10.0, 0.0, 1.0));

		baseCloudWithCoverage = baseCloudWithCoverage - highFreqNoiseModifier * (1.0 - baseCloudWithCoverage);

		baseCloudWithCoverage = remap(
			baseCloudWithCoverage * 2.0,
			highFreqNoiseModifier * 0.2, 1.0,
			0.0, 1.0);
	}

	return clamp(baseCloudWithCoverage, 0.0, 1.0);
}

// ------------------
// Lighting
// ------------------

float beerLambert(float sampleDensity, float precipitation) {
	return exp(-sampleDensity * precipitation);
}

float powder(float d){
	return (1. - exp(-2.*d));
}

float henyeyGreenstein(float lightDotEye, float g) {
	float g2 = g * g;
	return ((1.0 - g2) / pow((1.0 + g2 - 2.0 * g * lightDotEye), 1.5)) * 0.25;
}

// Determine amount of light which reaches a point in the cloud by raymarching through a cone
// Implementation help sourced from: https://github.com/fede-vaccaro/TerrainEngine-OpenGL/blob/master/shaders/volumetric_clouds.frag
float sampleCloudDensityAlongCone(in vec3 startPos, in float stepSize, in float lightDotEye, in float originalDensity) {
	float ds = stepSize * 6.0;
	vec3 rayStep = sunDir * ds;
	const float CONE_STEP = 1.0/6.0;
	float coneRadius = 1.0; 
	float density = 0.0;
	float coneDensity = 0.0;
	float invDepth = 1.0/ds;
	const float absorption = 0.0035;
	float sigmaDS = -ds*absorption;
	vec3 pos;

	float T = 1.0;

	for(int i = 0; i < 6; i++)
	{
		pos = startPos + coneRadius*noiseKernel[i]*float(i);

		float heightFraction = getHeightFraction(pos);
		if(heightFraction >= 0)
		{
			float cloudDensity = sampleCloudDensity(pos, heightFraction, density > 0.3);
			if(cloudDensity > 0.0)
			{
				float Ti = exp(cloudDensity*sigmaDS);
				T *= Ti;
				density += cloudDensity;
			}
		}
		startPos += rayStep;
		coneRadius += CONE_STEP;
	}

	return T;
}

vec3 ambientLight(float heightFrac) {
	return mix(
		CLOUDS_AMBIENT_COLOR_BOTTOM * sunColor,
		CLOUDS_AMBIENT_COLOR_TOP * sunColor,
		heightFrac);
}

float lightScattering(in float lightDotEye) {
	return mix(henyeyGreenstein(lightDotEye, -0.2),
			   henyeyGreenstein(lightDotEye, 0.2),
			   clamp(lightDotEye*0.5 + 0.5, 0.0, 1.0));
}

#define BAYER_FACTOR 1.0/16.0
const float bayerFilter[16u] = float[]
(
	0.0*BAYER_FACTOR, 8.0*BAYER_FACTOR, 2.0*BAYER_FACTOR, 10.0*BAYER_FACTOR,
	12.0*BAYER_FACTOR, 4.0*BAYER_FACTOR, 14.0*BAYER_FACTOR, 6.0*BAYER_FACTOR,
	3.0*BAYER_FACTOR, 11.0*BAYER_FACTOR, 1.0*BAYER_FACTOR, 9.0*BAYER_FACTOR,
	15.0*BAYER_FACTOR, 7.0*BAYER_FACTOR, 13.0*BAYER_FACTOR, 5.0*BAYER_FACTOR
);

// Volumetric raymarching algorithm
vec4 volumetricRaymarch(in vec3 startRay, in vec3 endRay, in vec3 rayDir, in float stepSize, in vec2 t, in vec2 dt, in vec2 wt) {
	vec4 accumulation = vec4(0.0);

	int a = int(fragCoord.x) % 4;
	int b = int(fragCoord.y) % 4;
	startRay += rayDir * bayerFilter[a * 4 + b];

	float lightDotEye = dot(normalize(sunDir), normalize(rayDir));
	const float absorption = 0.01;
	float sigmaDS = -stepSize * absorption;
	float density;
	float transmittance = 1.0;

	// Integrate over the volume texture to determine transmittance
	for (int i = 0; i < MAX_STEPS; ++i) {

		// Snap next sample to view aligned plane
		vec4 data = t.x < t.y ? vec4( t.x, wt.x, dt.x, 0.0 ) : vec4( t.y, wt.y, 0.0, dt.y );
		vec3 pos = startRay + data.x * rayDir;
		float w = data.y;
		t += data.zw;

		// Get height fraction and cloud density
		float heightFrac = getHeightFraction(pos);
		float cloudDensity = sampleCloudDensity(pos, heightFrac, true);

		// If the density is above 0 calculate lighting
		if (cloudDensity > 0.0 + EPSILON) {
			float scattering = lightScattering(lightDotEye);
			float lightDensity = sampleCloudDensityAlongCone(
				pos,
				stepSize*0.1,
				lightDotEye,
				cloudDensity);

			vec3 ambientLight = ambientLight(heightFrac);
			float powderTerm = (1.0*0.25 + 0.75*powder(cloudDensity));

			vec3 source = 0.6 * (mix(ambientLight * 1.8, scattering * sunColor, powderTerm * lightDensity)) * cloudDensity;
			float deltaTrans = exp(cloudDensity * sigmaDS);
			vec3 sourceIntegral = (source - source * deltaTrans) * (1.0 / cloudDensity);

			accumulation.rgb += transmittance * sourceIntegral;
			transmittance *= deltaTrans;
	
			if(transmittance <= CLOUDS_MIN_TRANSMITTANCE) {
				break;
			}
		}
	}

	accumulation.a = 1.0 - transmittance;

	return accumulation;
}

// Plane Alignment
// From: https://github.com/huwb/volsample
//NOTE: This assumes the volume will only be UNIFORMLY scaled. Non uniform scale would require tons of little changes.
float mysign( float x ) { return x < 0. ? -1. : 1. ; }
vec2 mysign( vec2 x ) { return vec2( x.x < 0. ? -1. : 1., x.y < 0. ? -1. : 1. ) ; }

void planeAlignment(in vec3 ro, in vec3 rd, in float stepSize, out vec2 t, out vec2 dt, out vec2 wt) {
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

float computeFogAmount(in vec3 startPos, in float factor){
	float dist = length(startPos - cameraPos);
	float radius = (cameraPos.y - sphereCenter.y) * 0.3;
	float alpha = (dist / radius);

	return (1.0 - exp(-dist*alpha*factor));
}

vec3 computeClipSpaceCoord(){
	vec2 ray_nds = 2.0*fragCoord.xy/resolution.xy - 1.0;
	return vec3(ray_nds, 1.0);
}

vec2 computeScreenPos(vec2 ndc){
	return (ndc*0.5 + 0.5);
}

const int bayerMatrix16[16] = int[]
(
	0, 8, 2, 10,
	12, 4, 14, 6,
	3, 11, 1, 9,
	15, 7, 13, 5
);

bool writePixel() {
	int index = bayerMatrix16[frameIter];
	ivec2 icoord = ivec2(fragCoord.xy);
    return ((icoord.x + 4*icoord.y) % 16 == index);
}

float threshold(float v, float t) {
	return v > t ? v : 0.0;
}

void main() {
	// Check if texel is occluded by terrain -- early exit if true
	if (texture(terrainOcclusion, computeScreenPos(computeClipSpaceCoord().xy)).r > 0.0) {
		out_color = vec4(0.0, 0.0, 0.0, 0.0);
		cloudColor = vec4(0.0, 0.0, 0.0, 0.0);
		alphaness = vec4(vec3(texture(terrainOcclusion, computeScreenPos(computeClipSpaceCoord().xy)).r), 1.0);
		return;
	}

	// Compute Ray Direction
	vec4 rayClip = vec4(computeClipSpaceCoord(), 1.0);
	vec4 rayView = inverseProjection * rayClip;
	rayView = vec4(rayView.xy, -1.0, 0.0);
	vec3 worldDir = (inverseView * rayView).xyz;
	worldDir = normalize(worldDir);

	// For picking previous frame color -- temporal projection
	vec4 camToWorldPos = inverseViewProjection * rayClip;
	camToWorldPos /= camToWorldPos.w;
	vec4 pPrime = oldViewProjection * camToWorldPos;
	pPrime /= pPrime.w;
	vec2 prevFrameScreenPos = computeScreenPos(pPrime.xy); 
	bool isOut = any(greaterThan(abs(prevFrameScreenPos - 0.5) , vec2(0.5)));

	//compute raymarching starting and ending point by intersecting with spheres
	vec3 startPos, endPos;
	startPos = worldDir * raySphereIntersection(cameraPos, worldDir, SPHERE_INNER_RADIUS);
	endPos = worldDir * raySphereIntersection(cameraPos, worldDir, SPHERE_OUTER_RADIUS);

	// Compute fog amount -- early exit if fog is too large
	float fogAmount = computeFogAmount(cameraPos + startPos, 0.00002);
	if (fogAmount > 0.990) {
		out_color = vec4(0.0, 0.0, 0.0, 0.0);
		cloudColor = vec4(0.0, 0.0, 0.0, 0.0);
		alphaness = vec4(0.0, 0.0, 0.0, 1.0);
		return;
	}

	// Early exit -- search for low alphaness areas
	float oldFrameAlpha = 1.0;
	vec4 oldFrameTexel = texture(lastFrame, prevFrameScreenPos);

	if (!isOut) {
		oldFrameAlpha = texture(lastFrameAlphaness, prevFrameScreenPos).r;
	}

	// If the pixel must be recalculated
	vec4 color = vec4(0.0);
	if ((oldFrameAlpha >= 0.0 || frameIter == 0) && (writePixel() || isOut)) {
		// Raymarch
		float stepSize = length(endPos - startPos) / MAX_STEPS;
		vec2 t, dt, wt;
		planeAlignment(startPos, worldDir, stepSize, t, dt, wt);
		color = volumetricRaymarch(startPos, endPos, worldDir, stepSize, t, dt, wt);
		cloudColor = color; // Output untampered with cloud color to texture
							// for temporal reprojection
	} else {
		// Temporal reprojection
		color = texture(lastFrame, computeScreenPos(computeClipSpaceCoord().xy));
		cloudColor = color; // Output untampered with cloud color to texture
							// for temporal reprojection
	}

	color.rgb = color.rgb*1.8 - 0.1; // Constrast-illumination tuning

	float cloudAlphaness = threshold(color.a, 0.2);
	alphaness = vec4(cloudAlphaness, 0.0, 0.0, 1.0); // Output cloud alphaness to texture
	alphaness += texture(terrainOcclusion, computeScreenPos(computeClipSpaceCoord().xy)).r; // Add terrain occlusion to alphaness texture
	alphaness = clamp(alphaness, 0.0, 1.0);

	// add sun glare to clouds
	float sun = clamp( dot(-sunDir,normalize(endPos - startPos)), 0.0, 1.0 );
	vec3 s = 0.8 * vec3(1.0,0.4,0.2) * pow(sun, 256.0);
	color.rgb += s * color.a;

	out_color = color; // Output fragment color to screen
}