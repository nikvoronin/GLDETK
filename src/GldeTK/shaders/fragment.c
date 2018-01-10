#version 330 core

uniform float iGlobalTime;
uniform vec3 iResolution;
uniform vec3 ro;	// camera ray origin
uniform mat3 camProj;	// camera projection matrix

uniform SdElements
{
	vec4 g_map[256];
};

varying vec2 fragCoord;

float sdPlaneY(vec3 p)
{
	return p.y;
}

float sdSphere(vec3 p, float s)
{
	return length(p) - s;
}

float sdBox(vec3 p, vec3 b)
{
	vec3 d = abs(p) - b;
	return min(max(d.x, max(d.y, d.z)), 0.0) + length(max(d, 0.0));
}

float sdCylinder(vec3 p, float r, float height) {
	float d = length(p.xz) - r;
	d = max(d, abs(p.y) - height);
	return d;
}

float sdCylinderInf(vec3 p, float r) {
	return  length(p.xz) - r;
}

//----------------------------------------------------------------------

float opA(float d1, float d2)
{
	return min(d2, d1);
}

vec3 opRep(vec3 p, vec3 c)
{
	return mod(p, c) - 0.5 * c;
}

//----------------------------------------------------------------------

float map(in vec3 pos)
{
	float d = sdPlaneY(pos);

	d =
		opA(
			d,
			sdSphere(pos, 2.0));

	//vec3 prep = opRep(pos, vec3(10, 10.0 + sin(iGlobalTime), 10));

	//d =
	//	opA(
	//		d,
	//		sdSphere(prep, g_map[0].x));
	//
	//prep = opRep(pos, vec3(7.0, 0.0, 9.0));

	//d =
	//	opA(
	//		d,
	//		sdBox(prep, g_map[0].yzw));

	//prep = opRep(pos, vec3(12.0, 0.0, 13.0));

	//d =
	//	opA(
	//		d,
	//		sdCylinder(prep, 1.0, 30.0));

	return d;
}

float castRay(in vec3 ro, in vec3 rd)
{
	//TODO move to external constants w/ uniq names
	const float MAX_DIST = 100;
	const float MIN_DIST = 0.0002;
	const int MAX_RAY_STEPS = 100;

	float t = 0.0;
	float overstep = 0.0;
	float phx = MAX_DIST;

	int i = 0;
	while (i < MAX_RAY_STEPS && t < MAX_DIST)
	{
		float d = map(ro + rd * t);

		if (d > overstep)
		{
			overstep = d * min(1.0, 0.5 * d / phx);
			t += d * 0.5 + overstep;
			phx = d;
			i++;
		}
		else
		{
			t -= overstep;
			phx = MAX_DIST;
			d = 1.0;
			overstep = 0.0;
		}

		if (d < MIN_DIST || t > MAX_DIST)
			break;
	}

	return t;
}

// TODO generalize over map() it similar
float softshadow(in vec3 ro, in vec3 rd)
{
	const float INIT_T = 0.02;
	const float INIT_RES = 0.1;
	const float MAX_DIST = 25;
	const float MIN_DIST = 0.001;
	const int MAX_RAY_STEPS = 64;		// higher -> longer shadow distance
	const float SHADOW_SMOOTH = 8.0;	// lower ~ smother, higher -> sharper

	float shade = 1.0;
	float t = INIT_T;
	int i = 0;
	while (i < MAX_RAY_STEPS && t < MAX_DIST)
	{
		float d = map(ro + rd * t);

		shade = min(shade, SHADOW_SMOOTH * d / t);
		t += clamp(d, INIT_T, INIT_RES);

		if (d < MIN_DIST || t > MAX_DIST)
			break;
		i++;
	}

	return clamp(shade, 0.0, 1.0);
}

vec3 calcNormal(in vec3 pos)
{
	vec3 eps = vec3(0.001, 0.0, 0.0);
	vec3 nor = vec3(
		map(pos + eps.xyy) - map(pos - eps.xyy),
		map(pos + eps.yxy) - map(pos - eps.yxy),
		map(pos + eps.yyx) - map(pos - eps.yyx));
	return normalize(nor);
}

vec3 render(in vec3 ro, in vec3 rd)
{
	vec3 col = vec3(1.0);
	float t = castRay(ro, rd);
	vec3 pos = ro + t * rd;
	vec3 nor = calcNormal(pos);
	vec3 ref = reflect(rd, nor);

	// lighitng        
	vec3  lig = normalize(vec3(cos(iGlobalTime *0.1), abs(sin(iGlobalTime *0.1)), cos(iGlobalTime *0.1) * sin(iGlobalTime *0.1)));
	float amb = clamp(0.5 + 0.5 * nor.y, 0.0, 1.0);
	float dif = clamp(dot(nor, lig), 0.0, 1.0);
	float spe = pow(clamp(dot(ref, lig), 0.0, 1.0), 16.0);

	dif *= softshadow(pos, lig);

	vec3 lin = vec3(0.0);
	lin += dif;
	lin += 1.20 * spe *dif;
	lin += 0.20 * amb;
	col *= lin;

	col = mix(col, vec3(0.8, 0.9, 1.0), 1.0 - exp(-0.002 * t * t));	// distance fog

	return vec3(clamp(col, 0.0, 1.0));
}

void main(void)
{
	// ray direction
	vec2 q = fragCoord.xy / iResolution.xy;
	vec2 p = -1.0 + 2.0 * q;
	p.x *= iResolution.x / iResolution.y;
	vec3 rd = camProj * normalize(vec3(p.xy, 2.0));

	vec3 col = render(ro, rd);

	col = pow(col, vec3(0.8545)); // tint
	gl_FragColor = vec4(col, 1.0);
}