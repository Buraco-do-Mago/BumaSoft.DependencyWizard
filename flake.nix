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

        sdk = pkgs.dotnet-sdk_10;
        runtime = pkgs.dotnet-runtime_10;

        buildPackage =
          {
            name,
            projectPath,
          }:
          let
            projectPathString = builtins.replaceStrings [ "${toString ./.}/" ] [ "" ] (toString projectPath);
            version = builtins.head (
              builtins.match ".*<PropertyGroup>.*<Version>([^<]+)</Version>.*" (
                builtins.replaceStrings [ "\n" ] [ " " ] (builtins.readFile projectPath)
              )
            );
          in
          pkgs.buildDotnetModule {
            pname = name;
            version = version;
            src = ./.;
            projectFile = projectPathString;
            nugetDeps = ./deps.nix;
            dotnet-sdk = sdk;
            dotnet-runtime = runtime;
            buildPhase = ''
              echo "Cleaning"
              dotnet clean
              echo "Restoring"
              dotnet restore --no-cache
              echo "Packing"
              dotnet pack ${projectPathString} -c Release -o ./nupkgs --no-restore
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
            sdk
            runtime
            dotnet-aspnetcore_10
          ];
        };

        packages.dependencyWizard = buildPackage {
          name = "BumaSoft.DependencyWizard";
          projectPath = ./src/BumaSoft.DependencyWizard/BumaSoft.DependencyWizard.csproj;
        };

        packages.dependencyWizardSemanticKernel = buildPackage {
          name = "BumaSoft.DependencyWizard.SemanticKernel";
          projectPath = ./src/BumaSoft.DependencyWizard.SemanticKernel/BumaSoft.DependencyWizard.SemanticKernel.csproj;
        };

        packages.all = [
          self.packages.${system}.dependencyWizard
          self.packages.${system}.dependencyWizardSemanticKernel
        ];
      }
    );
}
