using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using NetInteractor.Core;
using NetInteractor.Core.Config;

namespace NetInteractor.Core.Interacts
{
    public class Get : WebInteractionBase<GetConfig>
    {
        public Get(GetConfig config)
            : base(config)
        {
            
        }

        protected override async Task<ResponseInfo> MakeRequest(InterationContext context)
        {
            var url = PrepareValue(context, Config.Url);
            var webAccessor = context.WebAccessor;
            return await webAccessor.GetAsync(url);
        }
    }
}