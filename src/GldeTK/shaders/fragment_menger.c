﻿#version 430

uniform float iGlobalTime;
uniform vec3 iResolution;
uniform vec3 PlayerPos;
uniform vec4 iMouse;

varying vec2 fragCoord;
out vec4 fragColor;		// shadertoy compatibility

						// Created by inigo quilez - iq/2013
						// License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.

						// http://www.iquilezles.org/www/articles/menger/menger.htm

float maxcomp(in vec3 p) { return max(p.x, max(p.y, p.z)); }
float sdBox(vec3 p, vec3 b)
{
	vec3  di = abs(p) - b;
	float mc = maxcomp(di);
	return min(mc, length(max(di, 0.0)));
}

const mat3 ma = mat3(0.60, 0.00, 0.80,
	0.00, 1.00, 0.00,
	-0.80, 0.00, 0.60);

vec3 opRep(vec3 p, vec3 c)
{
	return mod(p, c) - 0.5 * c;
}

vec4 map(in vec3 p)
{
	p = opRep(p, vec3(4));

	float d = sdBox(p, vec3(1.0));
	vec4 res = vec4(d, 1.0, 0.0, 0.0);

	float ani = 0;// smoothstep(-0.2, 0.2, -cos(0.5*iGlobalTime));
	float off = 0;// 1.5*sin(0.01*iGlobalTime);

	float s = 1.0;
	for (int m = 0; m < 3; m++)
	{
		p = mix(p, ma*(p + off), ani);

		vec3 a = mod(p*s, 2.0) - 1.0;
		s *= 3.0;
		vec3 r = abs(1.0 - 3.0*abs(a));
		float da = max(r.x, r.y);
		float db = max(r.y, r.z);
		float dc = max(r.z, r.x);
		float c = (min(da, min(db, dc)) - 1.0) / s;

		if (c>d)
		{
			d = c;
			res = vec4(d, min(res.y, 0.2*da*db*dc), (1.0 + float(m)) / 4.0, 0.0);
		}
	}

	return res;
}

//vec4 intersect(in vec3 ro, in vec3 rd)
//{
//	const float far = 100;
//	float t = 0.0;
//	vec4 res = vec4(-1.0);
//	vec4 h = vec4(1.0);
//	for (int i = 0; i < 64; i++)
//	{
//		if (h.x<0.002 || t>far) break;
//		h = map(ro + rd*t);
//		res = vec4(t, h.yzw);
//		t += h.x;
//	}
//	if (t>far) res = vec4(-1.0);
//	return res;
//}

vec4 intersect(in vec3 ro, in vec3 rd)
{
	const float MAX_DIST = 100;
	const float MIN_DIST = 0.002;
	const float NONE = -1.0;

	float t = 0.0;
	vec4 res = vec4(-1.0);
	vec4 h = vec4(1.0);
	float overstep = 0.0;
	float phx = MAX_DIST;

	for (int i = 0; i < 64; i++)
	{
		if (h.x < MIN_DIST || t > MAX_DIST)
			break;

		h = map(ro + rd * t);

		if (h.x > overstep)
		{
			overstep = h.x * min(1.0, 0.5 * h.x / phx);
			t += h.x + overstep;
			phx = h.x;
		}
		else
		{
			t -= overstep;
			phx = MAX_DIST;
			h.x = 1.0;
			overstep = 0.0;
		}

		res = vec4(t, h.yzw);
	}
	
	if (t > MAX_DIST)
		res = vec4(NONE);
	
	return res;
}

float softshadow(in vec3 ro, in vec3 rd, float mint, float k)
{
	float res = 1.0;
	float t = mint;
	float h = 1.0;
	for (int i = 0; i<32; i++)
	{
		h = map(ro + rd*t).x;
		res = min(res, k*h / t);
		t += clamp(h, 0.005, 0.1);
	}
	return clamp(res, 0.0, 1.0);
}

vec3 calcNormal(in vec3 pos)
{
	vec3  eps = vec3(.001, 0.0, 0.0);
	vec3 nor;
	nor.x = map(pos + eps.xyy).x - map(pos - eps.xyy).x;
	nor.y = map(pos + eps.yxy).x - map(pos - eps.yxy).x;
	nor.z = map(pos + eps.yyx).x - map(pos - eps.yyx).x;
	return normalize(nor);
}

// light
vec3 light = normalize(vec3(1.0, 0.9, 0.3));

vec3 render(in vec3 ro, in vec3 rd)
{
	// background color
	vec3 col = mix(vec3(0.3, 0.2, 0.1)*0.5, vec3(0.7, 0.9, 1.0), 0.5 + 0.5*rd.y);

	vec4 tmat = intersect(ro, rd);
	if (tmat.x > 0.0)
	{
		vec3  pos = ro + tmat.x*rd;
		vec3  nor = calcNormal(pos);

		float occ = tmat.y;
		float sha = softshadow(pos, light, 0.01, 64.0);

		float dif = max(0.1 + 0.9*dot(nor, light), 0.0);
		float sky = 0.5 + 0.5*nor.y;
		float bac = max(0.4 + 0.6*dot(nor, vec3(-light.x, light.y, -light.z)), 0.0);

		vec3 lin = vec3(0.0);
		lin += 1.00*dif*vec3(1.10, 0.85, 0.60)*sha;
		lin += 0.50*sky*vec3(0.10, 0.20, 0.40)*occ;
		lin += 0.10*bac*vec3(1.00, 1.00, 1.00)*(0.5 + 0.5*occ);
		lin += 0.25*occ*vec3(0.15, 0.17, 0.20);

		vec3 matcol = vec3(
			0.5 + 0.5*cos(0.0 + 2.0*tmat.z),
			0.5 + 0.5*cos(1.0 + 2.0*tmat.z),
			0.5 + 0.5*cos(2.0 + 2.0*tmat.z));
		col = matcol * lin;
	}

	return pow(col, vec3(0.4545));
}

void main(void)
{
	vec2 p = -1.0 + 2.0 * fragCoord.xy / iResolution.xy;
	p.x *= iResolution.x / iResolution.y;

	float ctime = iGlobalTime;
	// camera
	vec3 ro =1.1*vec3( 2.5, 1.0 + 1.0*cos(ctime*.13), 2.5);
	//vec3 ro =1.1*vec3(2.5*sin(0.25*ctime), 1.0 + 1.0*cos(ctime*.13), 2.5*cos(0.25*ctime));
	vec3 ww = normalize(vec3(0.0) - ro);
	vec3 uu = normalize(cross(vec3(0.0, 1.0, 0.0), ww));
	vec3 vv = normalize(cross(ww, uu));
	vec3 rd = normalize(p.x*uu + p.y*vv + 2.5*ww);

	vec3 col = render(ro, rd);

	fragColor = vec4(col, 1.0);
}