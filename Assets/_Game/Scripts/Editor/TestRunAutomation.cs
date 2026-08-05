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

        public static void RunEditMode(string assemblyName, string testName = null)
        {
            Run(TestMode.EditMode, assemblyName, testName);
        }

        public static void RunPlayMode(string assemblyName, string testName = null)
        {
            Run(TestMode.PlayMode, assemblyName, testName);
        }

        /// <summary>
        /// <paramref name="testName"/> is a regex over full test names. Running one
        /// test alone is how an inter-test state leak gets separated from a real
        /// defect — a suite-only failure means the rig, not the code.
        /// </summary>
        private static void Run(TestMode mode, string assemblyName, string testName)
        {
            if (File.Exists(ResultPath))
            {
                File.Delete(ResultPath);
            }

            var api = ScriptableObject_CreateApi();
            var filter = new Filter
            {
                testMode = mode,
                assemblyNames = string.IsNullOrEmpty(assemblyName) ? null : new[] { assemblyName },
                testNames = string.IsNullOrEmpty(testName) ? null : new[] { testName }
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
