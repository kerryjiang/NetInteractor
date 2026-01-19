using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using NetInteractor.Core;
using NetInteractor.Core.Config;

namespace NetInteractor.Core.Interacts
{
    public class Call : InteractionBase<CallConfig>
    {
        public Call(CallConfig config)
            : base(config)
        {
            
        }

        public override Task<InteractionResult> ExecuteAsync(InterationContext context)
        {
            return Task.FromResult(new InteractionResult
            {
                Ok = true,
                Target = Config.Target
            });
        }
    }
}