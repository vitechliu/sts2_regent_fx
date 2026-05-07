using MegaCrit.Sts2.Core.Localization;

namespace RegentFX.Scripts;

public static class SimpleLocUtil {
    public static string Simple(string chs, string eng) {
        var lang = LocManager.Instance?.Language;
        return lang is "zhs" or "zht" ? chs : eng;
    }
}
