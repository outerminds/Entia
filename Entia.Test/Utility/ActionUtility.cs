using System;
using System.Collections.Generic;
using System.Linq;
using Entia.Modules.Component;

namespace Entia.Test
{
    public static class ActionUtility
    {
        public static States NextState(this Random random)
        {
            var value = random.NextDouble();
            return
                value < 0.3 ? States.Enabled :
                value < 0.6 ? States.Disabled :
                value < 0.9 ? States.All :
                States.None;
        }

        public static Entity NextEntity(this Random random, Modules.Entities entities) => entities.ElementAt(random.Next(entities.Count));
    }
}