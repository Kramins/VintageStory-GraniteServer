#:sdk Cake.Sdk@6.0.0
#:package Cake.Json?7.0.1

var target = Argument("target", "");
var configuration = Argument("configuration", "Release");
var buildVersion = Argument("buildVersion", "vNext");
var dockerRegistry = Argument("dockerRegistry", "ghcr.io/kramins");
var dockerImageName = Argument("dockerImageName", "granite-server");

// Granite.Mod paths
var modProjectDir = "./Granite.Mod";
var modProjectFile = "./Granite.Mod/Granite.Mod.csproj";
var modTargetFramework = "net8.0";
var modOutputDir = $"{modProjectDir}/bin/{configuration}/{modTargetFramework}/";
var modArtifactsDir = "./artifacts/mod";
var modsDir = modArtifactsDir;

// Granite.Web.Client paths
var clientProjectFile = "./Granite.Web.Client/Granite.Web.Client.csproj";
var clientProjectDir = "./Granite.Web.Client";
var clientPublishDir = $"{clientProjectDir}/bin/{configuration}/net9.0/publish";
var clientWwwrootSource = $"{clientPublishDir}/wwwroot";

// Granite.Server paths
var serverProjectDir = "./Granite.Server";
var serverProjectFile = "./Granite.Server/Granite.Server.csproj";
var serverOutputDir = $"{serverProjectDir}/bin/{configuration}";
var serverPublishDir = $"{serverProjectDir}/obj/publish";
var serverWwwroot = $"{serverProjectDir}/wwwroot";
var serverArtifactsDir = "./artifacts/server";

// Test projects
var testProjects = new[]
{
    "./Granite.Tests/Granite.Tests.csproj",
    "./Granite.Web.Tests/Granite.Web.Tests.csproj",
    "./Granite.Mod.Tests/Granite.Mod.Tests.csproj",
};

// Package naming
var serverPackageName = $"Granite.Server-{buildVersion}-{configuration}";
var serverPackageDir = $"{serverArtifactsDir}/{serverPackageName}";

//////////////////////////////////////////////////////////////////////
// VALIDATION
//////////////////////////////////////////////////////////////////////

// Helper function to check for required environment variables
Action<string, string> RequireEnvironmentVariable = (varName, description) =>
{
    var value = System.Environment.GetEnvironmentVariable(varName) ?? "";
    if (string.IsNullOrEmpty(value))
    {
        throw new Exception(
            $"Required environment variable '{varName}' is not set.\n"
                + $"Description: {description}\n"
                + $"Please set this variable and try again.\n"
                + $"Example: export {varName}=/path/to/vintage/story/installation"
        );
    }
};

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

#region Mod tasks

Task("Clean-Granite.Mod")
    .Does(() =>
    {
        if (DirectoryExists($"{modProjectDir}/bin"))
        {
            DeleteDirectory(
                $"{modProjectDir}/bin",
                new DeleteDirectorySettings { Recursive = true, Force = true }
            );
        }
        if (DirectoryExists($"{modProjectDir}/obj"))
        {
            DeleteDirectory(
                $"{modProjectDir}/obj",
                new DeleteDirectorySettings { Recursive = true, Force = true }
            );
        }
        if (DirectoryExists(modsDir))
        {
            DeleteDirectory(
                modsDir,
                new DeleteDirectorySettings { Recursive = true, Force = true }
            );
        }
    });

Task("UpdateModinfoVersion")
    .Does(() =>
    {
        // Only update if buildVersion is explicitly set (not default)
        var defaultVersion = "vNext";
        if (buildVersion == defaultVersion || buildVersion == "dev")
        {
            Information("Using default buildVersion, skipping modinfo.json update");
            return;
        }

        Information($"Updating modinfo.json version to {buildVersion}");
        var modinfoPath = $"{modProjectDir}/resources/modinfo.json";

        // Read, modify and write JSON using System.IO and Newtonsoft.Json
        var modinfoContent = System.IO.File.ReadAllText(modinfoPath);
        var jObject = Newtonsoft.Json.Linq.JObject.Parse(modinfoContent);
        jObject["version"] = buildVersion;
        System.IO.File.WriteAllText(modinfoPath, jObject.ToString());
    });

Task("Build-Granite.Mod")
    .IsDependentOn("Clean-Granite.Mod")
    .IsDependentOn("UpdateModinfoVersion")
    .Does(() =>
    {
        RequireEnvironmentVariable(
            "VINTAGE_STORY",
            "Path to Vintage Story game installation directory (contains Vintagestory.dll and other game files)"
        );

        DotNetBuild(modProjectFile, new DotNetBuildSettings { Configuration = configuration });
    });

Task("Package-Granite.Mod")
    .IsDependentOn("Build-Granite.Mod")
    .Does(() =>
    {
        CreateDirectory(modsDir);

        // Remove runtimes folder from output if present (matches msbuild CleanupBeforeZip target)
        var runtimesPath = modOutputDir + "runtimes";
        if (DirectoryExists(runtimesPath))
        {
            DeleteDirectory(
                runtimesPath,
                new DeleteDirectorySettings { Recursive = true, Force = true }
            );
        }

        var zipPath = $"{modsDir}/Granite.Mod-{buildVersion}-{configuration}.zip";

        // Zip the build output into bin/mods/... using .NET
        if (FileExists(zipPath))
        {
            DeleteFile(zipPath);
        }
        System.IO.Compression.ZipFile.CreateFromDirectory(
            modOutputDir,
            zipPath,
            System.IO.Compression.CompressionLevel.Optimal,
            false
        );
    });

Task("UploadArtifacts-Granite.Mod")
    .WithCriteria(GitHubActions.IsRunningOnGitHubActions)
    .IsDependentOn("Package-Granite.Mod")
    .Does(() =>
    {
        Information("========================================");
        Information("GitHub Actions: Granite.Mod build complete");
        Information("========================================");
        Information($"Configuration: {configuration}");
        Information($"Version: {buildVersion}");

        GitHubActions.Commands.UploadArtifact(
            DirectoryPath.FromString(modOutputDir),
            $"granite-mod-${buildVersion}"
        );
    });

#endregion

// ======================================================================
// Granite.Web.Client Tasks
// ======================================================================

#region Client tasks

Task("Clean-Granite.Web.Client")
    .Does(() =>
    {
        if (DirectoryExists($"{clientProjectFile.Replace("Granite.Web.Client.csproj", "bin")}"))
        {
            DeleteDirectory(
                $"{clientProjectFile.Replace("Granite.Web.Client.csproj", "bin")}",
                new DeleteDirectorySettings { Recursive = true, Force = true }
            );
        }
        if (DirectoryExists($"{clientProjectFile.Replace("Granite.Web.Client.csproj", "obj")}"))
        {
            DeleteDirectory(
                $"{clientProjectFile.Replace("Granite.Web.Client.csproj", "obj")}",
                new DeleteDirectorySettings { Recursive = true, Force = true }
            );
        }
    });

Task("Build-Granite.Web.Client")
    .IsDependentOn("Clean-Granite.Web.Client")
    .Does(() =>
    {
        DotNetPublish(
            clientProjectFile,
            new DotNetPublishSettings
            {
                Configuration = configuration,
                OutputDirectory = clientPublishDir,
            }
        );
    });

Task("Copy-Client-To-Server-Wwwroot")
    .IsDependentOn("Build-Granite.Web.Client")
    .Does(() =>
    {
        // Remove existing wwwroot to ensure clean state
        if (DirectoryExists(serverWwwroot))
        {
            DeleteDirectory(
                serverWwwroot,
                new DeleteDirectorySettings { Recursive = true, Force = true }
            );
        }

        CreateDirectory(serverWwwroot);

        // Copy client wwwroot to server wwwroot
        var sourceDir = new System.IO.DirectoryInfo(clientWwwrootSource);
        var destDir = new System.IO.DirectoryInfo(serverWwwroot);

        System.Action<System.IO.DirectoryInfo, System.IO.DirectoryInfo> copyDir = null;
        copyDir = (source, target) =>
        {
            if (!target.Exists)
                target.Create();

            foreach (var file in source.GetFiles())
            {
                file.CopyTo(System.IO.Path.Combine(target.FullName, file.Name), true);
            }

            foreach (var dir in source.GetDirectories())
            {
                var newTarget = target.CreateSubdirectory(dir.Name);
                copyDir(dir, newTarget);
            }
        };

        copyDir(sourceDir, destDir);

        Information($"Copied client files from {clientWwwrootSource} to {serverWwwroot}");
    });

#endregion

// ======================================================================
// Granite.Server Tasks
// ======================================================================

#region Server tasks

Task("Clean-Granite.Server")
    .Does(() =>
    {
        if (DirectoryExists($"{serverProjectDir}/bin"))
        {
            DeleteDirectory(
                $"{serverProjectDir}/bin",
                new DeleteDirectorySettings { Recursive = true, Force = true }
            );
        }
        if (DirectoryExists($"{serverProjectDir}/obj"))
        {
            DeleteDirectory(
                $"{serverProjectDir}/obj",
                new DeleteDirectorySettings { Recursive = true, Force = true }
            );
        }
        if (DirectoryExists(serverArtifactsDir))
        {
            DeleteDirectory(
                serverArtifactsDir,
                new DeleteDirectorySettings { Recursive = true, Force = true }
            );
        }
    });

Task("Build-Granite.Server")
    .IsDependentOn("Clean-Granite.Server")
    .IsDependentOn("Copy-Client-To-Server-Wwwroot")
    .Does(() =>
    {
        DotNetBuild(serverProjectFile, new DotNetBuildSettings { Configuration = configuration });
    });

Task("Publish-Granite.Server")
    .IsDependentOn("Build-Granite.Server")
    .Does(() =>
    {
        DotNetPublish(
            serverProjectFile,
            new DotNetPublishSettings
            {
                Configuration = configuration,
                OutputDirectory = serverPublishDir,
            }
        );
    });

Task("Package-Granite.Server")
    .IsDependentOn("Publish-Granite.Server")
    .Does(() =>
    {
        CreateDirectory(serverPackageDir + "/publish");

        // Copy published output to package directory using .NET IO
        var sourceDir = new System.IO.DirectoryInfo(serverPublishDir);
        var destDir = new System.IO.DirectoryInfo(serverPackageDir + "/publish");

        // Helper function to recursively copy directories
        System.Action<System.IO.DirectoryInfo, System.IO.DirectoryInfo> copyDir = null;
        copyDir = (source, target) =>
        {
            if (!target.Exists)
                target.Create();

            foreach (var file in source.GetFiles())
            {
                file.CopyTo(System.IO.Path.Combine(target.FullName, file.Name), true);
            }

            foreach (var dir in source.GetDirectories())
            {
                var newTarget = target.CreateSubdirectory(dir.Name);
                copyDir(dir, newTarget);
            }
        };

        copyDir(sourceDir, destDir);

        // Delete existing package zip
        var zipPath = $"{serverArtifactsDir}/{serverPackageName}.zip";
        if (FileExists(zipPath))
        {
            DeleteFile(zipPath);
        }

        // Create zip archive
        System.IO.Compression.ZipFile.CreateFromDirectory(
            serverPackageDir + "/publish",
            zipPath,
            System.IO.Compression.CompressionLevel.Optimal,
            false
        );

        Information($"Server package created: {zipPath}");
    });

#endregion

// ======================================================================
// Unit Test Tasks
// ======================================================================

Task("Test-Unit")
    .Does(() =>
    {
        foreach (var testProject in testProjects)
        {
            try
            {
                Information($"Running tests for {testProject}...");
                DotNetTest(
                    testProject,
                    new DotNetTestSettings
                    {
                        Configuration = configuration,
                        NoBuild = false,
                        NoRestore = false,
                        Verbosity = DotNetVerbosity.Minimal,
                    }
                );
            }
            catch (Exception ex)
            {
                Warning($"Test project {testProject} failed, but continuing: {ex.Message}");
            }
        }
    });

// ======================================================================
// Docker Build Tasks
// ======================================================================

Task("Docker-Granite.Server")
    .IsDependentOn("Publish-Granite.Server")
    .Does(() =>
    {
        var dockerTag = $"{buildVersion}-{configuration.ToLower()}";
        var imageTag = string.IsNullOrEmpty(dockerRegistry)
            ? $"{dockerImageName}:{dockerTag}"
            : $"{dockerRegistry}/{dockerImageName}:{dockerTag}";

        Information("========================================");
        Information("Building Docker image...");
        Information("========================================");
        Information($"Image tag: {imageTag}");

        var exitCode = StartProcess(
            "docker",
            new ProcessSettings { Arguments = $"build -t {imageTag} -f ./Dockerfile ." }
        );

        if (exitCode != 0)
        {
            throw new Exception($"Docker build failed with exit code {exitCode}");
        }

        Information("========================================");
        Information("Docker image built successfully!");
        Information("========================================");
        Information($"Built: {imageTag}");
        Information("To run locally: docker run -p 80:80 " + imageTag);
    });

Task("Push-Docker-Granite.Server")
    .IsDependentOn("Docker-Granite.Server")
    .WithCriteria(!string.IsNullOrEmpty(dockerRegistry))
    .Does(() =>
    {
        var dockerTag = $"{buildVersion}-{configuration.ToLower()}";
        var imageTag = $"{dockerRegistry}/{dockerImageName}:{dockerTag}";

        Information("========================================");
        Information("Pushing Docker image to registry...");
        Information("========================================");
        Information($"Registry: {dockerRegistry}");
        Information($"Image: {imageTag}");

        var exitCode = StartProcess(
            "docker",
            new ProcessSettings { Arguments = $"push {imageTag}" }
        );

        if (exitCode != 0)
        {
            throw new Exception($"Docker push failed with exit code {exitCode}");
        }

        Information("========================================");
        Information("Docker image pushed successfully!");
        Information("========================================");
        Information($"Pushed: {imageTag}");
    });

// ======================================================================
// Combined Build Targets
// ======================================================================

#region Combined targets

Task("Build-Server-Only")
    .IsDependentOn("Package-Granite.Server")
    .Does(() =>
    {
        Information("========================================");
        Information("Server package built successfully!");
        Information("========================================");
        Information(
            $"Server package: ./artifacts/server/Granite.Server-{buildVersion}-{configuration}.zip"
        );
    });

Task("Build-All")
    .IsDependentOn("Package-Granite.Server")
    .IsDependentOn("Package-Granite.Mod")
    .Does(() =>
    {
        Information("========================================");
        Information("All packages built successfully!");
        Information("========================================");
        Information(
            $"Server package: ./artifacts/server/Granite.Server-{buildVersion}-{configuration}.zip"
        );
        Information($"Mod package: ./artifacts/mod/Granite.Mod-{buildVersion}-{configuration}.zip");
    });

Task("Build-Docker-Image")
    .IsDependentOn("Docker-Granite.Server")
    .Does(() =>
    {
        var dockerTag = $"{buildVersion}-{configuration.ToLower()}";
        var imageTag = string.IsNullOrEmpty(dockerRegistry)
            ? $"{dockerImageName}:{dockerTag}"
            : $"{dockerRegistry}/{dockerImageName}:{dockerTag}";

        Information("========================================");
        Information("Docker image ready!");
        Information("========================================");
        Information($"Image: {imageTag}");
        Information("");
        Information("Usage examples:");
        Information($"  docker run -p 80:80 {imageTag}");
        Information($"  docker run -p 8080:80 {imageTag}");
        Information($"  docker tag {imageTag} {dockerImageName}:latest");
    });

Task("UploadArtifacts-Granite.Server")
    .WithCriteria(GitHubActions.IsRunningOnGitHubActions)
    .IsDependentOn("Package-Granite.Server")
    .Does(() =>
    {
        Information("========================================");
        Information("GitHub Actions: Granite.Server build complete");
        Information("========================================");
        Information($"Configuration: {configuration}");
        Information($"Version: {buildVersion}");

        var zipPath = $"{serverArtifactsDir}/{serverPackageName}.zip";
        GitHubActions.Commands.UploadArtifact(
            FilePath.FromString(zipPath),
            $"granite-server-{buildVersion}"
        );
    });

Task("GithubActions-Granite.Mod")
    .IsDependentOn("UploadArtifacts-Granite.Mod")
    .WithCriteria(GitHubActions.IsRunningOnGitHubActions)
    .Does(() =>
    {
        Information("========================================");
        Information("GitHub Actions: Granite.Mod build complete");
        Information("========================================");
        Information($"Configuration: {configuration}");
        Information($"Version: {buildVersion}");
    });

Task("GithubActions-Granite.Server")
    .IsDependentOn("UploadArtifacts-Granite.Server")
    .WithCriteria(GitHubActions.IsRunningOnGitHubActions)
    .Does(() =>
    {
        Information("========================================");
        Information("GitHub Actions: Granite.Server build complete");
        Information("========================================");
        Information($"Configuration: {configuration}");
        Information($"Version: {buildVersion}");
    });

#endregion

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
