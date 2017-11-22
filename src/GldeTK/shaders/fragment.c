#version 430

uniform float iGlobalTime;
uniform vec3 iResolution;
uniform vec3 CamRo;
uniform vec3 CamTa;

varying vec2 fragCoord;

// Created by inigo quilez - iq/2013
// License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.

// A list of usefull distance function to simple primitives, and an example on how to 
// do some interesting boolean operations, repetition and displacement.
//
// More info here: http://www.iquilezles.org/www/articles/distfunctions/distfunctions.htm

float sdPlane(vec3 p)
{
	return p.y;
}

float sdPlaneX(vec3 p)
{
	return p.x;
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

float sdEllipsoid(in vec3 p, in vec3 r)
{
	return (length(p / r) - 1.0) * min(min(r.x, r.y), r.z);
}

float udRoundBox(vec3 p, vec3 b, float r)
{
	return length(max(abs(p) - b, 0.0)) - r;
}

float sdTorus(vec3 p, vec2 t)
{
	return length(vec2(length(p.xz) - t.x, p.y)) - t.y;
}

float sdHexPrism(vec3 p, vec2 h)
{
	vec3 q = abs(p);
#if 0
	return max(q.z - h.y, max((q.x*0.866025 + q.y*0.5), q.y) - h.x);
#else
	float d1 = q.z - h.y;
	float d2 = max((q.x*0.866025 + q.y*0.5), q.y) - h.x;
	return length(max(vec2(d1, d2), 0.0)) + min(max(d1, d2), 0.);
#endif
}

float sdCapsule(vec3 p, vec3 a, vec3 b, float r)
{
	vec3 pa = p - a, ba = b - a;
	float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
	return length(pa - ba*h) - r;
}

float sdTriPrism(vec3 p, vec2 h)
{
	vec3 q = abs(p);
#if 0
	return max(q.z - h.y, max(q.x*0.866025 + p.y*0.5, -p.y) - h.x*0.5);
#else
	float d1 = q.z - h.y;
	float d2 = max(q.x*0.866025 + p.y*0.5, -p.y) - h.x*0.5;
	return length(max(vec2(d1, d2), 0.0)) + min(max(d1, d2), 0.);
#endif
}

float sdCylinder(vec3 p, vec2 h)
{
	vec2 d = abs(vec2(length(p.xz), p.y)) - h;
	return min(max(d.x, d.y), 0.0) + length(max(d, 0.0));
}

float sdCone(in vec3 p, in vec3 c)
{
	vec2 q = vec2(length(p.xz), p.y);
	float d1 = -q.y - c.z;
	float d2 = max(dot(q, c.xy), q.y);
	return length(max(vec2(d1, d2), 0.0)) + min(max(d1, d2), 0.);
}

float sdConeSection(in vec3 p, in float h, in float r1, in float r2)
{
	float d1 = -p.y - h;
	float q = p.y - h;
	float si = 0.5*(r1 - r2) / h;
	float d2 = max(sqrt(dot(p.xz, p.xz)*(1.0 - si*si)) + q*si - r2, q);
	return length(max(vec2(d1, d2), 0.0)) + min(max(d1, d2), 0.);
}


float length2(vec2 p)
{
	return sqrt(p.x*p.x + p.y*p.y);
}

float length6(vec2 p)
{
	p = p*p*p; p = p*p;
	return pow(p.x + p.y, 1.0 / 6.0);
}

float length8(vec2 p)
{
	p = p*p; p = p*p; p = p*p;
	return pow(p.x + p.y, 1.0 / 8.0);
}

float sdTorus82(vec3 p, vec2 t)
{
	vec2 q = vec2(length2(p.xz) - t.x, p.y);
	return length8(q) - t.y;
}

float sdTorus88(vec3 p, vec2 t)
{
	vec2 q = vec2(length8(p.xz) - t.x, p.y);
	return length8(q) - t.y;
}

float sdCylinder6(vec3 p, vec2 h)
{
	return max(length6(p.xz) - h.x, abs(p.y) - h.y);
}



//----------------------------------------------------------------------

float opS(float d1, float d2)
{
	return max(-d2, d1);
}

float opA(float d1, float d2)
{
	return min(d2, d1);
}

vec2 opU(vec2 d1, vec2 d2)
{
	return (d1.x < d2.x) ? d1 : d2;
}

vec3 opRep(vec3 p, vec3 c)
{
	return mod(p, c) - 0.5 * c;
}

vec3 opTwist(vec3 p)
{
	float  c = cos(10.0*p.y + 10.0);
	float  s = sin(10.0*p.y + 10.0);
	mat2   m = mat2(c, -s, s, c);
	return vec3(m*p.xz, p.y);
}

//----------------------------------------------------------------------
float ScherkDe(vec3 p)
{
	float Ex = exp(p.x);
	float Ey = exp(p.y);
	float zz = Ex*Ey;
	float N = Ex*Ex + Ey*Ey;
	float D = 1.0 + zz*zz;
	zz = 4.0*sin(p.z)*zz; // can be + or - or change 4 to get elliptic holes
	if (zz>0.0) N += zz; else D -= zz; // we bring it to the correct eq side :)
	return abs(log(N / D)) - 0.05; // give it a little thickness so it renders better
}

float kTower(vec3 p)
{
	float z = p.z;
	float t = 1.5*atan(p.y, p.x);
	float u = sqrt(p.x*p.x + p.y*p.y);
	float x = sin(t)*u;
	float y = cos(t)*u;
	float ex = exp(x);
	float ey = exp(y);
	float zz = ex*ey;
	float n = ex*ex + ey*ey;
	float d = 1.0 + zz*zz;
	zz = 4.0*sin(p.z)*zz;
	if (zz>0.0) n = n + zz; else d = d - zz;
	return abs(log(n / d)) - 0.05;
}

float menger(in vec3 p)
{
	p = opRep(p, vec3(10));

	float d = sdBox(p, vec3(2));
	vec2 res = vec2(d, 1.0);

	float s = 1.0;
	for (int m = 0; m < 4; m++)
	{
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
			res = vec2(d, min(res.y, 0.2*da*db*dc));
		}
	}

	return res.x;
}

#define PI 3.14159265
#define TAU (2*PI)
#define PHI (sqrt(5.)*0.5 + 0.5)

float fBlob(vec3 p) {
	p = abs(p);
	if (p.x < max(p.y, p.z)) p = p.yzx;
	if (p.x < max(p.y, p.z)) p = p.yzx;
	float b = max(max(max(
		dot(p, normalize(vec3(1, 1, 1))),
		dot(p.xz, normalize(vec2(PHI + 1., 1.)))),
		dot(p.yx, normalize(vec2(1., PHI)))),
		dot(p.xz, normalize(vec2(1., PHI))));
	float l = length(p);
	return l - 1.5 - 0.2 * (1.5 / 2.)* cos(min(sqrt(1.01 - b / l)*(PI / 0.25), PI));
}

float sdPlaneSin(vec3 p)
{
	return p.y + cos(p.x) * sin(p.z) * 0.3;
}

float fOpUnionRound(float a, float b, float r) {
	vec2 u = max(vec2(r - a, r - b), vec2(0));
	return max(r, min(a, b)) - length(u);
}

float mbox(vec3 p)
{
	const int iterations = 10;
	const float fixedRadius = 1.0;
	const float scale = 2.3;
	const float minRadius = 0.5;

	float de = scale;
	float fR2 = sqrt(fixedRadius * fixedRadius);
	float mR2 = sqrt( minRadius * minRadius);

	vec3 q = p;
	for (int i = 0; i < iterations; i++)
	{
		if (q.x > 1.0)
			q.x = 2.0 - q.x;
		else
			if (q.x < -1.0)
				q.x = -2.0 - q.x;

		if (q.y > 1.0)
			q.y = 2.0 - q.y;
		else
			if (q.y < -1.0)
				q.y = -2.0 - q.y;

		if (q.z > 1.0)
			q.z = 2.0 - q.z;
		else
			if (q.z < -1.0)
				q.z = -2.0 - q.z;

		float r2 = length(q);

		if (r2 < mR2)
		{
			float div = fR2 / mR2;

			q *= div;
			de *= div;
		}
		else
			if (r2 < fR2)
			{
				float div = fR2 / r2;
				q *= div;
				de *= div;
			}

		q *= scale;
		q += p;
		de *= scale;
	}

	// Return the distance estimation value which determines the next raytracing
	// step size, or if whether we are within the threshold of the surface.
	return length(q) / abs(de);
}

float mbulb(vec3 pp)
{
	vec3 c = pp;// new Vector3d(-1.1, 0.0, 0.0); // Julia set has fixed c, Mandelbrot c changes with location
	float p = 8;			// power
	float pd = p - 1.0;    // power for derivative

	//					   // Convert z to polar coordinates
	float R = length(pp);
	float th = atan(pp.y, pp.x);
	float ph = acos(pp.z / R);

	vec3 dz;
	float ph_dz = 0.0;
	float th_dz = 0.0;
	float R_dz = 1.0;

	//// Iterate to compute the distance estimator.
	float powR, powRRdz, phdz_pdph, powRsin, thdz_pdth, powR_powRsin, p_th;
	vec3 z = pp;
	for (int i = 0; i < 10; i++)
	{
		// Calculate derivative of 
		powR = p * pow(R, pd);
		powRRdz = powR * R_dz;
		phdz_pdph = ph_dz + pd * ph;
		powRsin = powRRdz * sin(phdz_pdph);
		thdz_pdth = th_dz + pd * th;
		dz.x = powRsin * cos(thdz_pdth) + 1.0;
		dz.y = powRsin * sin(thdz_pdth);
		dz.z = powRRdz * cos(phdz_pdph);

		// polar coordinates of derivative dz
		R_dz = length(dz);
		th_dz = atan(dz.y, dz.x);
		ph_dz = asin(dz.z / R_dz);

		// z iteration
		powR = pow(R, p);
		powRsin = sin(p * ph);
		powR_powRsin = powR * powRsin;
		p_th = p * th;
		z.x = powR_powRsin * cos(p_th);
		z.y = powR_powRsin * sin(p_th);
		z.z = powR * cos(p * ph);
		z += c;

		R = length(z);
		th = atan(z.y, z.x);
		ph = asin(z.z / R);

		if (R > 4.0)
			break;
	}

	// Return the distance estimation value which determines the next raytracing
	// step size, or if whether we are within the threshold of the surface.
	return  0.5 *R * log(R) / R_dz;
}

vec2 map(in vec3 pos)
{
	/// mandelbulb
	//vec2 res = vec2(sdPlane(pos), 1.0);
	//res =
	//	opU(res, vec2(
	//		mbulb(pos), 15.0));

	/// mandlebox
	//vec2 res = vec2(mbox(pos), 15.0);

	/// blob
	//vec2 res = vec2(sdPlaneSin(pos), 1.0);
	//res =
	//	opU(res, vec2(
	//		fBlob(pos - vec3(0.0, 2.0, 0.0)) + sin(iGlobalTime) * 0.4, 49.0));

	///a valley of mengers
	vec2 res = vec2(sdPlaneSin(pos), 1.0);
	res = opU(res, vec2(menger(vec3(pos.x, pos.y - sin(iGlobalTime) * 0.02, pos.z)), 15.0));
	vec3 repp = opRep(pos, vec3(7));
	//float blob = fBlob(repp);// +sin(iGlobalTime) * 0.4;
	float blob = sdSphere(repp, 1.5);
	res.x = fOpUnionRound(res.x, blob, 0.2);

	/// boxes
	//vec3 repp = opRep(pos, vec3(5));
	//float sdBoxRep = sdBox( repp, vec3(1.0, 1.0, 1.0));

	//vec2 res = vec2(sdPlane(pos), 1.0);
	//res = opU(res, vec2(sdBoxRep, 17.0));

	/// tunnels
	//res = opU(res, vec2(
	//		opS(
	//			sdPlaneX(
	//				pos - vec3(-5.0, 0.0, 0.0)),
	//			sdBoxRep ),
	//	17.0));

	//vec2 res = opU(vec2(sdPlane(pos), 1.0),
	//	vec2(sdSphere(pos - vec3(0.0, 0.25, 0.0), 0.25), 46.9));
	//res = opU(res, vec2(sdBox(pos - vec3(1.0, 0.25, 0.0), vec3(0.25)), 3.0));
	//res = opU(res, vec2(udRoundBox(pos - vec3(1.0, 0.25, 1.0), vec3(0.15), 0.1), 41.0));
	//res = opU(res, vec2(sdTorus(pos - vec3(0.0, 0.25, 1.0), vec2(0.20, 0.05)), 25.0));
	//res = opU(res, vec2(sdCapsule(pos, vec3(-1.3, 0.10, -0.1), vec3(-0.8, 0.50, 0.2), 0.1), 31.9));
	//res = opU(res, vec2(sdTriPrism(pos - vec3(-1.0, 0.25, -1.0), vec2(0.25, 0.05)), 43.5));
	//res = opU(res, vec2(sdCylinder(pos - vec3(1.0, 0.30, -1.0), vec2(0.1, 0.2)), 8.0));
	//res = opU(res, vec2(sdCone(pos - vec3(0.0, 0.50, -1.0), vec3(0.8, 0.6, 0.3)), 55.0));
	//res = opU(res, vec2(sdTorus82(pos - vec3(0.0, 0.25, 2.0), vec2(0.20, 0.05)), 50.0));
	//res = opU(res, vec2(sdTorus88(pos - vec3(-1.0, 0.25, 2.0), vec2(0.20, 0.05)), 43.0));
	//res = opU(res, vec2(sdCylinder6(pos - vec3(1.0, 0.30, 2.0), vec2(0.1, 0.2)), 12.0));
	//res = opU(res, vec2(sdHexPrism(pos - vec3(-1.0, 0.20, 1.0), vec2(0.25, 0.05)), 17.0));

	////res = opU(res, vec2(kTower(pos - vec3(10.0, -1.0, 10.0)), 41.0));
	//
	//res = opU(res, vec2(opS(
	//	udRoundBox(pos - vec3(-2.0, 0.2, 1.0), vec3(0.15), 0.05),
	//	sdSphere(pos - vec3(-2.0, 0.2, 1.0), 0.25)), 13.0));
	//res = opU(res, vec2(opS(
	//	sdTorus82(pos - vec3(-2.0, 0.2, 0.0), vec2(0.20, 0.1)),
	//	sdCylinder(opRep(vec3(atan(pos.x + 2.0, pos.z) / 6.2831,
	//		pos.y,
	//		0.02 + 0.5*length(pos - vec3(-2.0, 0.2, 0.0))),
	//		vec3(0.05, 1.0, 0.05)), vec2(0.02, 0.6))), 51.0));
	//res = opU(res, vec2(0.7*sdSphere(pos - vec3(-2.0, 0.25, -1.0), 0.2) +
	//	0.03*sin(50.0*pos.x)*sin(50.0*pos.y)*sin(50.0*pos.z),
	//	65.0));
	//res = opU(res, vec2(0.5*sdTorus(opTwist(pos - vec3(-2.0, 0.25, 2.0)), vec2(0.20, 0.05)), 46.7));

	//res = opU(res, vec2(sdConeSection(pos - vec3(0.0, 0.35, -2.0), 0.15, 0.2, 0.1), 13.67));

	//res = opU(res, vec2(sdEllipsoid(pos - vec3(1.0, 0.35, -2.0), vec3(0.15, 0.2, 0.05)), 43.17));
	
	return res;
}

vec2 castRay1111111111(in vec3 ro, in vec3 rd)
{
	float tmin = 0.0002;
	float tmax = 100.0;

	float precis = 0.0002;
	float t = tmin;
	float m = -1.0;
	for (int i = 0; i < 50; i++)
	{
		vec2 res = map(ro + rd * t);
		if (res.x < precis || t > tmax) break;
		t += res.x;
		m = res.y;
	}

	if (t>tmax) m = -1.0;
	return vec2(t, m);
}


vec2 castRay(in vec3 ro, in vec3 rd)
{
	const float MAX_DIST = 100;
	const float MIN_DIST = 0.0002;

	float t = 0.0;
	vec2 h = vec2(1.0);
	float overstep = 0.0;
	float phx = MAX_DIST;

	for (int i = 0; i < 100; i++)
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

float calcAO(in vec3 pos, in vec3 nor)
{
	float occ = 0.0;
	float sca = 1.0;
	for (int i = 0; i<5; i++)
	{
		float hr = 0.01 + 0.12*float(i) / 4.0;
		vec3 aopos = nor * hr + pos;
		float dd = map(aopos).x;
		occ += -(dd - hr)*sca;
		sca *= 0.95;
	}
	return clamp(1.0 - 3.0*occ, 0.0, 1.0);
}

vec3 render(in vec3 ro, in vec3 rd)
{
	vec3 col = vec3(0.7, 0.9, 1.0) + rd.y*0.8;
	vec2 res = castRay(ro, rd);
	float t = res.x;
	float m = res.y;
	if (m > -0.5)
	{
		vec3 pos = ro + t * rd;
		vec3 nor = calcNormal(pos);
		vec3 ref = reflect(rd, nor);

		// material        
		col = 0.45 + 0.3*sin(vec3(0.05, 0.08, 0.10)*(m - 1.0));

		if (m < 1.5)
		{
			float f = mod(floor(5.0*pos.z) + floor(5.0*pos.x), 2.0);
			col = 0.4 + 0.1*f*vec3(1.0);
		}

		// lighitng        
		float occ = calcAO(pos, nor);
		//vec3  lig = normalize(vec3(-0.6, 0.7, -0.5));
		vec3  lig = normalize(vec3(cos(iGlobalTime *0.1), abs(sin(iGlobalTime *0.1)), cos(iGlobalTime *0.1) * sin(iGlobalTime *0.1)));
		float amb = clamp(0.5 + 0.5*nor.y, 0.0, 1.0);
		float dif = clamp(dot(nor, lig), 0.0, 1.0);
		float bac = clamp(dot(nor, normalize(vec3(-lig.x, 0.0, -lig.z))), 0.0, 1.0)*clamp(1.0 - pos.y, 0.0, 1.0);
		float dom = smoothstep(-0.1, 0.1, ref.y);
		float fre = pow(clamp(1.0 + dot(nor, rd), 0.0, 1.0), 2.0);
		float spe = pow(clamp(dot(ref, lig), 0.0, 1.0), 16.0);

		dif *= softshadow(pos, lig, 0.02, 25);
		dom *= softshadow(pos, ref, 0.02, 25);

		vec3 lin = vec3(0.0);
		lin += 1.20*dif*vec3(1.00, 0.85, 0.55);
		lin += 1.20*spe*vec3(1.00, 0.85, 0.55)*dif;
		lin += 0.20*amb*vec3(0.50, 0.70, 1.00)*occ;
		lin += 0.30*dom*vec3(0.50, 0.70, 1.00)*occ;
		lin += 0.30*bac*vec3(0.25, 0.25, 0.25)*occ;
		lin += 0.40*fre*vec3(1.00, 1.00, 1.00)*occ;
		col = col*lin;

		col = mix(col, vec3(0.8, 0.9, 1.0), 1.0 - exp(-0.002*t*t));

	}

	return vec3(clamp(col, 0.0, 1.0));
}

mat3 setCamera(in vec3 ro, in vec3 ta, float cr)
{
	vec3 cw = normalize(ta - ro);
	vec3 cp = vec3(sin(cr), cos(cr), 0.0);
	vec3 cu = normalize(cross(cw, cp));
	vec3 cv = normalize(cross(cu, cw));
	return mat3(cu, cv, cw);
}

void main(void)
{
	// camera	
	vec3 ro = CamRo;
	vec3 ta = CamTa;

	// camera-to-world transformation
	mat3 ca = setCamera(ro, ta, 0.0);

	// ray direction
	vec2 q = fragCoord.xy / iResolution.xy;
	vec2 p = -1.0 + 2.0 * q;
	p.x *= iResolution.x / iResolution.y;

	vec3 rd = ca * normalize(vec3(p.xy, 2.0));

	// render	
	vec3 col = render(ro, rd);

	col = pow(col, vec3(0.4545));

	gl_FragColor = vec4(col, 1.0);
}