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

        buildPackage =
          { name, projectPath }:
          pkgs.buildDotnetModule {
            pname = name;
            version = builtins.head (
              builtins.match ".*<PropertyGroup>.*<Version>([^<]+)</Version>.*" (
                builtins.replaceStrings [ "\n" ] [ " " ] (builtins.readFile projectPath)
              )
            );
            src = ./.;
            projectFile = projectPath;
            nugetDeps = ./deps.nix;
            dotnet-sdk = pkgs.dotnet-sdk_10;
            dotnet-runtime = pkgs.dotnet-runtime_10;
            buildPhase = ''
              dotnet pack ${projectPath} -c Release -o ./nupkgs --no-restore
            '';
            installPhase = ''
              mkdir -p $out
              cp ./nupkgs/*.nupkg $out/
            '';
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

        packages.dependencyWizard = buildPackage {
          name = "BumaSoft.DependencyWizard";
          projectPath = ./src/DependencyWizard/DependencyWizard.csproj;
        };

        packages.dependencyWizardSemanticKernel = buildPackage {
          name = "BumaSoft.DependencyWizard.SemanticKernel";
          projectPath = ./src/DependencyWizard.SemanticKernel/DependencyWizard.SemanticKernel.csproj;
        };

        packages.all = [
          self.packages.${system}.dependencyWizard
          self.packages.${system}.dependencyWizardSemanticKernel
        ];
      }
    );
}
