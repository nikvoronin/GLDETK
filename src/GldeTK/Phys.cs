using OpenTK;
using System;

namespace GldeTK
{
    public static class Phys
    {

        static float SdPlaneY(Vector3 p)
        {
            return p.Y;
        }

        static float SdSphere(Vector3 p, float s)
        {
            return p.LengthFast - s;
        }

        //----------------------------------------------------------------------

        static float OpA(float d1, float d2)
        {
            return Math.Min(d2, d1);
        }

        static Vector3 OpRep(Vector3 p, Vector3 c)
        {
            return
                new Vector3(Math.Abs(p.X % c.X), Math.Abs(p.Y % c.Y), Math.Abs(p.Z % c.Z)) - 0.5f * c;
            //mod(p, c) - 0.5 * c;
        }

        //----------------------------------------------------------------------

        static float Map(Vector3 pos)
        {
            float d = SdPlaneY(pos);

            Vector3 posRepeat = OpRep(pos, new Vector3(7.0f));

            d = OpA(
                    d,
                    SdSphere(posRepeat, 1.0f));

            return d;
        }

        const float EPS = 0.001f;
        static Vector3 eps_xyy = new Vector3(EPS, 0.0f, 0.0f);
        static Vector3 eps_yxy = new Vector3(0.0f, EPS, 0.0f);
        static Vector3 eps_yyx = new Vector3(0.0f, 0.0f, EPS);
        public static Vector3 CalcNormal(Vector3 pos)
        {
            Vector3 nor = new Vector3(
                Map(pos + eps_xyy) - Map(pos - eps_xyy),
                Map(pos + eps_yxy) - Map(pos - eps_yxy),
                Map(pos + eps_yyx) - Map(pos - eps_yyx));

            return nor.Normalized();
        }


        // TODO can optimize we should not need of vector2 just a distance to object or negative value
        /// <summary>
        /// Distance to the founded object or -1.0f otherwise
        /// </summary>
        /// <param name="ro">Ray origin</param>
        /// <param name="rd">Ray direction</param>
        /// <param name="playerR">Radius of the player's capsule</param>
        /// <returns></returns>
        public static float CastRay(Vector3 ro, Vector3 rd, float playerR)
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
    } // class
}
