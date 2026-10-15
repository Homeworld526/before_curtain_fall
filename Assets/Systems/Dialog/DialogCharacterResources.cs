public static class DialogCharacterResources
{
    public static string GetHeadPath(string spriteName)
    {
        return GetPath("Head", spriteName);
    }

    public static string GetBodyPath(string spriteName)
    {
        return GetPath("Body", spriteName);
    }

    private static string GetPath(string category, string spriteName)
    {
        // Empty portrait fields explicitly hide the image in dialogue tables.
        if (string.IsNullOrEmpty(spriteName) || spriteName == "空") return null;
        if (spriteName.IndexOf('/') >= 0) return spriteName;

        int separator = spriteName.IndexOf('-');
        string character = separator < 0 ? spriteName : spriteName.Substring(0, separator);
        string folder;
        switch (character)
        {
            case "姜韶平":
            case "夏伦":
            case "张海心":
            case "陈玉澍":
                folder = character;
                break;
            default:
                folder = "其他角色";
                break;
        }

        return $"Characters/{category}/{folder}/{spriteName}";
    }
}
