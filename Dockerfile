FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
ARG GITHUB_ACTOR
ARG GITHUB_TOKEN

COPY nuget.config .
COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY src/Shared/ src/Shared/
COPY src/Contracts/ src/Contracts/
COPY Services/UAM/ Services/UAM/

RUN dotnet publish Services/UAM/UAM.csproj -c Release -o /app/publish --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER app
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD bash -c '< /dev/tcp/localhost/8080' || exit 1
ENTRYPOINT ["dotnet", "UAM.dll"]
