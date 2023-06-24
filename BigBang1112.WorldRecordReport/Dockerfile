FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
WORKDIR /src
COPY ["BigBang1112.WorldRecordReport/BigBang1112.WorldRecordReport.csproj", "BigBang1112.WorldRecordReport/"]
COPY ["BigBang1112.Essentials/BigBang1112.Essentials.Razor/BigBang1112.Essentials.Razor.csproj", "BigBang1112.Essentials/BigBang1112.Essentials.Razor/"]
COPY ["BigBang1112.Essentials/BigBang1112.Essentials/BigBang1112.Essentials.csproj", "BigBang1112.Essentials/BigBang1112.Essentials/"]
COPY ["BigBang1112.WorldRecordReport.DiscordBot/BigBang1112.WorldRecordReport.DiscordBot.csproj", "BigBang1112.WorldRecordReport.DiscordBot/"]
COPY ["BigBang1112.WorldRecordReportLib/BigBang1112.WorldRecordReportLib.csproj", "BigBang1112.WorldRecordReportLib/"]
COPY ["BigBang1112.Essentials/BigBang1112.DiscordBot/BigBang1112.DiscordBot.csproj", "BigBang1112.Essentials/BigBang1112.DiscordBot/"]
RUN dotnet restore "BigBang1112.WorldRecordReport/BigBang1112.WorldRecordReport.csproj"
COPY . .
WORKDIR "/src/BigBang1112.WorldRecordReport"
RUN dotnet build "BigBang1112.WorldRecordReport.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BigBang1112.WorldRecordReport.csproj" -c Release -o /app/publish -r linux-musl-x64 -p:PublishReadyToRun=true --self-contained

FROM mcr.microsoft.com/dotnet/runtime-deps:6.0-alpine
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./BigBang1112.WorldRecordReport"]