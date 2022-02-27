# ATC Demo

## Purpose

Code demonstration for displaying and searching [ATC classifications](https://www.who.int/tools/atc-ddd-toolkit/atc-classification)

## Credits

Inspired by Steve Sanderson's [Blazor Demo](https://github.com/SteveSandersonMS/BlazeOrbital.git) at [NDC Oslo](https://youtu.be/Rn8psTi8FBk) 🙇‍

## Install

```shell
> git --version
git version 2.30.0.windows.2

> git clone https://github.com/si618/AtcDemo.git
Cloning into 'AtcDemo'...

> cd .\AtcDemo\src
```

## Compile

```shell
> dotnet --version
6.0.102

> dotnet workload install wasm-tools
...
Successfully installed workload(s) wasm-tools.

> dotnet build
Microsoft (R) Build Engine version 17.0.0+c9eb9dd64 for .NET
...
```

## Run

```shell
> cd .\Server

> dotnet run
Building...
[22:22:22 INF] Seeded 4,326 ATC records in 1,692ms
[22:22:22 INF] Now listening on: https://localhost:7204
...
```

## Points of interest

- gRPC for comms
- SQLite in browser
- Blazor WebAssembly client + PWA
- C# record types for DTOs (Hat-tip: [Ross Lovas](https://github.com/rosslovas) for static class wrapper)
- NBGV (for CI/CD pipelines and releases)
- EditorConfig for `dotnet format` (mostly taken from roslyn + namespace at top of file)
- File-scoped namespaces (for saving horizontal whitespace)
- Directory.Build.props (for nbgv & large apps)

## TODO

- Figure out why SQLite is throwing an error (or wait for WebAssembly package)
- Azure Pipelines or GitHub Actions for builds and deployment (dockerise server)
- Add descriptions for ATC Level and DDD Administration Route codes
- Unit and browser tests
