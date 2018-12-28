#version 440 core

// Math constants
#define M_PI 3.1415926535897932384626433832795
#define EPSILON 0.01f

// Raymarching constants
#define MAX_STEPS 64.0

// Cloud constants
#define CLOUD_RADIUS 1.5
#define REPETITION_PERIOD vec2(2.5)
#define SQUASH_FACTOR 2.0
#define AMPLITUDE_FACTOR 0.707;
#define FREQUENCY_FACTOR 2.5789;
#define OPACITY_THRESHOLD 0.95
#define DENSITY 0.1
#define INNER_COLOR vec4(0.7, 0.7, 0.7, DENSITY)
#define OUTER_COLOR vec4(1.0, 1.0, 1.0, 0.0)

// Noise constants
#define NUM_OCTAVES 5
#define DISTORTION_NOISE_FREQ 0.5
#define DISTORTION_NOISE_AMP 2.0

#define fragCoord (gl_FragCoord.xy)

in vec3 vPos;

layout (location = 0) out vec4 out_color;

uniform float time;
uniform vec2 resolution;
uniform vec3 cameraPos;
uniform mat4 view;
uniform vec3 sunDir; // TODO : Pass in sun parameters
uniform float fov;
#define FOV (fov / 180 * M_PI) // In radians

uniform sampler3D noiseLayer1;
uniform sampler3D noiseLayer2;
uniform sampler3D noiseLayer3;

// ---------------------------------
// Signed Distance Field Raymarching
// ---------------------------------

// Signed-distance function of a sphere
// Sourced from: https://iquilezles.org/www/articles/distfunctions/distfunctions.htm
float sphereSDF(in vec3 pos, in float radius, in vec3 center) {
    return length(pos + center) - radius;
}

// Signed-distance function of a box
// Sourced from: https://iquilezles.org/www/articles/distfunctions/distfunctions.htm
float boxSDF(in vec3 pos, in vec3 boxDimensions) {
	vec3 d = abs(pos) - boxDimensions;
	return length(max(d,0.0))
         + min(max(d.x,max(d.y,d.z)),0.0);
}

// Determine the unit vector to march along
vec3 rayDirection(in float fieldOfView, in vec2 size, in vec2 frag_coord) {
    vec2 xy = frag_coord - size / 2.0;
    float z = size.y / tan(fieldOfView / 2.0);
    return normalize(vec3(xy, -z));
}

float sampleCloudDistortion(in vec3 pos);

float cloudSDF(in vec3 pos) {
	pos.x -= time;

	vec3 q = pos;
	q.xz = mod(q.xz - REPETITION_PERIOD, 5.0) - REPETITION_PERIOD; // Repeat over x & z
	q.y *= SQUASH_FACTOR;										   // Squash over y
	float dist = sphereSDF(q, CLOUD_RADIUS, vec3(0.0));

	pos.y -= time * 0.3;										   // Offset based on time
	dist += sampleCloudDistortion(pos);							   // Distort based on noise

	return dist;
}

// ---------------------
// Sampling cloud volume
// ---------------------

float fbm(in vec3 pos) {
	return mix(texture(noiseLayer1, pos).r, 
			   texture(noiseLayer2, pos).r, 
			   texture(noiseLayer3, pos).r);
}

float sampleCloudDistortion(in vec3 pos) {
	return fbm(pos * DISTORTION_NOISE_FREQ) * DISTORTION_NOISE_AMP;
}

// Map distance to color
// Sourced from: https://www.shadertoy.com/view/Xsl3zr
vec4 shade(float d) {	
	return mix(INNER_COLOR, OUTER_COLOR, smoothstep(0.5, 1.0, d));
}

// Maps position to color
// Sourced from: https://www.shadertoy.com/view/Xsl3zr
vec4 volumeFunc(vec3 pos) {
	float dist = cloudSDF(pos);
	vec4 color = shade(dist);
	color.rgb *= smoothstep(-1.0, 0.0, pos.y)*0.5+0.5;	// Emulate shadows
	float r = length(pos)*0.04;
	color.a *= exp(-r*r);								// Fog
	return color;
}

// Samples Light
// Sourced from: https://www.shadertoy.com/view/Xsl3zr
float sampleLight(in vec3 pos)
{
	const int LightSteps = 8;
	const float ShadowDensity = 1.0;
	vec3 lightStep = (sunDir * 2.0) / float(LightSteps);
	float t = 1.0;	// transmittance
	for(int i=0;  i < LightSteps; ++i) {
		vec4 col = volumeFunc(pos);
		t *= max(0.0, 1.0 - col.a * ShadowDensity);
		//if (t < 0.01)
			//break;
		pos += lightStep;
	}

	return t;
}


// Volumetric raymarching algorithm
vec3 volumetricRaymarch(in vec3 p, in vec3 rayDir, in float stepSize) {
	vec4 accumulation = vec4(0.0);

	// Integrate over the volume texture to determine opacity
	for (int i = 0; i < MAX_STEPS; ++i) {
		vec4 color = volumeFunc(p + rayDir*i*stepSize);

		color.rgb *= color.a;
		accumulation += color * (1.0 - accumulation.a);

		if (accumulation.a > OPACITY_THRESHOLD) {
			break;
		}
	}

	return accumulation;
}

// Box-intersection test for bounding the clouds
// Sourced from: https://www.shadertoy.com/view/Xsl3zr
bool
intersectBox(vec3 ro, vec3 rd, vec3 boxmin, vec3 boxmax, out float tnear, out float tfar)
{
	// compute intersection of ray with all six bbox planes
	vec3 invR = 1.0 / rd;
	vec3 tbot = invR * (boxmin - ro);
	vec3 ttop = invR * (boxmax - ro);
	// re-order intersections to find smallest and largest on each axis
	vec3 tmin = min (ttop, tbot);
	vec3 tmax = max (ttop, tbot);
	// find the largest tmin and the smallest tmax
	vec2 t0 = max (tmin.xx, tmin.yz);
	tnear = max (t0.x, t0.y);
	t0 = min (tmax.xx, tmax.yz);
	tfar = min (t0.x, t0.y);
	// check for hit
	bool hit;
	if ((tnear > tfar)) 
		hit = false;
	else
		hit = true;
	return hit;
}

void main() {
	vec3 rayDir = rayDirection(FOV, resolution, fragCoord);

	// Bound the clouds
	vec3 boxMin = vec3(-50.0, 2.0, -50.0);
	vec3 boxMax = vec3(50.0, -2.0, 50.0);
	float tNear, tFar;
	bool hit = intersectBox(cameraPos, rayDir, boxMin, boxMax, tNear, tFar);
	tNear = max(tNear, 0.0);
	tFar = max(tFar, 0.0);

	if (hit) {
		vec3 pNear = cameraPos + rayDir*tNear;
		vec3 pFar = cameraPos + rayDir*tFar;

		float stepSize = length(pFar - pNear) / MAX_STEPS;

		out_color = vec4(volumetricRaymarch(pNear, rayDir, stepSize), 1.0);
	} else {
		out_color = vec4(0.0, 0.0, 0.0, 0.0);
	}
}