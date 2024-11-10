### How to
 * Build and run
 * run the backend + dashboard:
   ```
   docker run --rm -it -p 18888:18888 -p 4317:18889 --name aspire-dashboard mcr.microsoft.com/dotnet/aspire-dashboard:latest
   ```
 * connect via the link provided in the docker run output
 
### More info

 * https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-otlp-example
 * https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs
