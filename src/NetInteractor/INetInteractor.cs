using System;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace NetInteractor
{
    public interface INetInteractor
    {
        Task VisitAsync(string url);

        Task PostForm(int formIndex, NameValueCollection formValues = null);

        Task PostForm(string actionOrName, NameValueCollection formValues = null);
    }
}