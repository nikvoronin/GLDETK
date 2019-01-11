using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GldeTK
{
    public class Render
    {
        int h_vertex,
            h_fragment,
            h_shaderProgram;

        int uf_iGlobalTime,
            uf_iResolution,
            uf_CamRo,
            um3_CamProj,
            ubo_GlobalMap;

        GameWindow Window;

        public Render(GameWindow gameWindow)
        {
            Window = gameWindow;
        }
    }
}
