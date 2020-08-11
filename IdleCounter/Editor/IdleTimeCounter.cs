using System;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace ExternalLib.IdleCounter.Editor
{
	/// <summary>
	/// This script is responsible for measuring the time you have to wait for Unity3D.
	/// It also accumulates the over all wait time and store it in the <see cref="EditorPrefs"/>
	/// </summary>
	[InitializeOnLoad]
	public class IdleTimeCounter
	{
		private const string MenuName = "Tools/IdleTimeCounter/";
		private const string ClearItemName = MenuName + "Clear IdleTimes";
		private const string ActiveMenuItemName = MenuName + "Enabled";

		private const string OverallIdleTimeKey = "OverallIdleTime";
		private const string LastCompileIdleTimeKey = "LastCompileIdleTime";
		private const string LastIdleStartTimeKey = "LastIdleStartTime";

		private const string ActiveIdleTimerKey = "Active";

		private static bool _isActive = true;

		/// <summary>
		/// Static constructor to hook into the Editor callbacks
		/// </summary>
		static IdleTimeCounter()
		{
			Menu.SetChecked(ActiveMenuItemName, _isActive);
			if (!_isActive) return;

			CompilationPipeline.compilationStarted += StartedCompilation;
			CompilationPipeline.compilationFinished += FinishedCompilation;
			AssemblyReloadEvents.afterAssemblyReload += ReloadFinished;
		}

		/// <summary>
		/// Clear all player prefs set for this project
		/// </summary>
		[MenuItem(ClearItemName)]
		public static void ClearEditorPrefsForThisProject()
		{
			EditorPrefs.DeleteKey($"{GetProjectName()}_{OverallIdleTimeKey}");
			EditorPrefs.DeleteKey($"{GetProjectName()}_{LastIdleStartTimeKey}");
			EditorPrefs.DeleteKey($"{GetProjectName()}_{LastCompileIdleTimeKey}");
		}

		/// <summary>
		/// Toggles the IdleTimeCounter on or off
		/// </summary>
		[MenuItem(ActiveMenuItemName)]
		public static void ToggleActivity()
		{
			_isActive = !_isActive;

			bool hasActiveKey = EditorPrefs.HasKey($"{GetProjectName()}_{ActiveIdleTimerKey}");
			if (!hasActiveKey)
			{
				EditorPrefs.SetBool($"{GetProjectName()}_{ActiveIdleTimerKey}", _isActive);
			}

			Menu.SetChecked(ActiveMenuItemName, _isActive);
		}

		/// <summary>
		/// Called as soon as Unity starts compiling code
		/// Stores the timestamp to the EditorPrefs
		/// </summary>
		private static void StartedCompilation(object obj)
		{
			CompilationPipeline.compilationStarted -= StartedCompilation;
			if (!_isActive) return;

			float time = Time.realtimeSinceStartup;

			EditorPrefs.SetFloat($"{GetProjectName()}_{LastIdleStartTimeKey}", time);
		}
		/// <summary>
		/// Called as soon as Unity has finished code compilation
		/// Stores the delta idle time for compilation to the Editor prefs
		/// </summary>
		private static void FinishedCompilation(object obj)
		{
			CompilationPipeline.compilationFinished -= FinishedCompilation;
			if (!_isActive) return;

			float idleStartTime = EditorPrefs.GetFloat($"{GetProjectName()}_{LastIdleStartTimeKey}");
			float deltaCompileTime = Time.realtimeSinceStartup - idleStartTime;
			if (Math.Abs(idleStartTime) < float.Epsilon)
			{
				return;
			}

			EditorPrefs.SetFloat($"{GetProjectName()}_{LastCompileIdleTimeKey}", deltaCompileTime);
		}

		/// <summary>
		/// Called as soon as Unity has finished reloading the AssetDatabase
		/// Stores the delta idle time and the overall idle time to the Editor prefs
		/// </summary>
		private static void ReloadFinished()
		{
			AssemblyReloadEvents.afterAssemblyReload -= ReloadFinished;
			if (!_isActive) return;

			float idleStartTime = EditorPrefs.GetFloat($"{GetProjectName()}_{LastIdleStartTimeKey}");
			float compileIdleTime = EditorPrefs.GetFloat($"{GetProjectName()}_{LastCompileIdleTimeKey}");
			float deltaIdleTime = Time.realtimeSinceStartup - idleStartTime;
			if (Math.Abs(idleStartTime) < float.Epsilon)
			{
				return;
			}
			float overallCompileTime = EditorPrefs.GetFloat($"{GetProjectName()}_{OverallIdleTimeKey}");
			overallCompileTime += deltaIdleTime;

			Debug.Log($"Your idle time was <b>{FormatTime(deltaIdleTime)}</b> (CompileTime: {FormatTime(compileIdleTime)}). Your overall idle time on this project was <color=#BB2222><b>{FormatTime(overallCompileTime)}</b></color>");
			EditorPrefs.SetFloat($"{GetProjectName()}_{OverallIdleTimeKey}", overallCompileTime);
			EditorPrefs.DeleteKey($"{GetProjectName()}_{LastIdleStartTimeKey}");
			EditorPrefs.DeleteKey($"{GetProjectName()}_{LastCompileIdleTimeKey}");
		}

		/// <summary>
		/// Returns the formatted time
		/// </summary>
		private static string FormatTime(float time)
		{
			var minutes = (int)( time / 60 );
			int hours = minutes / 60;
			int days = hours / 24;
			var idleTime = "";
			if (minutes <= 0)
			{
				idleTime = $"{time:##.##} [s]";
			}
			else if (hours <= 0)
			{
				idleTime = $"{minutes:00}:{(time % 60):00} [m]";
			}
			else if (days <= 0)
			{
				idleTime = $"{hours:00}:{(minutes % 60):00}:{(time % 60):00} [h]";
			}
			else
			{
				idleTime = $"{days:00}:{(hours % 24):00}:{(minutes % 60):00}:{(time % 60):00} [d]";
			}
			return idleTime;
		}

		/// <summary>
		/// Returns the current project name. Since Unity has no API for this we assume that
		/// the project name is (pathTokens.length - 2) e.g.
		/// Path of : /Projects/Unity/AwesomeProject/Assets/
		/// would return 'AwesomeProject'
		/// </summary>
		private static string GetProjectName()
		{
			string path = Application.dataPath;
			string[] tokens;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				tokens = path.Split('\\');
			}
			else
			{
				tokens = path.Split('/');
			}

			return tokens[tokens.Length - 2];
		}
	}
}
