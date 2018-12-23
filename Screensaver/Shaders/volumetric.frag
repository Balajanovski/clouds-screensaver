#version 440 core

// Math constants
#define M_PI 3.1415926535897932384626433832795
#define EPSILON 0.01f

// Raymarching constants
#define MAX_SDF_STEPS 300.0
#define MAX_VOL_STEPS 60.0
#define MAX_DIST 100.0

// Cloud constants
#define CLOUD_RADIUS 1.2
#define REPETITION_PERIOD vec2(2.5)
#define SQUASH_FACTOR 2.0

#define fragCoord (gl_FragCoord.xy)

in vec3 vPos;

layout (location = 0) out vec4 out_color;

// Pregenerated layers of volume
uniform sampler3D volumeTexLayer1;
uniform sampler3D volumeTexLayer2;
uniform sampler3D volumeTexLayer3;

// Pregenerated layers of displacement
uniform sampler2D cloudDistLayer1;
uniform sampler2D cloudDistLayer2;

uniform float time;
uniform vec2 resolution;
uniform vec3 cameraPos;
uniform mat4 view;
uniform float fov;
#define FOV (fov / 180 * M_PI) // In radians


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

float sampleCloudMedium(in vec3 pos) {
	float value;
	
	// Distort pos by rotating it about the x axis
	// then sample
	mat4 xAxisRot = mat4(1, 0,			0,			 0,
					     0, cos(pos.x), -sin(pos.x), 0,
					     0, sin(pos.x), cos(pos.x),  0,
					     0, 0,          0,           1);
	vec3 tex1Sample = (xAxisRot * vec4(pos, 1.0)).xyz;
	value = texture(volumeTexLayer1, tex1Sample).r;

	// Distort pos by rotating it about the y axis
	// then sample
	mat4 yAxisRot = mat4(cos(pos.y), 0, sin(pos.y), 0,
					     0,          1, 0,          0,
					     -sin(pos.y), 0, cos(pos.y),  0,
					     0,           0, 0,           1);
	vec3 tex2Sample = (yAxisRot * vec4(pos, 1.0)).xyz;
	value = smoothstep(0.0, 1.0, texture(volumeTexLayer2, tex2Sample).r + value);

	// Distort pos by rotating it about the z axis
	// then sample
	mat4 zAxisRot = mat4(cos(pos.z), -sin(pos.z), 0, 0,
					     sin(pos.z), cos(pos.z),  0, 0,
					     0,          0,           1, 0,
					     0,          0,           0, 1);
	vec3 tex3Sample = (zAxisRot * vec4(pos, 1.0)).xyz;
	value = smoothstep(0.0, 1.0, texture(volumeTexLayer3, tex3Sample).r + value);
	
	return value;
}

float sampleCloudDistortion(in vec2 pos) {
	float value;
	
	// Distort pos by rotating it about the x axis
	// then sample
	mat3 xAxisRot = mat3(1, 0,			0,			
					     0, cos(pos.x), -sin(pos.x),
					     0, sin(pos.x), cos(pos.x));
	vec2 tex1Sample = (xAxisRot * vec3(pos, 1.0)).xy;
	value = texture(cloudDistLayer1, tex1Sample).r;

	// Distort pos by rotating it about the y axis
	// then sample
	mat3 yAxisRot = mat3(cos(pos.y), 0, sin(pos.y), 
					     0,          1, 0,          
					     -sin(pos.y), 0, cos(pos.y));
	vec2 tex2Sample = (yAxisRot * vec3(pos, 1.0)).xy;
	value = smoothstep(0.0, 1.0, texture(cloudDistLayer2, tex2Sample).r + value);

	return value;
}

float cloudSDF(in vec3 pos) {
	vec3 q = pos;
	q.xz = mod(q.xz - REPETITION_PERIOD, 5.0) - REPETITION_PERIOD; // Repeat over x & z
	q.y *= SQUASH_FACTOR;										   // Squash over y
	float dist = sphereSDF(q, CLOUD_RADIUS, vec3(0.0));

	pos.y -= time * 0.2;										   // Offset based on time
	dist += sampleCloudDistortion(pos.xz);					   // Distort based on noise

	return dist;
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
		float currentSample = sampleCloudMedium(p + rayDir*i*stepSize);
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