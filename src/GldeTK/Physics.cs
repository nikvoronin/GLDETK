﻿using OpenTK;
using System;

namespace GldeTK
{
    public class Physics
    {
        // Utils ----------------------------------------------------------------------
        Vector3 AbsV3(Vector3 v)
        {
            return
                new Vector3(Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z));
        }

        Vector3 MaxV3(Vector3 v1, Vector3 v2)
        {
            return
                new Vector3(
                    Math.Max(v1.X, v2.X),
                    Math.Max(v1.Y, v2.Y),
                    Math.Max(v1.Z, v2.Z) );
        }

        Vector3 MaxV3(Vector3 v1, float s)
        {
            return
                new Vector3(
                    Math.Max(v1.X, s),
                    Math.Max(v1.Y, s),
                    Math.Max(v1.Z, s));
        }

        // Signed Distance Functions ----------------------------------------------------------------------

        float SdPlaneY(Vector3 p)
        {
            return p.Y;
        }

        float SdSphere(Vector3 p, float s)
        {
            return p.LengthFast - s;
        }

        float SdBox(Vector3 p, Vector3 b)
        {
            Vector3 d = AbsV3(p) - b;
            return
                Math.Min( Math.Max( d.X, Math.Max(d.Y, d.Z)), 0.0f) +
                MaxV3(d, 0.0f).LengthFast;
        }


        // Domain operations ----------------------------------------------------------------------

        float OpA(float d1, float d2)
        {
            return Math.Min(d2, d1);
        }

        Vector3 OpRep(Vector3 p, Vector3 c)
        {
            return
                new Vector3(Math.Abs(p.X % c.X), Math.Abs(p.Y % c.Y), Math.Abs(p.Z % c.Z)) - 0.5f * c;
            //mod(p, c) - 0.5 * c;
        }

        // Map projection and raycaster systems ----------------------------------------------------------------------

        float Map(Vector3 pos)
        {
            float d = SdPlaneY(pos);

            Vector3 posRepeat = OpRep(pos, new Vector3(10.0f));

            d = OpA(
                    d,
                    SdSphere(posRepeat, 1.0f));

            posRepeat = OpRep(pos, new Vector3(7.0f));

            d = OpA(
                    d,
                    SdBox(posRepeat, new Vector3(1.0f, 2.0f, 1.0f)));

            return d;
        }

        const float EPS = 0.001f;
        Vector3 eps_xyy = new Vector3(EPS, 0.0f, 0.0f);
        Vector3 eps_yxy = new Vector3(0.0f, EPS, 0.0f);
        Vector3 eps_yyx = new Vector3(0.0f, 0.0f, EPS);
        public Vector3 GetSurfaceNormal(Vector3 pos)
        {
            Vector3 n = Vector3.NormalizeFast(
                new Vector3(
                    Map(pos + eps_xyy) - Map(pos - eps_xyy),
                    Map(pos + eps_yxy) - Map(pos - eps_yxy),
                    Map(pos + eps_yyx) - Map(pos - eps_yyx)
                    ));

            return n;
        }


        // TODO can optimize we should not need of vector2 just a distance to object or negative value
        /// <summary>
        /// Distance to the founded object or -1.0f otherwise
        /// </summary>
        /// <param name="ro">Ray origin</param>
        /// <param name="rd">Ray direction</param>
        /// <param name="playerR">Radius of the player's capsule</param>
        /// <returns></returns>
        public float CastRay(Vector3 ro, Vector3 rd, float playerR)
        {
            //TODO move to external constants w/ uniq names
            const int MAX_RAY_STEPS = 10;
            const float MIN_DIST = 0.01f;
            float MAX_DIST = 100;

            float t = 0.0f;
            float h = 1.0f;

            for (int i = 0; i < MAX_RAY_STEPS; i++)
            {

                h = Map(ro + rd * t);

                if (h < MIN_DIST || t > MAX_DIST)
                    break;

                t += h;
            }

            return t;
        }

        float motion_fallSpeed = .0f;
        float phys_freeFallAccel = 9.8f;
        public void Gravity(float delta, Vector3 origin, Ray nextStep, float player_hitRadius)
        {
            if (nextStep.Origin.Y <= 0)
                motion_fallSpeed += phys_freeFallAccel * delta;
            else
                motion_fallSpeed = 0f;

            Vector3 fallVector = new Vector3(-nextStep.Up * motion_fallSpeed * delta);
            float sd = CastRay(origin, Vector3.NormalizeFast(fallVector), player_hitRadius);
            // when hit any surface
            if (sd <= player_hitRadius)
            {
                motion_fallSpeed = 0f;
                fallVector = Vector3.Zero;
            }

            nextStep.Origin += fallVector;
        }
    } // class
}
