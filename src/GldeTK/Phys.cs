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
                new Vector3(p.X % c.X, p.Y % c.Y, p.Z % c.Z ) - 0.5f * c;
                //mod(p, c) - 0.5 * c;
        }

        //----------------------------------------------------------------------

        static Vector2 Map(Vector3 pos)
        {
            Vector2 res = new Vector2(SdPlaneY(pos), 1.0f);

            Vector3 prep = OpRep(pos, new Vector3(7.0f));

            res.X =
                OpA(
                    res.X,
                    SdSphere(prep, 1.0f));

            res.Y = 45.0f;

            return res;
        }

        // TODO can optimize we should not need of vector2 just a distance to object or negative value
        public static Vector2 CastRay(Vector3 ro, Vector3 rd, float playerR)
        {
            //TODO move to external constants w/ uniq names
            const float MIN_DIST = 0.01f;
            float MAX_DIST = playerR * 2.0f;

            float t = 0.0f;
            Vector2 h = new Vector2(1.0f);

            for (int i = 0; i < 10; i++)
            {

                h = Map(ro + rd * t);

                if (h.X < MIN_DIST || t > MAX_DIST)
                    break;

                t += h.X;
            }

            h.X = t;

            if (t > MAX_DIST)
                h.Y = -1.0f;

            return h;
        }
    } // class
}
