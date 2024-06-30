#if HAS_SHADER_GRAPH
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEngine.UIElements;
using Edge = UnityEditor.Experimental.GraphView.Edge;

namespace Vertx.EdgeKnife.Editor
{
	internal static class ShaderGraphSupport
	{
		private static readonly Dictionary<MaterialGraphEditWindow, EdgeKnifeManipulator> s_windowsToManipulators = new();
		
		public static bool TryAddKnifeToWindow(EditorWindow window)
		{
			// ReSharper disable once Unity.NoNullPatternMatching
			if (window is not MaterialGraphEditWindow graphWindow)
				return false;

			MaterialGraphView graphView = graphWindow.graphEditorView?.graphView;

			if (graphView == null)
			{
				EdgeKnifeSupport.TryAddKnifeToWindowAfterDelay(window);
				return true;
			}

			if (!s_windowsToManipulators.TryGetValue(graphWindow, out EdgeKnifeManipulator manipulator))
				s_windowsToManipulators.Add(graphWindow, manipulator = new EdgeKnifeManipulator(graphView, CreateRedirectFromKnife));
			else
				graphView.RemoveManipulator(manipulator);
			
			graphView.AddManipulator(manipulator);

			return true;

			void CreateRedirectFromKnife(Vector2 position, IEnumerable<Edge> edges)
			{
				position = graphView.contentViewContainer.LocalToWorld(position);
				RedirectNodeData redirectNode = null;
				foreach (Edge edge in edges)
				{
					if (redirectNode == null)
					{
						// If we've not yet created a relay node for these edges, create it now.
						redirectNode = CreateRedirectNode(graphView, position, edge);
					}
					else
					{
						SlotReference inputSlotRef = edge.input.GetSlot().slotReference;
						SlotReference nodeOutSlotRef = redirectNode.GetSlotReference(RedirectNodeData.kOutputSlotID);
						graphView.graph.Connect(nodeOutSlotRef, inputSlotRef);
					}
				}
			}
		}

		private static RedirectNodeData CreateRedirectNode(MaterialGraphView graphView, Vector2 position, Edge edgeTarget)
		{
			MaterialSlot outputSlot = edgeTarget.output.GetSlot();
			MaterialSlot inputSlot = edgeTarget.input.GetSlot();
			// Need to check if the Nodes that are connected are in a group or not
			// If they are in the same group we also add in the Redirect Node
			// var groupGuidOutputNode = graph.GetNodeFromGuid(outputSlot.slotReference.nodeGuid).groupGuid;
			// var groupGuidInputNode = graph.GetNodeFromGuid(inputSlot.slotReference.nodeGuid).groupGuid;
			GroupData group = null;
			if (outputSlot.owner.group == inputSlot.owner.group)
			{
				group = inputSlot.owner.group;
			}

			return RedirectNodeData.Create(graphView.graph,
				outputSlot.concreteValueType,
				graphView.contentViewContainer.WorldToLocal(position),
				inputSlot.slotReference,
				outputSlot.slotReference,
				group
			);
		}
	}
}
#endif