using System;

public static class DialogCGResources
{
    public static string GetPath(string resourceName)
    {
        // These table values explicitly hide CGs or request a transition.
        if (string.IsNullOrEmpty(resourceName) || resourceName == "空" || resourceName == "转场") return null;
        if (resourceName.IndexOf('/') >= 0) return resourceName;
        if (resourceName == "黑屏") return "UI/黑屏";
        if (resourceName.StartsWith("小剧场-", StringComparison.Ordinal)) return "CG/TheaterCG/" + resourceName;
        if (resourceName.EndsWith("-动态", StringComparison.Ordinal)) return "CG/DynamicCG/" + resourceName;
        if (string.Equals(resourceName, "PV", StringComparison.OrdinalIgnoreCase)) return "CG/DynamicCG/pv";
        return "CG/GeneralCG/" + resourceName;
    }
}
