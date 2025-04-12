using Clerk.Net.Client;

namespace Congen.Storage.Api.Background_Workers
{
    public class ClerkWorker : BackgroundService
    {
        private readonly ClerkApiClient clerkClient;

        public ClerkWorker(ClerkApiClient clerkApiClient)
        {
            clerkClient = clerkApiClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            
        }
    }
}
