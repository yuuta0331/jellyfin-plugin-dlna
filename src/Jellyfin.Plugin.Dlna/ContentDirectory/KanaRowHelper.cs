using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Fifty-sound (gojuon) row definitions for Japanese title browse.
/// </summary>
internal static class KanaRowHelper
{
    /// <summary>
    /// Row index for titles starting with alphanumeric characters.
    /// </summary>
    public const int AlphanumericRowIndex = 10;

    /// <summary>
    /// Row index for titles that cannot be classified into kana or alphanumeric rows.
    /// </summary>
    public const int OtherRowIndex = 11;

    private static readonly string[] RowLabelsJa =
    [
        "あ行", "か行", "さ行", "た行", "な行", "は行", "ま行", "や行", "ら行", "わ行", "英数字", "その他"
    ];

    private static readonly string[] RowLabelsEn =
    [
        "A row", "Ka row", "Sa row", "Ta row", "Na row", "Ha row", "Ma row", "Ya row", "Ra row", "Wa row", "Alphanumeric", "Other"
    ];

    /// <summary>
    /// Gets the number of kana rows.
    /// </summary>
    public static int RowCount => RowLabelsJa.Length;

    /// <summary>
    /// Gets the display label for a kana row.
    /// </summary>
    public static string GetRowLabel(int rowIndex, bool isJapanese)
        => isJapanese ? RowLabelsJa[rowIndex] : RowLabelsEn[rowIndex];

    /// <summary>
    /// Classifies a title into a kana browse row index.
    /// </summary>
    public static int Classify(string? sortName, string? name, KanaClassificationOptions options)
        => KanaTitleClassifier.Classify(sortName, name, options) ?? OtherRowIndex;

    /// <summary>
    /// Returns true when the title belongs to the given kana row.
    /// </summary>
    public static bool MatchesRow(string? sortName, string? name, int rowIndex, KanaClassificationOptions options)
    {
        if (rowIndex < 0 || rowIndex >= RowCount)
        {
            return false;
        }

        return Classify(sortName, name, options) == rowIndex;
    }

    /// <summary>
    /// Builds all kana row virtual folders for a library.
    /// </summary>
    public static ServerItem[] CreateRowFolders(BaseItem library)
    {
        var items = new ServerItem[RowCount];
        for (var i = 0; i < RowCount; i++)
        {
            items[i] = new ServerItem(library, StubType.BrowseByKanaRow, library.Id, i);
        }

        return items;
    }
}
