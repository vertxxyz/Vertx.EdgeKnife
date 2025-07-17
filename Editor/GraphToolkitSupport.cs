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
				if (graphView == null)
				{
					continue;
				}
				
				hasManipulator = true;
				if (graphView.ClassListContains(EdgeKnifeSupport.HasEdgeKnifeUssClassName))
				{
					// Already has a manipulator.
					continue;
				}

				graphView.AddToClassList(EdgeKnifeSupport.HasEdgeKnifeUssClassName);
				graphView.AddManipulator(new WireKnifeManipulator(graphView));
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