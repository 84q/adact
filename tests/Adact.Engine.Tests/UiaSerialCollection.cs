using Xunit;

namespace Adact.Engine.Tests;

/// <summary>
/// 実アプリ (電卓 / Notepad++ など) を起動する L3/L4 テスト用の collection。
/// このコレクションに属するテストは並列実行を無効化する。L1/L2 (Unit, Integration) はこの
/// 属性を付与しないため、collection 既定どおり並列実行される。
/// </summary>
[CollectionDefinition("UiaSerial", DisableParallelization = true)]
public class UiaSerialCollection { }
