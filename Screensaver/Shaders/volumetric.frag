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
uniform float fov;
uniform vec3 sunDir;
#define FOV (fov / 180 * M_PI) // In radians

// ----------------------------
// Simplex noise
// Sourced from: https://www.shadertoy.com/view/Xsl3zr

vec3 mod289(vec3 x) {
  return x - floor(x * (1.0 / 289.0)) * 289.0;
}

vec4 mod289(vec4 x) {
  return x - floor(x * (1.0 / 289.0)) * 289.0;
}

vec4 permute(vec4 x) {
     return mod289(((x*34.0)+1.0)*x);
}

vec4 taylorInvSqrt(vec4 r)
{
  return 1.79284291400159 - 0.85373472095314 * r;
}

float snoise(vec3 v)
  { 
  const vec2  C = vec2(1.0/6.0, 1.0/3.0) ;
  const vec4  D = vec4(0.0, 0.5, 1.0, 2.0);

  // First corner
  vec3 i  = floor(v + dot(v, C.yyy) );
  vec3 x0 =   v - i + dot(i, C.xxx) ;

  // Other corners
  vec3 g = step(x0.yzx, x0.xyz);	  
  vec3 l = 1.0 - g;
  vec3 i1 = min( g.xyz, l.zxy );
  vec3 i2 = max( g.xyz, l.zxy );

  vec3 x1 = x0 - i1 + C.xxx;
  vec3 x2 = x0 - i2 + C.yyy; // 2.0*C.x = 1/3 = C.y
  vec3 x3 = x0 - D.yyy;      // -1.0+3.0*C.x = -0.5 = -D.y

  // Permutations
  i = mod289(i); 
  vec4 p = permute( permute( permute( 
             i.z + vec4(0.0, i1.z, i2.z, 1.0 ))
           + i.y + vec4(0.0, i1.y, i2.y, 1.0 )) 
           + i.x + vec4(0.0, i1.x, i2.x, 1.0 ));

  // Gradients: 7x7 points over a square, mapped onto an octahedron.
  // The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
  float n_ = 0.142857142857; // 1.0/7.0
  vec3  ns = n_ * D.wyz - D.xzx;

  vec4 j = p - 49.0 * floor(p * ns.z * ns.z);  //  mod(p,7*7)

  vec4 x_ = floor(j * ns.z);
  vec4 y_ = floor(j - 7.0 * x_ );    // mod(j,N)

  vec4 x = x_ *ns.x + ns.yyyy;
  vec4 y = y_ *ns.x + ns.yyyy;
  vec4 h = 1.0 - abs(x) - abs(y);

  vec4 b0 = vec4( x.xy, y.xy );
  vec4 b1 = vec4( x.zw, y.zw );

  vec4 s0 = floor(b0)*2.0 + 1.0;
  vec4 s1 = floor(b1)*2.0 + 1.0;
  vec4 sh = -step(h, vec4(0.0));

  vec4 a0 = b0.xzyw + s0.xzyw*sh.xxyy ;
  vec4 a1 = b1.xzyw + s1.xzyw*sh.zzww ;

  vec3 p0 = vec3(a0.xy,h.x);
  vec3 p1 = vec3(a0.zw,h.y);
  vec3 p2 = vec3(a1.xy,h.z);
  vec3 p3 = vec3(a1.zw,h.w);

  //Normalise gradients
  vec4 norm = taylorInvSqrt(vec4(dot(p0,p0), dot(p1,p1), dot(p2, p2), dot(p3,p3)));
  p0 *= norm.x;
  p1 *= norm.y;
  p2 *= norm.z;
  p3 *= norm.w;

  // Mix final noise value
  vec4 m = max(0.6 - vec4(dot(x0,x0), dot(x1,x1), dot(x2,x2), dot(x3,x3)), 0.0);
  m = m * m;
  return 42.0 * dot( m*m, vec4( dot(p0,x0), dot(p1,x1), 
                                dot(p2,x2), dot(p3,x3) ) );
}

// -----------------------------
// Fractal brownian motion noise
// Code sourced from: https://www.shadertoy.com/view/Xsl3zr

float fbm(vec3 p)
{
    float f;
    f = 0.5000*snoise( p ); p = p*2.02;
    f += 0.2500*snoise( p ); p = p*2.03;
    f += 0.1250*snoise( p ); p = p*2.01;
    f += 0.0625*snoise( p );
    return f;
}

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

float sampleCloudMedium(in vec3 pos) {
	return fbm(pos);
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