using OpenTK;
using System;

namespace GldeTK
{
    public class Physics
    {
        public float GlobalTime = 0;

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

        Vector3 Mod3(Vector3 v1, Vector3 v2)
        {
            return
                new Vector3(
                    Math.Abs(v1.X % (v2.X != 0 ? v2.X : float.MinValue)),
                    Math.Abs(v1.Y % (v2.Y != 0 ? v2.Y : float.MinValue)),
                    Math.Abs(v1.Z % (v2.Z != 0 ? v2.Z : float.MinValue)));
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

        float SdCylinderInf(Vector3 p, float r)
        {
            p.Y = 0f;
            return p.LengthFast - r;
        }

        float SdCylinder(Vector3 p, float r, float h)
        {
            return 
                Math.Max(
                p.Xz.LengthFast - r,
                Math.Abs(p.Y) - h);
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
                Mod3(p, c) - 0.5f * c;
                //new Vector3(Math.Abs(p.X % c.X), Math.Abs(p.Y % c.Y), Math.Abs(p.Z % c.Z)) - 0.5f * c;
                //mod(p, c) - 0.5 * c;
        }

        // Map projection and raycaster systems ----------------------------------------------------------------------

        float Map(Vector3 pos)
        {
            float d = SdPlaneY(pos);

            d = OpA(
                    d,
                    SdBox(
                        new Vector3(pos.X, pos.Y - 1.0f, pos.Z),
                        new Vector3(1.0f)));

            //Vector3 posRepeat = OpRep(pos, new Vector3(10f, 10f + (float)Math.Sin(GlobalTime), 10f));

            //d = OpA(
            //        d,
            //        SdSphere(posRepeat, 1.0f));

            //posRepeat = OpRep(pos, new Vector3(7f, 0f, 9f));

            //d = OpA(
            //        d,
            //        SdBox(posRepeat, new Vector3(1.0f, 2.0f, 1.0f)));

            //posRepeat = OpRep(pos, new Vector3(12f, 0f, 13f));

            //d = OpA(
            //        d,
            //        SdCylinder(posRepeat, 1.0f, 30.0f));

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
        /// <returns></returns>
        public float CastRay(Vector3 ro, Vector3 rd)
        {
            //TODO move to external constants w/ uniq names
            const int MAX_RAY_STEPS = 16;
            const float MIN_DIST = 0.1f;
            float MAX_DIST = 100;

            float t = 0.0f;
            float h = 1.0f;

            for (int i = 0; i < MAX_RAY_STEPS; i++)
            {

                h = Map(ro + rd * t);
                t += h;

                if (h < MIN_DIST || t > MAX_DIST)
                    break;
            }

            return t;
        }

        float motion_fallSpeed = .0f;
        float phys_freeFallAccel = 9.8f;
        public Vector3 Gravity(float delta, Ray rayOrigin, float player_hitRadius, bool stopFallTrick = false)
        {
            if (stopFallTrick)
                motion_fallSpeed = 0f;  // stop fall
            else
                motion_fallSpeed += phys_freeFallAccel * delta; // free fall

            Vector3 fallVector = new Vector3(-rayOrigin.Up * motion_fallSpeed * delta);
            float sd = CastRay(rayOrigin.Origin, Vector3.NormalizeFast(fallVector));
            
            // when hit bottom surface
            if (sd <= player_hitRadius)
            {
                motion_fallSpeed = 0f;
                fallVector = Vector3.Zero;
            }

            return fallVector;
        }
    } // class
}
