using System.CommandLine;

using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary>
/// </summary>
internal static class InstallCommand
{
    /// <summary>
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, (string CwdRelative, string HomeRelative)>
      ClientPaths = new Dictionary<string, (string, string)>(StringComparer.Ordinal)
      {
          ["copilot"] = (".github/skills", ".copilot/skills"),
          ["claude"] = (".claude/skills", ".claude/skills"),
          ["codex"] = (".agents/skills", ".agents/skills"),
      };

    internal const string SkillName = "adact-cli";

    internal static readonly IReadOnlyList<string> SkillNames =
    [
        SkillName,
        "adact-flaui-testgen",
    ];

    public static Command Build()
    {
        var skills = new Option<string>("--skills")
        {
            Description = "AI client to install Skill files for. One of: copilot, claude, codex.",
            Required = true,
        };
        skills.AcceptOnlyFromAmong("copilot", "claude", "codex");

        var global = new Option<bool>("--global")
        {
            Description = "Install to the user-global location instead of the current working directory.",
        };

        var cmd = new Command("install", "Install ADACT Skill files for an AI coding client.");
        cmd.Options.Add(skills);
        cmd.Options.Add(global);

        cmd.SetAction((parseResult, ct) =>
        {
            var client = parseResult.GetValue(skills) ?? string.Empty;
            var isGlobal = parseResult.GetValue(global);
            return Task.FromResult(Execute(client, isGlobal));
        });

        return cmd;
    }

    /// <summary>
    /// </summary>
    internal static int Execute(string client, bool global)
    {
        string sourceRoot;
        try
        {
            sourceRoot = ResolveSourceRootDirectory();
        }
        catch (DirectoryNotFoundException ex)
        {
            CliError.Write(ErrorCodes.InternalError, ex.Message);
            return ExitCodes.CommandFailed;
        }

        string targetRoot;
        try
        {
            targetRoot = ResolveTargetDirectory(
              client,
              global,
              cwd: Directory.GetCurrentDirectory(),
              homeDir: GetHomeDirectory());
        }
        catch (ArgumentException ex)
        {
            CliError.Write(ErrorCodes.InvalidArgument, ex.Message);
            return ExitCodes.UserError;
        }

        try
        {
            foreach (var skill in SkillNames)
            {
                CopyDirectory(
                  Path.Combine(sourceRoot, skill),
                  Path.Combine(targetRoot, skill));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            CliError.Write(ErrorCodes.InternalError, $"failed to write Skill files: {ex.Message}");
            return ExitCodes.CommandFailed;
        }

        CliOutput.WriteYamlSuccess(
            metaFields: null,
            [
                CliOutput.Field("installed", "true"),
                CliOutput.Field("skills", string.Join(",", SkillNames)),
                CliOutput.Field("path", targetRoot),
            ]);
        return ExitCodes.Success;
    }

    /// <summary>
    /// </summary>
    internal static string ResolveSourceRootDirectory()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Skills");
        if (!Directory.Exists(dir))
        {
            throw new DirectoryNotFoundException(
              $"Skill source directory not found at '{dir}'. The adact build may be incomplete.");
        }

        foreach (var skill in SkillNames)
        {
            var skillDir = Path.Combine(dir, skill);
            if (!Directory.Exists(skillDir))
            {
                throw new DirectoryNotFoundException(
                  $"Skill source directory not found at '{skillDir}'. The adact build may be incomplete.");
            }
        }

        return dir;
    }

    /// <summary>
    /// </summary>
    internal static string ResolveSourceDirectory()
    {
        return Path.Combine(ResolveSourceRootDirectory(), SkillName);
    }

    /// <summary>
    /// </summary>
    internal static string ResolveTargetDirectory(string client, bool global, string cwd, string homeDir)
    {
        if (!ClientPaths.TryGetValue(client, out var paths))
        {
            throw new ArgumentException(
              $"unknown --skills client '{client}'. Expected one of: copilot, claude, codex.",
              nameof(client));
        }

        var basePath = global ? homeDir : cwd;
        if (string.IsNullOrEmpty(basePath))
        {
            throw new ArgumentException(
              global
                ? "user home directory could not be determined."
                : "current working directory could not be determined.",
              global ? nameof(homeDir) : nameof(cwd));
        }

        var relative = global ? paths.HomeRelative : paths.CwdRelative;
        return Path.GetFullPath(Path.Combine(basePath, relative));
    }

    private static string GetHomeDirectory()
    {
        var profile = Environment.GetEnvironmentVariable("USERPROFILE");
        if (!string.IsNullOrEmpty(profile))
        {
            return profile;
        }
        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    /// <summary>
    /// </summary>
    internal static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.EnumerateFiles(source))
        {
            var target = Path.Combine(destination, Path.GetFileName(file));
            File.Copy(file, target, overwrite: true);
        }

        foreach (var subDir in Directory.EnumerateDirectories(source))
        {
            var target = Path.Combine(destination, Path.GetFileName(subDir));
            CopyDirectory(subDir, target);
        }
    }
}
