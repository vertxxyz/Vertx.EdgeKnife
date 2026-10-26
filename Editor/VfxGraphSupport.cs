#nullable enable

#if HAS_VFX_GRAPH
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VFX;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Vertx.EdgeKnife.Editor
{
#if UNITY_6000_7_OR_NEWER
	[NoAutoStaticsCleanup]
#endif
	internal static class VfxGraphSupport
	{
		private static HashSet<Type>? s_supportedRedirectTypes;
		
		private static HashSet<Type> SupportedRedirectTypes => s_supportedRedirectTypes ?? new InlineTypeProvider()
#if VFX_17_0_1_OR_NEWER
			.GetVariants()
#else
			.ComputeVariants()
#endif
			.Select(v => (Type)(SerializableType)v.settings.First(kvp => kvp.Key == "m_Type").Value)
			.ToHashSet();

		public static bool TryAddKnifeToWindow(EditorWindow window)
		{
			// ReSharper disable once Unity.NoNullPatternMatching
			if (window is not VFXViewWindow graphWindow)
				return false;

			VFXView graphView = graphWindow.graphView;

			if (graphView == null)
			{
				EdgeKnifeSupport.TryAddKnifeToWindowAfterDelay(window);
				return true;
			}
			
			if (graphView.ClassListContains(EdgeKnifeSupport.HasEdgeKnifeUssClassName))
			{
				// Already has a manipulator.
				return true;
			}
			
			graphView.AddToClassList(EdgeKnifeSupport.HasEdgeKnifeUssClassName);
			graphView.AddManipulator(new EdgeKnifeManipulator(graphView, CreateRedirectFromKnife));
			
			return true;

			void CreateRedirectFromKnife(Vector2 position, IEnumerable<Edge> edges)
			{
				VFXNodeController? controller = null;
				foreach (Edge edge in edges)
				{
					if (edge is not VFXDataEdge vfxEdge)
						continue;
					
					VFXDataAnchorController outputSlot = vfxEdge.controller.output;
					VFXDataAnchorController inputSlot = vfxEdge.controller.input;
					Type type = inputSlot.portType ?? outputSlot.portType;

					if (!SupportedRedirectTypes.Contains(type))
						continue;

					if (controller == null)
					{
						// If we've not yet created a relay node for these edges, create it now.
						position += new Vector2(-30, -12);
#if VFX_17_0_1_OR_NEWER
						controller = graphView.AddOperator(typeof(VFXInlineOperator));
						controller.position = position;
#else
						VFXModelDescriptor<VFXOperator> op = VFXLibrary.GetOperators().FirstOrDefault(x => x.modelType == typeof(VFXInlineOperator))!;
						controller = graphView.AddNode(new VFXNodeProvider.Descriptor
						{
							modelDescriptor = op,
							name = op.name
						}, graphView.contentViewContainer.LocalToWorld(position));
						
#endif
						controller.superCollapsed = true;
						controller.model.SetSettingValue("m_Type", (SerializableType)type);
						controller.ApplyChanges();
						graphView.controller.CreateLink(controller.outputPorts.First(), inputSlot);
						graphView.controller.CreateLink(outputSlot, controller.inputPorts.First());
					}
					else
					{
						graphView.controller.CreateLink(controller.outputPorts.First(), inputSlot);
					}
				}
			}
		}
	}
}
#endif