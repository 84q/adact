using System.CommandLine;

using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>install --skills &lt;client&gt;</c> コマンド。ADACT の Skill ファイル一式を
/// AI コーディングクライアントの規定位置にコピーする。設計 013 §3。
/// </summary>
internal static class InstallCommand
{
    /// <summary>
    /// 設計 013 §3.3 のパスマトリクス。
    /// 値: (cwd 配下の相対パス, ホーム配下の相対パス)。両方とも末尾に skill 名 (adact-cli) を含む。
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, (string CwdRelative, string HomeRelative)>
      ClientPaths = new Dictionary<string, (string, string)>(StringComparer.Ordinal)
      {
          ["copilot"] = (".github/skills/adact-cli", ".copilot/skills/adact-cli"),
          ["claude"] = (".claude/skills/adact-cli", ".claude/skills/adact-cli"),
          ["codex"] = (".agents/skills/adact-cli", ".agents/skills/adact-cli"),
      };

    /// <summary>インストールされる Skill 名 (ディレクトリ名 / SKILL.md の <c>name</c> フロントマタ)。</summary>
    internal const string SkillName = "adact-cli";

    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>install サブコマンド。</returns>
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
    /// install 本体の実行。source / target を解決してディレクトリコピーを行う。
    /// </summary>
    /// <param name="client">--skills の値 (copilot/claude/codex)。</param>
    /// <param name="global">true ならユーザ home 配下、false なら cwd 配下にインストールする。</param>
    /// <returns>exit code (成功 0)。</returns>
    internal static int Execute(string client, bool global)
    {
        string sourceDir;
        try
        {
            sourceDir = ResolveSourceDirectory();
        }
        catch (DirectoryNotFoundException ex)
        {
            CliError.Write(ErrorCodes.InternalError, ex.Message);
            return ExitCodes.CommandFailed;
        }

        string targetDir;
        try
        {
            targetDir = ResolveTargetDirectory(
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
            CopyDirectory(sourceDir, targetDir);
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
                CliOutput.Field("skill", SkillName),
                CliOutput.Field("path", targetDir),
            ]);
        return ExitCodes.Success;
    }

    /// <summary>
    /// 出力ディレクトリ配下の <c>Skills/adact-cli/</c> を探す。csproj で
    /// CopyToOutputDirectory=PreserveNewest 指定済み (設計 013 §4.2)。
    /// </summary>
    /// <returns>Skill ソースディレクトリの絶対パス。</returns>
    /// <exception cref="DirectoryNotFoundException">ソースディレクトリが見つからない場合。</exception>
    internal static string ResolveSourceDirectory()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Skills", SkillName);
        if (!Directory.Exists(dir))
        {
            throw new DirectoryNotFoundException(
              $"Skill source directory not found at '{dir}'. The adact build may be incomplete.");
        }
        return dir;
    }

    /// <summary>
    /// クライアント名と <c>--global</c> フラグから install 先絶対パスを解決する。
    /// 設計 013 §3.3。テスト容易性のため cwd / homeDir を引数で受け取る。
    /// </summary>
    /// <param name="client">--skills の値 (copilot/claude/codex)。</param>
    /// <param name="global">true なら homeDir を、false なら cwd をベースにする。</param>
    /// <param name="cwd">カレントディレクトリ。</param>
    /// <param name="homeDir">ユーザ home ディレクトリ。</param>
    /// <returns>install 先の絶対パス。</returns>
    /// <exception cref="ArgumentException">未知のクライアント名、もしくは base path が空の場合。</exception>
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

    /// <summary>ユーザ home ディレクトリを取得する。USERPROFILE を優先し、未設定なら SpecialFolder.UserProfile にフォールバック。</summary>
    /// <returns>ユーザ home ディレクトリの絶対パス。</returns>
    private static string GetHomeDirectory()
    {
        // Windows 専用 (TFM net10.0-windows)。USERPROFILE 環境変数を優先することで
        // テストから差し替え可能にする (設計 013 §5.1 - 環境変数で `~` を差し替える方針)。
        var profile = Environment.GetEnvironmentVariable("USERPROFILE");
        if (!string.IsNullOrEmpty(profile))
        {
            return profile;
        }
        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    /// <summary>
    /// <paramref name="source"/> 配下を <paramref name="destination"/> へ再帰コピーする。
    /// 既存ファイルは上書き (設計 013 §3.4)。
    /// </summary>
    /// <param name="source">コピー元ディレクトリ。</param>
    /// <param name="destination">コピー先ディレクトリ。存在しなければ作成される。</param>
    /// <exception cref="IOException">コピー中の IO エラー。</exception>
    /// <exception cref="UnauthorizedAccessException">保存先の書き込み権限不足。</exception>
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
