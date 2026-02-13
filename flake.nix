{
  description = "BumaSoft.DependencyWizard flake";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs?ref=nixpkgs-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs =
    {
      self,
      nixpkgs,
      flake-utils,
    }:
    flake-utils.lib.eachDefaultSystem (
      system:
      let
        pkgs = import nixpkgs {
          inherit system;
        };
      in
      with pkgs;
      {
        devShells.default = mkShell {
          buildInputs = [
            dotnet-sdk_10
            dotnet-runtime_10
            dotnet-aspnetcore_10
          ];
        };

        packages.default = pkgs.buildDotnetModule {
          pname = "BumaSoft.DependencyWizard";
          version = "1.0.0";
          src = ./.;
          projectFile = "src/BumaSoft.DependencyWizard/BumaSoft.DependencyWizard.csproj";
          nugetDeps = ./deps.nix;
          dotnet-sdk = pkgs.dotnet-sdk_10;
          dotnet-runtime = pkgs.dotnet-runtime_10;
        };
      }
    );
}
