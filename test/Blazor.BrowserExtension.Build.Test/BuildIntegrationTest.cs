﻿using System;
using System.IO;
using System.Threading.Tasks;
using Blazor.BrowserExtension.Build.Test.Helpers;
using FluentAssertions;
using Xunit;

namespace Blazor.BrowserExtension.Build.Test
{
    public class BuildIntegrationTest(BuildIntegrationTestFixture testFixture) : IClassFixture<BuildIntegrationTestFixture>
    {
        [Fact]
        public async Task TestNewProjectFromTemplate()
        {
            var projectName = "NewBrowserExtensionProject";
            var projectDirectory = testFixture.GetTestProjectDirectory(projectName);
            testFixture.ExecuteDotnetCommand($"new browserext --name {projectName}");
            try
            {
                testFixture.ExecuteDotnetRestoreCommand(projectName);
                testFixture.ExecuteDotnetBuildCommand(projectName);
                using (var extensionFromBuild = await testFixture.LoadExtensionBuildOutput(projectName))
                {
                    var pageContentFromBuild = await extensionFromBuild.GetContent("h1");
                    pageContentFromBuild.Should().Be("Hello, from Blazor.");
                }

                testFixture.ExecuteDotnetPublishCommand(projectName);
                using (var extensionFromPublish = await testFixture.LoadExtensionPublishOutput(projectName))
                {
                    var pageContentFromPublish = await extensionFromPublish.GetContent("h1");
                    pageContentFromPublish.Should().Be("Hello, from Blazor.");
                }
            }
            finally
            {
                ResetDirectoryChanges(projectDirectory);
            }
        }

        [Fact]
        public async Task TestBootstrapExistingProject()
        {
            var projectName = "EmptyBlazorProject";
            var projectDirectory = testFixture.GetTestProjectDirectory(projectName);
            try
            {
                testFixture.ExecuteDotnetRestoreCommand(projectName);
                testFixture.ExecuteDotnetBuildCommand(projectName);
                var projectFile = Path.Combine(projectDirectory, projectName + ".csproj");
                var projectFileContent = File.ReadAllText(projectFile);
                projectFileContent.Should().NotContain("BrowserExtensionBootstrap");
                using (var extensionFromBuild = await testFixture.LoadExtensionBuildOutput(projectName))
                {
                    var pageContentFromBuild = await extensionFromBuild.GetContent("h1");
                    pageContentFromBuild.Should().Be("Hello, world!");
                }

                testFixture.ExecuteDotnetPublishCommand(projectName);
                using (var extensionFromPublish = await testFixture.LoadExtensionPublishOutput(projectName))
                {
                    var pageContentFromPublish = await extensionFromPublish.GetContent("h1");
                    pageContentFromPublish.Should().Be("Hello, world!");
                }
            }
            finally
            {
                ResetDirectoryChanges(projectDirectory);
            }
        }

        void ResetDirectoryChanges(string directory)
        {
            try
            {
                CommandHelper.ExecuteCommandVoid("git", "restore .", directory);
            }
            catch (Exception exception) when (exception.Message.Contains("pathspec '.' did not match any file(s) known to git"))
            {
                // No file to restore which is fine
            }
            CommandHelper.ExecuteCommandVoid("git", "clean . -xdf", directory);
        }
    }
}
