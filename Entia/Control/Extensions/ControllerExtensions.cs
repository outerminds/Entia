using System;
using Entia.Modules.Control;
using Entia.Phases;

namespace Entia.Modules
{
    public static class ControllerExtensions
    {
        public static void Run(this Controller controller, Func<bool> quit)
        {
            controller.Run<Initialize>();
            controller.Run<React.Initialize>();
            while (!quit()) controller.Run<Run>();
            controller.Run<React.Dispose>();
            controller.Run<Dispose>();
        }

        public static void Run(this Controller controller, in bool quit)
        {
            controller.Run<Initialize>();
            controller.Run<React.Initialize>();
            while (!quit) controller.Run<Run>();
            controller.Run<React.Dispose>();
            controller.Run<Dispose>();
        }
    }
}