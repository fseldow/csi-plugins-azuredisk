using System;
using System.Threading.Tasks;
using Grpc.Core;
using Xunit;

namespace Csi.Plugins.AzureDisk.Tests
{
    static class AssertRpc
    {
        public static async Task ThrowsRpcUnimplementedException(Func<Task> run)
        {
            var ex = await Assert.ThrowsAsync<RpcException>(run);
            Assert.Equal(StatusCode.Unimplemented, ex.Status.StatusCode);
        }
    }
}
