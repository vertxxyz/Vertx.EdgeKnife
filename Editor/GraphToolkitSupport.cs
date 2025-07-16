#if HAS_GRAPH_TOOLKIT
using UnityEditor;
using Unity.GraphToolkit.Editor;
using UnityEngine.UIElements;

namespace Vertx.EdgeKnife.Editor
{
	internal static class GraphToolkitSupport
	{
		public static bool TryAddKnifeToWindow(EditorWindow window)
		{
			if (window is not GraphViewEditorWindow graphViewEditorWindow)
			{
				return false;
			}
			
			var hasManipulator = false;
			foreach (GraphView graphView in graphViewEditorWindow.GraphViews)
			{
				hasManipulator = true;
				if (graphView.ClassListContains(EdgeKnifeSupport.HasEdgeKnifeUssClass))
				{
					// Already has a manipulator.
					continue;
				}

				graphView.AddToClassList(EdgeKnifeSupport.HasEdgeKnifeUssClass);
				graphView.AddManipulator(new GraphToolkitEdgeKnifeManipulator(graphView));
			}

			if (!hasManipulator)
			{
				EdgeKnifeSupport.TryAddKnifeToWindowAfterDelay(window);
			}

			return true;
		}
	}
}
#endif