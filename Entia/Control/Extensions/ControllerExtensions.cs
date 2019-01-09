using Entia.Core;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Nodes;
using System;

namespace Entia.Modules
{
    public static class ControllerExtensions
    {
        public static void Initialize(this Controller controller)
        {
            controller.Run<Phases.PreInitialize>();
            controller.Run<Phases.Initialize>();
            controller.Run<Phases.React.Initialize>();
            controller.Run<Phases.PostInitialize>();
        }

        public static void Dispose(this Controller controller)
        {
            controller.Run<Phases.PreDispose>();
            controller.Run<Phases.React.Dispose>();
            controller.Run<Phases.Dispose>();
            controller.Run<Phases.PostDispose>();
        }

        public static void Run(this Controller controller)
        {
            controller.Run<Phases.PreRun>();
            controller.Run<Phases.Run>();
            controller.Run<Phases.PostRun>();
        }

        public static Result<Controller> Run(this Controllers controllers, Node node, Func<bool> condition)
        {
            var result = controllers.Control(node);
            if (result.TryValue(out var controller))
            {
                controller.Initialize();
                while (condition()) controller.Run();
                controller.Dispose();
            }

            return result;
        }
    }
}
