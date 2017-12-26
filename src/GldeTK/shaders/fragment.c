#version 330 core

uniform float iGlobalTime;
uniform vec3 iResolution;
uniform vec3 CamRo;
uniform vec3 CamTa;

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

vec2 map(in vec3 pos)
{
	vec2 res = vec2(sdPlaneY(pos), 1.0);

	vec3 prep = opRep(pos, vec3(10.0));

	res.x =
		opA(
			res.x,
			sdSphere(prep, g_map[0].x));
	
	prep = opRep(pos, vec3(7.0));

	res.x =
		opA(
			res.x,
			sdBox(prep, g_map[0].yzw));

	res.y = 45.0;

	return res;
}

vec2 castRay(in vec3 ro, in vec3 rd)
{
	//TODO move to external constants w/ uniq names
	const float MAX_DIST = 100;
	const float MIN_DIST = 0.0002;
	const int MAX_RAY_STEPS = 100;

	float t = 0.0;
	vec2 h = vec2(1.0);
	float overstep = 0.0;
	float phx = MAX_DIST;

	for (int i = 0; i < MAX_RAY_STEPS; i++)
	{
		if (h.x < MIN_DIST || t > MAX_DIST)
			break;

		h = map(ro + rd * t);

		if (h.x > overstep)
		{
			overstep = h.x * min(1.0, 0.5 * h.x / phx);
			t += h.x * 0.5 + overstep;
			phx = h.x;
		}
		else
		{
			t -= overstep;
			phx = MAX_DIST;
			h.x = 1.0;
			overstep = 0.0;
		}
	}

	if (t > MAX_DIST)
		h.y = -1.0;

	return vec2(t, h.y);
}

// TODO generalize over map() it similar
float softshadow(in vec3 ro, in vec3 rd, in float mint, in float tmax)
{
	float res = 1.0;
	float t = mint;
	for (int i = 0; i < 64; i++)
	{
		float h = map(ro + rd*t).x;
		res = min(res, 8.0*h / t);
		t += clamp(h, 0.02, 0.10);
		if (h<0.0002 || t>tmax) break;
	}
	return clamp(res, 0.0, 1.0);

}

vec3 calcNormal(in vec3 pos)
{
	vec3 eps = vec3(0.001, 0.0, 0.0);
	vec3 nor = vec3(
		map(pos + eps.xyy).x - map(pos - eps.xyy).x,
		map(pos + eps.yxy).x - map(pos - eps.yxy).x,
		map(pos + eps.yyx).x - map(pos - eps.yyx).x);
	return normalize(nor);
}

vec3 render(in vec3 ro, in vec3 rd)
{
	vec3 col = vec3(1.0);
	vec2 res = castRay(ro, rd);
	float t = res.x;
	float m = res.y;
	if (m > -0.5)
	{
		vec3 pos = ro + t * rd;
		vec3 nor = calcNormal(pos);
		vec3 ref = reflect(rd, nor);

		// lighitng        
		vec3  lig = normalize(vec3(cos(iGlobalTime *0.1), abs(sin(iGlobalTime *0.1)), cos(iGlobalTime *0.1) * sin(iGlobalTime *0.1)));
		//vec3  lig = normalize(vec3(-0.6, 0.7, -0.5));
		float amb = clamp(0.5 + 0.5 * nor.y, 0.0, 1.0);
		float dif = clamp(dot(nor, lig), 0.0, 1.0);
		float spe = pow(clamp(dot(ref, lig), 0.0, 1.0), 16.0);

		dif *= softshadow(pos, lig, 0.02, 25);

		vec3 lin = vec3(0.0);
		lin += dif;
		lin += 1.20 * spe *dif;
		lin += 0.20 * amb;
		col *= lin;

		col = mix(col, vec3(0.8, 0.9, 1.0), 1.0 - exp(-0.002 * t * t));	// distance fog
	}

	return vec3(clamp(col, 0.0, 1.0));
}

mat3 setCamera(in vec3 ro, in vec3 ta)
{
	const vec3 up = vec3(0.0, 1.0, 0.0);

	vec3 cw = normalize(ta - ro);
	vec3 cu = normalize(cross(cw, up));
	vec3 cv = normalize(cross(cu, cw));

	return mat3(cu, cv, cw);
}

void main(void)
{
	// camera	
	vec3 ro = CamRo;
	vec3 ta = CamTa;

	// camera-to-world transformation
	mat3 ca = setCamera(ro, ta);

	// ray direction
	vec2 q = fragCoord.xy / iResolution.xy;
	vec2 p = -1.0 + 2.0 * q;
	p.x *= iResolution.x / iResolution.y;

	vec3 rd = ca * normalize(vec3(p.xy, 2.0));

	// render	
	vec3 col = render(ro, rd);

	// tint
	col = pow(col, vec3(0.8545));

	gl_FragColor = vec4(col, 1.0);
}