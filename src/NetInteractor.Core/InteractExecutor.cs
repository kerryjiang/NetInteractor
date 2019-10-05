using System;
using System.Linq;
using System.Collections.Specialized;
using System.Threading.Tasks;
using NetInteractor.Core.Config;
using Microsoft.Extensions.DependencyInjection;
using NetInteractor.Core.WebAccessors;

namespace NetInteractor.Core
{
    public class InterationExecutor
    {
        private IServiceProvider serviceProvider;

        private static IServiceProvider CreateDefaultServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddTransient<IWebAccessor, HttpWebAccessor>();
            return services.BuildServiceProvider();;
        }

        public InterationExecutor()
            : this(CreateDefaultServiceProvider())
        {

        }

        public InterationExecutor(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        private async Task<InteractionResult> ExecuteTargetAsync(TargetConfig target, InterationContext context, TargetConfig[] allTargets)
        {
            var actions = target.Actions.Select(x =>x.GetAction());

            var lastResult = default(InteractionResult);

            foreach (var action in actions)
            {
                var result = lastResult = await action.ExecuteAsync(context);

                if (!result.Ok)
                    break;

                if (string.IsNullOrEmpty(result.Target))
                    continue;

                var callTarget = allTargets.FirstOrDefault(t => t.Name.Equals(result.Target, StringComparison.OrdinalIgnoreCase));
                
                if (callTarget == null)
                    throw new Exception("callTarget cannot be found:" + result.Target);

                result = lastResult = await ExecuteTargetAsync(callTarget, context, allTargets);

                if (!result.Ok)
                    break;
            }

            lastResult.Outputs = context.Outputs;
            
            return lastResult;
        }
        
        public async Task<InteractionResult> ExecuteAsync(InteractConfig config, NameValueCollection inputs = null, string target = null)
        {
            var targets = config.Targets;

            if (string.IsNullOrEmpty(target))
            {
                target = config.DefaultTarget;
            }

            if (string.IsNullOrEmpty(target))
                throw new Exception("No target is specified.");

            var entranceTarget = targets.FirstOrDefault(t => t.Name.Equals(target, StringComparison.OrdinalIgnoreCase));
            
            if (entranceTarget == null)
                throw new Exception("target cannot be found:" + target);

            var context = new InterationContext();

            context.Inputs = inputs;
            context.WebAccessor = this.serviceProvider.GetService<IWebAccessor>();

            return await ExecuteTargetAsync(entranceTarget, context, targets);
        }
    }
}