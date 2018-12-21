#version 440 core

// Math constants
#define M_PI 3.1415926535897932384626433832795
#define EPSILON 0.01f

// Raymarching constants
#define MAX_SDF_STEPS 300.0
#define MAX_VOL_STEPS 60.0
#define MAX_DIST 100.0
#define FOV (M_PI / 2.0) // In radians

#define fragCoord (gl_FragCoord.xy)

in vec3 vPos;

layout (location = 0) out vec4 out_color;

uniform sampler3D volumeTexture;
uniform sampler2D cloudDistortion;

uniform float time;
uniform vec2 resolution;
uniform vec3 cameraPos;


// Signed-distance function of a sphere
float sphereSDF(in vec3 pos, in float radius, in vec3 center) {
    return length(pos + center) - radius;
}

// Determine the unit vector to march along
vec3 rayDirection(in float fieldOfView, in vec2 size, in vec2 frag_coord) {
    vec2 xy = frag_coord - size / 2.0;
    float z = size.y / tan(fieldOfView / 2.0);
    return normalize(vec3(xy, -z));
}

float cloudSDF(in vec3 pos) {
	return sphereSDF(pos, 1.0, vec3(0, 0, 0));
}

// The raymarching algorithm
// -------------------------
// March along a ray by the distance to the nearest object
// until that distance approaches zero (collision)
// or it exceeds the max steps or max distance
float raymarch(in vec3 eye, in vec3 ray_dir) {
    float depth = 0.0;
    for (int i = 0; i < MAX_SDF_STEPS; ++i) {
        float d = cloudSDF(eye + depth * ray_dir);
        if (d < EPSILON) {
            return depth;
        }
        depth += d;
        if (depth >= MAX_DIST) {
            return MAX_DIST;
        }
    }
    return MAX_DIST;
}

vec3 calculateColor(vec3 p, vec3 rayDir) {
	float accumdist = 0;
	float stepSize = 1.0 / MAX_VOL_STEPS;

	// Integrate over the volume texture to determine opacity
	for (int i = 0; i < MAX_VOL_STEPS; ++i) {
		float currentSample = texture(volumeTexture, p + rayDir*i*stepSize).r;
		accumdist += currentSample * stepSize;
	}

	return vec3(accumdist);
}

void main() {
	vec3 rayDir = rayDirection(FOV, resolution, fragCoord);
	float dist = raymarch(cameraPos, rayDir);

	vec3 p = cameraPos + dist * rayDir;

	if (dist > MAX_DIST - EPSILON) {
		out_color = vec4(0.0, 0.0, 0.0, 0.0);
	} else {
		out_color = vec4(calculateColor(p, rayDir), 1.0);
	}
}