using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using NetInteractor.Core;
using NetInteractor.Core.Config;

namespace NetInteractor.Core.Interacts
{
    public class If : InteractionBase<IfConfig>
    {
        public IInteractAction Child { get; private set; }

        public If(IfConfig config)
            : base(config)
        {
            if (config.Child != null)
            {
                Child = config.Child.GetAction();
            }
        }

        public override async Task<InteractionResult> ExecuteAsync(InterationContext context)
        {
            var value = GetValue(context, Config.Property);

            if (string.Compare(value, Config.Value, true) != 0)
            {
                return await Task.FromResult(new InteractionResult
                {
                    Ok = true
                });
            }

            return await Child.ExecuteAsync(context);
        }
    }
}