using System;
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
		private const string OverallIdleTime = "OverallIdleTime";
		private const string LastCompileIdleTime = "LastCompileIdleTime";
		private const string LastIdleStartTime = "LastIdleStartTime";

		static IdleTimeCounter()
		{
			CompilationPipeline.compilationStarted += StartedCompilation;
			CompilationPipeline.compilationFinished += FinishedCompilation;
			AssemblyReloadEvents.afterAssemblyReload += ReloadFinished;
		}

		private static void StartedCompilation(object obj)
		{
			CompilationPipeline.compilationStarted -= StartedCompilation;

			float time = Time.realtimeSinceStartup;

			EditorPrefs.SetFloat($"{GetProjectName()}_{LastIdleStartTime}", time);
		}

		private static void FinishedCompilation(object obj)
		{
			CompilationPipeline.compilationFinished -= FinishedCompilation;
			float idleStartTime = EditorPrefs.GetFloat($"{GetProjectName()}_{LastIdleStartTime}");
			float deltaCompileTime = Time.realtimeSinceStartup - idleStartTime;
			if (Math.Abs(idleStartTime) < float.Epsilon)
			{
				return;
			}

			EditorPrefs.SetFloat($"{GetProjectName()}_{LastCompileIdleTime}", deltaCompileTime);
		}

		private static void ReloadFinished()
		{
			AssemblyReloadEvents.afterAssemblyReload -= ReloadFinished;

			float idleStartTime = EditorPrefs.GetFloat($"{GetProjectName()}_{LastIdleStartTime}");
			float compileIdleTime = EditorPrefs.GetFloat($"{GetProjectName()}_{LastCompileIdleTime}");
			float deltaIdleTime = Time.realtimeSinceStartup - idleStartTime;
			if (Math.Abs(idleStartTime) < float.Epsilon)
			{
				return;
			}
			float overallCompileTime = EditorPrefs.GetFloat($"{GetProjectName()}_{OverallIdleTime}");
			overallCompileTime += deltaIdleTime;

			Debug.Log($"Your idle time was <b>{FormatTime(deltaIdleTime)}</b> (CompileTime: {FormatTime(compileIdleTime)}). Your overall idle time on this project was <color=#BB2222><b>{FormatTime(overallCompileTime)}</b></color>");
			EditorPrefs.SetFloat($"{GetProjectName()}_{OverallIdleTime}", overallCompileTime);
			EditorPrefs.DeleteKey($"{GetProjectName()}_{LastIdleStartTime}");
			EditorPrefs.DeleteKey($"{GetProjectName()}_{LastCompileIdleTime}");
		}

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
				idleTime = $"{hours:00}:{minutes:00}:{(time % 60):00} [h]";
			}
			else
			{
				idleTime = $"{days:00}:{hours:00}:{minutes:00}:{(time % 60):00} [d]";
			}
			return idleTime;
		}

		private static string GetProjectName()
		{
			string path = Application.dataPath;
			string[] tokens = path.Split('/');

			return tokens[tokens.Length - 2];
		}
	}
}
