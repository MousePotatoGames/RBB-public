using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;

namespace Game.Editor
{
    /// <summary>
    /// Headless-friendly test runner for the verify workflow. Kicks off
    /// EditMode/PlayMode runs and writes results to Temp/ so external tooling
    /// can poll them. [InitializeOnLoad] keeps the result callback alive
    /// across the domain reloads that PlayMode runs trigger.
    /// </summary>
    [InitializeOnLoad]
    public static class TestRunAutomation
    {
        private const string ResultPath = "Temp/firstplayable_test_result.txt";

        static TestRunAutomation()
        {
            var api = ScriptableObject_CreateApi();
            api.RegisterCallbacks(new ResultWriter());
        }

        private static TestRunnerApi ScriptableObject_CreateApi()
        {
            return UnityEngine.ScriptableObject.CreateInstance<TestRunnerApi>();
        }

        public static void RunEditMode(string assemblyName)
        {
            Run(TestMode.EditMode, assemblyName);
        }

        public static void RunPlayMode(string assemblyName)
        {
            Run(TestMode.PlayMode, assemblyName);
        }

        private static void Run(TestMode mode, string assemblyName)
        {
            if (File.Exists(ResultPath))
            {
                File.Delete(ResultPath);
            }

            var api = ScriptableObject_CreateApi();
            var filter = new Filter
            {
                testMode = mode,
                assemblyNames = string.IsNullOrEmpty(assemblyName) ? null : new[] { assemblyName }
            };
            api.Execute(new ExecutionSettings(filter));
        }

        private sealed class ResultWriter : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                var sb = new StringBuilder();
                sb.AppendLine("STATUS=" + result.TestStatus);
                sb.AppendLine("PASSED=" + result.PassCount);
                sb.AppendLine("FAILED=" + result.FailCount);
                sb.AppendLine("SKIPPED=" + result.SkipCount);
                AppendFailures(result, sb);
                File.WriteAllText(ResultPath, sb.ToString());
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }

            private static void AppendFailures(ITestResultAdaptor node, StringBuilder sb)
            {
                if (!node.HasChildren)
                {
                    if (node.TestStatus == TestStatus.Failed)
                    {
                        sb.AppendLine("FAIL: " + node.FullName + " :: " + node.Message);
                    }

                    return;
                }

                foreach (ITestResultAdaptor child in node.Children)
                {
                    AppendFailures(child, sb);
                }
            }
        }
    }
}
