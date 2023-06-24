using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Engine.Input
{
    internal interface IEntityInputHandler
    {
        public void HandleInput(InputState inputState);
    }
}
