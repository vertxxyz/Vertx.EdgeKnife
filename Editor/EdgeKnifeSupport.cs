using UnityEditor;

namespace Vertx.EdgeKnife.Editor
{
	/// <summary>
	/// Polls for new windows and attempts to add edge knife support.
	/// </summary>
	[InitializeOnLoad]
	internal static class EdgeKnifeSupport
	{
		static EdgeKnifeSupport()
		{
			TryInitializeFocusedWindow();
#if UNITY_2023_2_OR_NEWER
			EditorWindow.windowFocusChanged += TryInitializeFocusedWindow;
		}
#else
			EditorApplication.update += PollForWindowFocusChanges;
		}

		private static EditorWindow s_focusedWindow;

		private static void PollForWindowFocusChanges()
		{
			if (EditorWindow.focusedWindow == s_focusedWindow)
				return;
			s_focusedWindow = EditorWindow.focusedWindow;
			TryAddKnifeToNewWindow(s_focusedWindow);
		}
#endif

		private static void TryInitializeFocusedWindow() => TryAddKnifeToNewWindow(EditorWindow.focusedWindow);

		internal static void TryAddKnifeToWindowAfterDelay(EditorWindow window)
		{
			EditorApplication.delayCall += () =>
			{
				if (EditorWindow.focusedWindow != window)
				{
					return;
				}

				TryAddKnifeToNewWindow(window);
			};
		}

		private static void TryAddKnifeToNewWindow(EditorWindow window)
		{
#if HAS_SHADER_GRAPH
			if (ShaderGraphSupport.TryAddKnifeToWindow(window))
			{
				return;
			}
#endif
#if HAS_VFX_GRAPH
			if (VfxGraphSupport.TryAddKnifeToWindow(window))
			{
				return;
			}
#endif
		}
	}
}