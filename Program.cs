using Newtonsoft.Json.Linq;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace no_as_a_service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);

            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(ip, partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 120, 
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
                });

            });

            var app = builder.Build();
            app.UseRateLimiter();

            var reasons = JRaw.Parse(File.ReadAllText(@".\reason.js"));
            var rnd = new Random(DateTime.Now.Second);

            app.MapGet("/", () => "welcome to no-as-a-service, navigate to /no to get started");
            app.MapGet("/no", () => reasons.ElementAt(rnd.Next(1, reasons.Count()) - 1).ToString());

            app.Run();
        }
    }

}
