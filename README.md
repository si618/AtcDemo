# ATC Demo

## Purpose

Experimental code demonstration for displaying and searching [ATC classifications](https://www.who.int/tools/atc-ddd-toolkit/atc-classification)

## Credits

- Inspired by Steve Sanderson's [Blazor Demo](https://github.com/SteveSandersonMS/BlazeOrbital.git) at [NDC Oslo](https://youtu.be/Rn8psTi8FBk) 🙇‍
- National Center for Biomedical Ontology for the [ATC classifications](https://bioportal.bioontology.org/ontologies/ATC?p=summary)
- [Ross Lovas](https://github.com/rosslovas) for the idea of using a static class around C# record types

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
6.0.200

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
[22:22:22 INF] Seeded 4,326 ATC classifications in 497ms
[22:22:22 INF] Now listening on: https://localhost:20222
...
```

## Points of interest

- gRPC for comms
- SQLite in browser
- Blazor WebAssembly client + PWA
- C# record types for DTOs
- NBGV (for CI/CD pipelines and releases)
- EditorConfig for `dotnet format` (mostly taken from roslyn + namespace at top of file)
- File-scoped namespaces (for saving horizontal whitespace)
- Directory.Build.props (for nbgv & large apps)

## TODO

- Figure out why SQLite is throwing an error (or wait for WebAssembly package)
- Work out where to deploy server (Azure web apps?)
- Fix docker port mapping issue (set up environment variables)
- Add descriptions for ATC Level and DDD Administration Route codes
- Add units of measurement for DefinedDailyDose to ATC.json
- Unit and browser tests
