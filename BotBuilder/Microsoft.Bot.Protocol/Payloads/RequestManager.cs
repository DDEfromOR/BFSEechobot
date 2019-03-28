using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Bot.Protocol.Payloads
{
    public class RequestManager : IRequestManager
    {
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<ReceiveResponse>> _responseTasks;

        public RequestManager()
            : this(new ConcurrentDictionary<Guid, TaskCompletionSource<ReceiveResponse>>())
        {
        }

        public RequestManager(ConcurrentDictionary<Guid, TaskCompletionSource<ReceiveResponse>> responseTasks)
        {
            _responseTasks = responseTasks;
        }
        
        public Task<bool> SignalResponse(Guid requestId, ReceiveResponse response)
        {
            if (_responseTasks.TryGetValue(requestId, out TaskCompletionSource<ReceiveResponse> signal))
            {
                Task.Run(() => { signal.SetResult(response); });
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
        
        public async Task<ReceiveResponse> GetResponseAsync(Guid requestId)
        {
            TaskCompletionSource<ReceiveResponse> responseTask = new TaskCompletionSource<ReceiveResponse>();

            if (!_responseTasks.TryAdd(requestId, responseTask))
            {
                return null;
            }

            try
            {
                var response = await responseTask.Task.ConfigureAwait(false);
                return response;
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            finally
            {
                _responseTasks.TryRemove(requestId, out responseTask);
            }
        }
    }
}
