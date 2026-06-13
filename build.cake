var target = Argument("target", "Build");

var workflow = BuildSystem.GitHubActions.Environment.Workflow;
var buildId = workflow.RunNumber;
var tag = workflow.RefType == GitHubActionsRefType.Tag ? workflow.RefName : null;

Task("Build")
    .Does(() =>
{
    var settings = new DotNetBuildSettings
    {
        Configuration = "Release",
        NoRestore = buildId != 0,
        MSBuildSettings = new DotNetMSBuildSettings()
    };

    if (tag != null) 
    {
        var version = tag.StartsWith("v") ? tag.Substring(1) : tag;
        settings.MSBuildSettings.Version = version;
    }
    else if (buildId != 0)
    {
        settings.MSBuildSettings.VersionSuffix = "ci." + buildId;
    }

    DotNetBuild(".", settings);
});

RunTarget(target);
