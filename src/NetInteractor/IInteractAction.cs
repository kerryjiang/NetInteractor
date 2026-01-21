using System;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace NetInteractor
{
    public interface IInteractAction
    {
        Task<InteractionResult> ExecuteAsync(InterationContext context);
    }
}