using System;

public static class DialogCGResources
{
    public static bool IsAutoTransitionCommand(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;

        string[] tokens = value.Split('+');
        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i].Trim();
            if (string.Equals(token, "自动转场", StringComparison.Ordinal)
                || string.Equals(token, "自动转场秒", StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    public static string GetPath(string resourceName)
    {
        // These table values explicitly hide CGs or request a transition.
        if (string.IsNullOrEmpty(resourceName) || resourceName == "空" || resourceName == "转场" || IsAutoTransitionCommand(resourceName)) return null;
        if (resourceName.IndexOf('/') >= 0) return resourceName;
        if (resourceName == "黑屏") return "UI/黑屏";
        if (resourceName.StartsWith("小剧场-", StringComparison.Ordinal)) return "CG/TheaterCG/" + resourceName;
        if (resourceName.EndsWith("-动态", StringComparison.Ordinal)) return "CG/DynamicCG/" + resourceName;
        if (string.Equals(resourceName, "PV", StringComparison.OrdinalIgnoreCase)) return "CG/DynamicCG/pv";
        return "CG/GeneralCG/" + resourceName;
    }
}
