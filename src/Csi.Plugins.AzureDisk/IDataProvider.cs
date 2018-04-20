using System.Threading.Tasks;

namespace Csi.Helpers.Azure
{
    class DataProviderContext<TResult>
    {
        public TResult Result { get; set; }
    }

    interface IDataProvider<TResult, TContext> where TContext : DataProviderContext<TResult>
    {
        Task Provide(TContext context);
    }

    abstract class ChainDataProvider<TResult, TContext> 
        : IDataProvider<TResult, TContext>
            where TContext : DataProviderContext<TResult>
    {
        public IDataProvider<TResult, TContext> Inner { get; set; }

        public abstract Task Provide(TContext context);
    }
}
