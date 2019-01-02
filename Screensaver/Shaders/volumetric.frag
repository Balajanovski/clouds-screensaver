#version 440 core

// Math constants
#define M_PI 3.1415926535897932384626433832795
#define EPSILON 0.01f

// Raymarching constants
#define MAX_STEPS 16.0

// Cloud constants
#define OPACITY_THRESHOLD 0.98
#define COVERAGE 60.0

#define fragCoord (gl_FragCoord.xy)

in vec3 vPos;

layout (location = 0) out vec4 out_color;

uniform float time;
uniform vec2 resolution;
uniform vec3 cameraPos;
uniform mat4 view;
//uniform vec3 sunDir; // TODO : Pass in sun parameters
const vec3 sunDir = normalize( vec3(-1.0,0.0,0.0) );
uniform float fov;
#define FOV (fov / 180 * M_PI) // In radians

uniform sampler3D simplexNoise;

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

// Sample pregenerated simplex noise
float snoise(in vec3 pos) {
	return texture(simplexNoise, pos*0.005).r;
}

// -----------------------------
// Fractal brownian motion noise
// Code sourced from: https://www.shadertoy.com/view/Xsl3zr

float fbm(in vec3 p) {
    float f;
	vec3 q = p - vec3(0.0,0.1,1.0)*time*-0.5;
    f = 0.5000*snoise(q); q = q*2.02 ;
    f += 0.2500*snoise(q); q = q*2.03;
    f += 0.1250*snoise(q); q = q*2.01;
    f += 0.0625*snoise(q);
    return f;
}

// ---------------------
// Sampling cloud volume
// ---------------------

vec4 map(in vec3 p) {
	float d = 0.1 + 0.8 * sin(0.6*p.z)*sin(0.5*p.x) - p.y;

    vec3 q = p;
    float f = fbm(q);
    d += 2.75 * f;

    d = clamp( d, 0.0, 1.0 );
    
    vec4 res = vec4( d );
    
    vec3 col = 1.15 * vec3(1.0,0.95,0.8);
    col += vec3(1.,0.,0.) * exp2(res.x*10.-10.);
    res.xyz = mix( col, vec3(0.7,0.7,0.7), res.x );
    
    return res;
}

// Utility function that maps a value from one range to another .
/*float Remap ( float original_value , float original_min , float original_max , float new_min , float new_max ) {
    return new_min + ( ( ( original_value ? original_min) / ( original_max ? original_min ) ) ? ( new_max ? new_min ) );
}*/

// Volumetric raymarching algorithm
vec4 volumetricRaymarch(in vec3 p, in vec3 rayDir, in float stepSize, in vec2 t, in vec2 dt, in vec2 wt, in vec3 endRay) {
	vec4 accumulation = vec4(0.0);

	// Fade samples at far extent
    float f = 0.6; 
    float endFade = f*float(MAX_STEPS)*stepSize;
    float startFade = .8*endFade;

	// Integrate over the volume texture to determine opacity
	// Sample min(MAX_STEPS, totalSamples) values from noise
	int samplePositions[int(MAX_STEPS)];
	vec4 samples[int(MAX_STEPS)];
	for (int i = 0; i < MAX_STEPS; ++i) {
		// data for next sample
		vec4 data = t.x < t.y ? vec4( t.x, wt.x, dt.x, 0.0 ) : vec4( t.y, wt.y, 0.0, dt.y );
		vec3 pos = p + data.x * rayDir;
		float w = data.y;
		t += data.zw;

		// If the sampling position is out of the bounding box, end the loop
		if (length(pos - p) > length(endRay - p) - EPSILON) {
			break;
		}
        
		// fade samples at far extent
		w *= smoothstep(endFade, startFade, data.x);

		vec4 color = map(pos);

		// Inigo Quilez's Cloud Shading
		// Sourced from: https://www.shadertoy.com/view/XslGRr
		float dif = clamp((color.w - map(pos+0.6*sunDir).w)/0.6, 0.0, 1.0 );
		vec3 lin = vec3(0.51, 0.53, 0.63)*1.35 + 0.55*vec3(0.85, 0.57, 0.3)*dif;
		color.rgb *= lin;
		color.rgb *= color.rgb;
		color.a *= 0.75;
		color.rgb *= color.a;

		samples[i] = color * (1.0 - accumulation.a) * w;
		samplePositions[i] = int(length(p - pos));
		accumulation += samples[i];

		if (accumulation.a > OPACITY_THRESHOLD - EPSILON) {
			return accumulation;
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

	// Bind the clouds
	vec3 boxMin = vec3(-50.0, 10.0, -50.0);
	vec3 boxMax = vec3(50.0, -10.0, 100.0);
	float tNear, tFar;
	bool hit = intersectBox(cameraPos, rayDir, boxMin, boxMax, tNear, tFar);
	tNear = max(tNear, 0.0);
	tFar = max(tFar, 0.0);

	if (hit) {
		vec3 pNear = cameraPos + rayDir*tNear;
		vec3 pFar = cameraPos + rayDir*tFar;

		float stepSize = length(pFar - pNear) / min(MAX_STEPS, length(pFar - pNear));

		vec2 t, dt, wt;
		planeAlignment(pNear, rayDir, stepSize, t, dt, wt);

		out_color = volumetricRaymarch(pNear, rayDir, stepSize, t, dt, wt, pFar);
	} else {
		out_color = vec4(0.0, 0.0, 0.0, 0.0);
	}
}